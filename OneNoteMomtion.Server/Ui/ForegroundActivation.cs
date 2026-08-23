using System;
using System.Runtime.InteropServices;

namespace OneNoteMomtion.Server.Ui
{
    internal static class ForegroundActivation
    {
        internal static bool Take(IntPtr window)
        {
            if (window == IntPtr.Zero)
            {
                return false;
            }

            IntPtr foreground = GetForegroundWindow();
            if (foreground == window)
            {
                return true;
            }

            uint ownThread = GetCurrentThreadId();
            uint foregroundThread = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out _);

            bool attached = foregroundThread != 0 && foregroundThread != ownThread && AttachThreadInput(ownThread, foregroundThread, true);

            try
            {
                _ = BringWindowToTop(window);
                _ = SetForegroundWindow(window);
                _ = SetActiveWindow(window);
                _ = SetFocus(window);
            }
            finally
            {
                if (attached)
                {
                    _ = AttachThreadInput(ownThread, foregroundThread, false);
                }
            }

            return GetForegroundWindow() == window;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachThreadInput(uint attachTo, uint attachFrom, [MarshalAs(UnmanagedType.Bool)] bool attach);
    }
}
