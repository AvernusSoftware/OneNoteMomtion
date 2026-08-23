using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace OneNoteMomtion.Server.Ui
{
    internal static class OneNoteWindowFinder
    {
        public static IntPtr TryFindMainWindow(int? hostProcessId)
        {
            int? processId = hostProcessId ?? TryFindOneNoteProcessId();
            if (processId is null)
            {
                EngineLog.Info("UIA-NAV: no OneNote process to look for a window in.");
                return IntPtr.Zero;
            }

            IntPtr foreground = GetForegroundWindow();
            if (foreground != IntPtr.Zero && WindowProcessId(foreground) == processId)
            {
                return foreground;
            }

            IntPtr best = IntPtr.Zero;
            long bestArea = -1;

            foreach (IntPtr window in TopLevelWindowsOf(processId.Value))
            {
                if (!IsWindowVisible(window) || GetWindowTextLength(window) == 0)
                {
                    continue;
                }

                if (!GetWindowRect(window, out WindowRect rect))
                {
                    continue;
                }

                long area = (long)Math.Max(0, rect.Right - rect.Left) * Math.Max(0, rect.Bottom - rect.Top);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = window;
                }
            }

            if (best == IntPtr.Zero)
            {
                EngineLog.Info($"UIA-NAV: process {processId} has no visible top-level window.");
            }

            return best;
        }

        public static string Describe(IntPtr window)
        {
            if (window == IntPtr.Zero)
            {
                return "-";
            }

            StringBuilder className = new(256);
            GetClassName(window, className, className.Capacity);

            StringBuilder title = new(256);
            GetWindowText(window, title, title.Capacity);

            return window.ToInt64().ToString("X") + " " + className + "/'" + title + "'";
        }

        private static int? TryFindOneNoteProcessId()
        {
            try
            {
                foreach (Process process in Process.GetProcessesByName("ONENOTE"))
                {
                    using (process)
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            return process.Id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EngineLog.Warn($"UIA-NAV: looking for the OneNote process failed: {ex.Message}");
            }

            return null;
        }

        private static IReadOnlyList<IntPtr> TopLevelWindowsOf(int processId)
        {
            List<IntPtr> windows = [];

            EnumWindows((window, _) =>
            {
                if (WindowProcessId(window) == processId)
                {
                    windows.Add(window);
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        private static int WindowProcessId(IntPtr window)
        {
            _ = GetWindowThreadProcessId(window, out uint processId);
            return (int)processId;
        }

        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr window, out WindowRect rect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr window);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr window, StringBuilder className, int maxCount);
    }
}
