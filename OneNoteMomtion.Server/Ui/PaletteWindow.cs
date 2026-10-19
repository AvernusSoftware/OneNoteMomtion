using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Server.Commands;

namespace OneNoteMomtion.Server.Ui
{
    internal sealed class PaletteWindow : Form, IPaletteController
    {
        private const string VirtualHost = "onenotemomtion.local";

        private const int DefaultWidth = 380;
        private const int DefaultHeight = 460;

        private const int DwmWindowAttributeCornerPreference = 33;
        private const int DwmWindowCornerPreferenceRound = 2;

        private readonly WebView2 _webView;
        private readonly Func<string, string?, CommandResult> _handler;
        private readonly string _contentRoot;
        private readonly Func<Localizer> _texts;

        private Task? _webViewReady;
        private bool _everActivated;
        private bool _suppressDeactivateHide;
        private string _currentView = "actions";
        private Point _anchor;

        public PaletteWindow(Func<string, string?, CommandResult> handler, string contentRoot, Func<Localizer> texts)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _contentRoot = contentRoot;
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            MinimizeBox = false;
            MaximizeBox = false;
            Size = new Size(DefaultWidth, DefaultHeight);
            BackColor = Color.FromArgb(250, 250, 249);
            Text = "OneNoteMomtion";

            _webView = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = BackColor };
            _webView.WebMessageReceived += OnWebMessageReceived;
            Controls.Add(_webView);

            Activated += (_, _) =>
            {
                _everActivated = true;
                EngineLog.Info($"PALETTE-DEBUG: Activated (view={_currentView}).");
            };

            Deactivate += (_, _) =>
            {
                EngineLog.Info($"PALETTE-DEBUG: Deactivate (view={_currentView}, everActivated={_everActivated}, suppressed={_suppressDeactivateHide}).");

                if (_everActivated && !_suppressDeactivateHide)
                {
                    Hide();
                }
            };

            _ = Handle;
            _webViewReady = InitializeWebViewAsync();
        }

        public void Show(string? view)
        {
            EngineLog.Info($"PALETTE-DEBUG: Show(\"{view}\") - previously visible={Visible}.");

            _currentView = string.IsNullOrWhiteSpace(view) ? "actions" : view!;
            _everActivated = false;
            _anchor = CaretLocator.TryGetCaretScreenPosition() ?? Cursor.Position;
            PositionAtAnchor();

            _suppressDeactivateHide = true;
            try
            {
                Show();
                BringToFront();
                Activate();

                if (!ForegroundActivation.Take(Handle))
                {
                    EngineLog.Warn("The palette did not take the foreground - keyboard focus stays in OneNote.");
                }

                FocusContent();

                if (_webViewReady is { IsCompletedSuccessfully: true })
                {
                    PostViewChange();
                }
            }
            finally
            {
                BeginInvoke(new Action(() => _suppressDeactivateHide = false));
            }

            EngineLog.Info($"PALETTE-DEBUG: Show() finished - visible={Visible} location={Location} size={Size} webViewReady={_webViewReady?.Status}.");
        }

        public new void Hide()
        {
            EngineLog.Info($"PALETTE-DEBUG: Hide() (view={_currentView}).");

            _everActivated = false;
            base.Hide();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundedCorners();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneNoteMomtion", "webview2");

                Directory.CreateDirectory(userDataFolder);

                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await _webView.EnsureCoreWebView2Async(environment);

                CoreWebView2 core = _webView.CoreWebView2;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.AreBrowserAcceleratorKeysEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsZoomControlEnabled = false;

                core.NavigationCompleted += (_, e) =>
                {
                    EngineLog.Info($"PALETTE-DEBUG: NavigationCompleted isSuccess={e.IsSuccess} status={e.WebErrorStatus}.");

                    PostViewChange();
                    FocusContent();
                };

                core.ProcessFailed += (_, e) =>
                {
                    EngineLog.Error($"PALETTE-DEBUG: WebView2 process failed - kind={e.ProcessFailedKind} reason={e.Reason} exitCode={e.ExitCode} processDescription={e.ProcessDescription}.");
                };

                if (Directory.Exists(_contentRoot))
                {
                    core.SetVirtualHostNameToFolderMapping(VirtualHost, _contentRoot, CoreWebView2HostResourceAccessKind.Allow);
                    core.Navigate($"https://{VirtualHost}/index.html");
                }
                else
                {
                    EngineLog.Warn($"Missing frontend directory: {_contentRoot}");
                    core.NavigateToString(MissingFrontendHtml(_contentRoot, _texts()));
                }
            }
            catch (Exception ex)
            {
                EngineLog.Error("Failed to start WebView2.", ex);

                Localizer texts = _texts();

                MessageBox.Show(
                    texts.Get(TextKeys.EngineWebViewFailed) + Environment.NewLine + Environment.NewLine +
                    ex.Message + Environment.NewLine + Environment.NewLine +
                    texts.Get(TextKeys.EngineWebViewCheckRuntime),
                    "OneNoteMomtion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Hide();
            }
        }

        private void FocusContent()
        {
            if (!Visible)
            {
                return;
            }

            _ = _webView.Focus();
        }

        private void PostViewChange()
        {
            _webView.CoreWebView2?.PostWebMessageAsJson("{\"type\":\"view\",\"view\":\"" + _currentView + "\"}");
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string? requestId = null;

            try
            {
                PaletteRequest? request = PaletteMessages.ParseRequest(e.WebMessageAsJson);
                if (request?.Command is null)
                {
                    return;
                }

                requestId = request.RequestId;

                if (request.Command.StartsWith("ui.", StringComparison.Ordinal))
                {
                    Respond(PaletteMessages.SerializeResponse(requestId, HandleUiCommand(request)));
                    return;
                }

                CommandResult result = _handler(request.Command, request.Argument);
                Respond(PaletteMessages.SerializeResponse(requestId, result));

                if (result.Changed)
                {
                    Hide();
                }
            }
            catch (Exception ex)
            {
                EngineLog.Error("Error handling a message from the palette.", ex);
                Respond(PaletteMessages.SerializeError(requestId, ex.Message));
            }
        }

        private CommandResult HandleUiCommand(PaletteRequest request)
        {
            switch (request.Command)
            {
                case "ui.close":
                    Hide();
                    return CommandResult.NothingToDo();

                case "ui.resize":
                    return ResizeToContent(request.Argument);

                default:
                    return CommandResult.Failed(_texts().Format(TextKeys.EngineUnknownUiCommand, ("command", request.Command ?? string.Empty)));
            }
        }

        private CommandResult ResizeToContent(string? argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                return CommandResult.Failed(_texts().Get(TextKeys.EngineNoDimensions));
            }

            string[] parts = argument!.Split('x');
            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width) || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
            {
                return CommandResult.Failed(_texts().Format(TextKeys.EngineUnreadableDimensions, ("value", argument)));
            }

            double scale = DeviceDpi / 96.0;
            int scaledWidth = (int)Math.Round(width * scale);
            int scaledHeight = (int)Math.Round(height * scale);

            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            Size = new Size(Math.Clamp(scaledWidth, 240, workingArea.Width), Math.Clamp(scaledHeight, 120, workingArea.Height));

            EngineLog.Info($"PALETTE-DEBUG: ResizeToContent {argument} css -> {Size} device (dpi={DeviceDpi}).");

            PositionAtAnchor();
            return CommandResult.NothingToDo();
        }

        private void Respond(string json)
        {
            _webView.CoreWebView2?.PostWebMessageAsJson(json);
        }

        private void PositionAtAnchor()
        {
            Rectangle workingArea = Screen.FromPoint(_anchor).WorkingArea;

            Location = new Point(Math.Max(workingArea.Left, Math.Min(_anchor.X - 8, workingArea.Right - Width)), Math.Max(workingArea.Top, Math.Min(_anchor.Y + 4, workingArea.Bottom - Height)));
        }

        private void ApplyRoundedCorners()
        {
            try
            {
                int preference = DwmWindowCornerPreferenceRound;
                _ = DwmSetWindowAttribute(Handle, DwmWindowAttributeCornerPreference, ref preference, sizeof(int));
            }
            catch (Exception)
            {
                // Older Windows versions do not know this attribute - the window will have sharp corners.
            }
        }

        private static string MissingFrontendHtml(string contentRoot, Localizer texts)
        {
            return "<!doctype html><html lang=\"" + texts.Language + "\"><head><meta charset=\"utf-8\"><style>" +
                   "body{font-family:Segoe UI,sans-serif;padding:24px;color:#37352F;background:#FAFAF9}" +
                   "code{background:#F1F1EF;padding:2px 6px;border-radius:4px}</style></head><body>" +
                   "<h3>" + texts.Get(TextKeys.EngineFrontendMissingTitle) + "</h3><p>" + texts.Get(TextKeys.EngineFrontendMissingDirectory) + "</p><p><code>" +
                   System.Net.WebUtility.HtmlEncode(contentRoot) +
                   "</code></p><p>" + texts.Get(TextKeys.EngineFrontendMissingHint) + "</p></body></html>";
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
