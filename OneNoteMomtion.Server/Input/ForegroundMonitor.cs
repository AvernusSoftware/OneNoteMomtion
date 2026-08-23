using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OneNoteMomtion.Server.Input
{
    internal sealed class ForegroundMonitor : IDisposable
    {
        private const int PollMilliseconds = 400;

        private readonly int? _hostProcessId;
        private readonly Timer _timer;

        private bool _hostInForeground;
        private bool _hostOrPaletteInForeground;

        public ForegroundMonitor(int? hostProcessId)
        {
            _hostProcessId = hostProcessId;

            _timer = new Timer { Interval = PollMilliseconds };
            _timer.Tick += (_, _) => Poll();
        }

        public event EventHandler? Changed;

        public bool HostInForeground => _hostInForeground;

        public bool HostOrPaletteInForeground => _hostOrPaletteInForeground;

        public void Start()
        {
            Poll();
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }

        private void Poll()
        {
            ReadForegroundState(out bool hostInForeground, out bool hostOrPaletteInForeground);

            if (hostInForeground == _hostInForeground && hostOrPaletteInForeground == _hostOrPaletteInForeground)
            {
                return;
            }

            _hostInForeground = hostInForeground;
            _hostOrPaletteInForeground = hostOrPaletteInForeground;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void ReadForegroundState(out bool hostInForeground, out bool hostOrPaletteInForeground)
        {
            if (_hostProcessId is null)
            {
                hostInForeground = true;
                hostOrPaletteInForeground = true;
                return;
            }

            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                hostInForeground = false;
                hostOrPaletteInForeground = false;
                return;
            }

            _ = GetWindowThreadProcessId(foregroundWindow, out uint processId);

            hostInForeground = processId == _hostProcessId.Value;
            hostOrPaletteInForeground = hostInForeground || processId == (uint)Environment.ProcessId;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}
