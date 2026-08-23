using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OneNoteMomtion.Core.Input;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Server.Input
{
    internal sealed class HotkeyListener : IDisposable
    {
        private const int WmHotkey = 0x0312;
        private const int HotkeyId = 0xB001;
        private const uint ModNoRepeat = 0x4000;

        private static readonly HotkeyCombo DefaultCombo = new(HotkeyModifiers.Control, 0x20);

        private readonly ForegroundMonitor _foreground;
        private readonly Action _onPressed;
        private readonly HotkeyWindow _window;

        private HotkeyCombo _combo = DefaultCombo;
        private bool _registered;

        public HotkeyListener(ForegroundMonitor foreground, Action onPressed)
        {
            _foreground = foreground ?? throw new ArgumentNullException(nameof(foreground));
            _onPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));

            _window = new HotkeyWindow(OnHotkey);
            _foreground.Changed += (_, _) => SyncRegistration();
        }

        public void Start()
        {
            SyncRegistration();
        }

        public void ApplySettings(StyleCatalog catalog)
        {
            HotkeyCombo combo = ResolveCombo(catalog.PaletteHotkey);

            if (combo == _combo)
            {
                return;
            }

            _combo = combo;

            if (_registered)
            {
                Unregister();
                Register();
            }
        }

        private static HotkeyCombo ResolveCombo(string? text)
        {
            if (HotkeyCombo.TryParse(text, out HotkeyCombo combo))
            {
                return combo;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                EngineLog.Warn("Invalid paletteHotkey \"" + text + "\" - falling back to Ctrl+Space.");
            }

            return DefaultCombo;
        }

        public void Dispose()
        {
            Unregister();
            _window.DestroyHandle();
        }

        private void OnHotkey()
        {
            try
            {
                _onPressed();
            }
            catch (Exception ex)
            {
                EngineLog.Error("Hotkey handling failed.", ex);
            }
        }

        private void SyncRegistration()
        {
            bool shouldBeActive = _foreground.HostOrPaletteInForeground;

            if (shouldBeActive && !_registered)
            {
                Register();
            }
            else if (!shouldBeActive && _registered)
            {
                Unregister();
            }
        }

        private void Register()
        {
            if (RegisterHotKey(_window.Handle, HotkeyId, (uint)_combo.Modifiers | ModNoRepeat, _combo.VirtualKey))
            {
                _registered = true;
                return;
            }

            EngineLog.Warn("Failed to register the " + _combo + " hotkey (error " + Marshal.GetLastWin32Error() + ").");
        }

        private void Unregister()
        {
            if (!_registered)
            {
                return;
            }

            _ = UnregisterHotKey(_window.Handle, HotkeyId);
            _registered = false;
        }

        private sealed class HotkeyWindow : NativeWindow
        {
            private readonly Action _onHotkey;

            public HotkeyWindow(Action onHotkey)
            {
                _onHotkey = onHotkey;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
                {
                    _onHotkey();
                    return;
                }

                base.WndProc(ref m);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
