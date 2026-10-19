using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using OneNoteMomtion.Server.Commands;
using OneNoteMomtion.Server.Input;
using OneNoteMomtion.Server.Ipc;
using OneNoteMomtion.Server.OneNote;
using OneNoteMomtion.Server.Ui;

namespace OneNoteMomtion.Server
{
    internal sealed class EngineContext : ApplicationContext
    {
        private const string FrontendFolderName = "wwwroot";

        private readonly EngineServices _services;
        private readonly OneNoteConnection _oneNote;
        private readonly PaletteWindow _palette;
        private readonly CommandDispatcher _dispatcher;
        private readonly CommandPipeServer _pipeServer;
        private readonly ForegroundMonitor _foreground;
        private readonly HotkeyListener _hotkey;
        private readonly TypingListener _typing;
        private Process? _hostProcess;
        private bool _disposed;

        public EngineContext(EngineOptions options)
        {
            _services = new EngineServices();
            _oneNote = new OneNoteConnection();

            string contentRoot = Path.Combine(AppContext.BaseDirectory, FrontendFolderName);
            _palette = new PaletteWindow(Execute, contentRoot, () => _services.Texts);

            _foreground = new ForegroundMonitor(options.HostProcessId);

            _dispatcher = new CommandDispatcher(_services, _oneNote, _palette, new CaretRestorer(_foreground), new NavigationPaneAutomation(options.HostProcessId));

            _pipeServer = new CommandPipeServer(options.PipeName, Execute, SynchronizationContext.Current!);
            _pipeServer.ShutdownRequested += (_, _) => Shutdown("request from the add-in");
            _pipeServer.Start();

            _hotkey = new HotkeyListener(_foreground, TogglePalette);
            _typing = new TypingListener(_services, _foreground, _dispatcher.Execute);

            _foreground.Start();
            _hotkey.ApplySettings(_services.Catalog);
            _hotkey.Start();
            _typing.ApplySettings();

            WatchHostProcess(options.HostProcessId);
        }

        private void TogglePalette()
        {
            if (_palette.Visible)
            {
                _palette.Hide();
                return;
            }

            _palette.Show("actions");
        }

        private CommandResult Execute(string command, string? argument)
        {
            CommandResult result = _dispatcher.Execute(command, argument);
            _hotkey.ApplySettings(_services.Catalog);
            _typing.ApplySettings();

            return result;
        }

        private void WatchHostProcess(int? hostProcessId)
        {
            if (hostProcessId is null)
            {
                return;
            }

            try
            {
                _hostProcess = Process.GetProcessById(hostProcessId.Value);
                _hostProcess.EnableRaisingEvents = true;
                _hostProcess.Exited += (_, _) => BeginInvokeShutdown("the OneNote process exited");
            }
            catch (ArgumentException)
            {
                EngineLog.Warn("The OneNote process with id " + hostProcessId + " no longer exists.");
                BeginInvokeShutdown("the OneNote process does not exist");
            }
        }

        private void BeginInvokeShutdown(string reason)
        {
            if (_palette.IsHandleCreated)
            {
                _palette.BeginInvoke(new Action(() => Shutdown(reason)));
                return;
            }

            Shutdown(reason);
        }

        private void Shutdown(string reason)
        {
            _oneNote.Reset();
            EngineLog.Info("Shutting down the engine (" + reason + ").");
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _oneNote.Dispose();
                _typing.Dispose();
                _hotkey.Dispose();
                _foreground.Dispose();
                _pipeServer.Dispose();
                _palette.Dispose();
                _hostProcess?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
