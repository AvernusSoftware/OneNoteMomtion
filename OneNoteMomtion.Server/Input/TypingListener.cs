using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using OneNoteMomtion.Core.Markdown;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Server.Commands;

namespace OneNoteMomtion.Server.Input
{
    internal sealed class TypingListener : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int HcAction = 0;

        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;

        private const int RetryDelayMilliseconds = 200;

        private const int EnterDelayMilliseconds = 40;

        private readonly EngineServices _services;
        private readonly ForegroundMonitor _foreground;
        private readonly Func<string, string?, CommandResult> _execute;
        private readonly HashKeyTracker _tracker = new();
        private readonly Timer _timer;

        private readonly LowLevelKeyboardProc _callback;

        private IntPtr _hook = IntPtr.Zero;
        private bool _markdownEnabled;
        private bool _normalAfterHeadingEnabled;
        private int _delayMilliseconds = 120;

        private bool _running;
        private int _attempt;

        private string _command = CommandIds.MarkdownTyping;
        private string? _argument;

        public TypingListener(EngineServices services, ForegroundMonitor foreground, Func<string, string?, CommandResult> execute)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _foreground = foreground ?? throw new ArgumentNullException(nameof(foreground));
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));

            _callback = OnKey;
            _timer = new Timer();
            _timer.Tick += (_, _) => Tick();

            _foreground.Changed += (_, _) => SyncHook();
        }

        public void ApplySettings()
        {
            StyleCatalog catalog = _services.Catalog;
            _delayMilliseconds = catalog.TypingApplyDelayMs;

            Toggle(ref _markdownEnabled, catalog.AutoMarkdownTyping, "Automatic Markdown headings");
            Toggle(ref _normalAfterHeadingEnabled, catalog.AutoNormalAfterHeading, "Normal text after a heading");

            SyncHook();
        }

        private static void Toggle(ref bool current, bool wanted, string what)
        {
            if (wanted == current)
            {
                return;
            }

            current = wanted;
            EngineLog.Info(what + (wanted ? " enabled." : " disabled."));
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
            Unhook();
        }

        private void SyncHook()
        {
            bool shouldBeActive = (_markdownEnabled || _normalAfterHeadingEnabled) && _foreground.HostInForeground;

            if (shouldBeActive)
            {
                Hook();
                return;
            }

            Unhook();
        }

        private void Hook()
        {
            if (_hook != IntPtr.Zero)
            {
                return;
            }

            _hook = SetWindowsHookEx(WhKeyboardLl, _callback, GetModuleHandle(null), 0);

            if (_hook == IntPtr.Zero)
            {
                EngineLog.Warn($"Failed to install the keyboard hook (error {Marshal.GetLastWin32Error()}) - automatic reactions to typing are unavailable.");
            }
        }

        private void Unhook()
        {
            _tracker.Reset();

            if (_hook == IntPtr.Zero)
            {
                return;
            }

            _ = UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        private IntPtr OnKey(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code == HcAction)
            {
                try
                {
                    int message = wParam.ToInt32();
                    if (message == WmKeyDown || message == WmSysKeyDown)
                    {
                        KeyboardHookStruct key = Marshal.PtrToStructure<KeyboardHookStruct>(lParam);

                        if (key.ExtraInfo != CaretRestorer.InjectedMarker)
                        {
                            Track(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EngineLog.Error("Keyboard hook callback failed.", ex);
                }
            }

            return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private void Track(KeyboardHookStruct key)
        {
            Keys virtualKey = (Keys)key.VirtualKeyCode;

            if (IsResetKey(virtualKey))
            {
                _tracker.Reset();

                if (virtualKey == Keys.Enter)
                {
                    OnEnter();
                }

                return;
            }

            if (!TryDecode(key, out char character))
            {
                return;
            }

            if (_markdownEnabled && _tracker.OnCharacter(character))
            {
                Schedule(CommandIds.MarkdownTyping, null, _delayMilliseconds, first: true);
            }
        }

        private void OnEnter()
        {
            if (!_normalAfterHeadingEnabled || IsModifierDown())
            {
                return;
            }

            Schedule(CommandIds.StyleNormalAfterHeading, null, EnterDelayMilliseconds, first: true);
        }

        private static bool IsModifierDown()
        {
            return PressedFlag(Keys.ShiftKey) != 0 || PressedFlag(Keys.ControlKey) != 0 || PressedFlag(Keys.Menu) != 0;
        }

        private static bool IsResetKey(Keys key)
        {
            switch (key)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Enter:
                case Keys.Tab:
                case Keys.Escape:
                case Keys.Back:
                case Keys.Delete:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryDecode(KeyboardHookStruct key, out char character)
        {
            character = '\0';

            IntPtr foreground = GetForegroundWindow();
            uint threadId = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out _);
            IntPtr layout = GetKeyboardLayout(threadId);

            byte[] state = new byte[256];
            state[(int)Keys.ShiftKey] = PressedFlag(Keys.ShiftKey);
            state[(int)Keys.ControlKey] = PressedFlag(Keys.ControlKey);
            state[(int)Keys.Menu] = PressedFlag(Keys.Menu);
            state[(int)Keys.Capital] = (byte)((GetKeyState((int)Keys.Capital) & 0x0001) != 0 ? 0x01 : 0x00);

            StringBuilder buffer = new(8);
            int written = ToUnicodeEx(key.VirtualKeyCode, key.ScanCode, state, buffer, buffer.Capacity, DoNotChangeKeyboardState, layout);

            if (written < 1 || buffer.Length < 1)
            {
                return false;
            }

            character = buffer[0];
            return true;
        }

        private static byte PressedFlag(Keys key)
        {
            return (byte)((GetAsyncKeyState((int)key) & 0x8000) != 0 ? 0x80 : 0x00);
        }

        private void Schedule(string command, string? argument, int delayMilliseconds, bool first)
        {
            if (first)
            {
                _attempt = 0;
            }

            _command = command;
            _argument = argument;

            _timer.Stop();
            _timer.Interval = Math.Max(1, delayMilliseconds);
            _timer.Start();
        }

        private void Tick()
        {
            _timer.Stop();

            if (_running)
            {
                return;
            }

            _running = true;
            _attempt++;

            string command = _command;

            try
            {
                CommandResult result = _execute(command, _argument);

                if (!result.Success)
                {
                    EngineLog.Warn($"Command \"{command}\" from the keyboard hook failed: {result.Message}");
                    return;
                }

                if (result.Changed || _attempt > 1)
                {
                    return;
                }

                if (command == CommandIds.MarkdownTyping)
                {
                    Schedule(command, null, RetryDelayMilliseconds, first: false);
                }
                else if (command == CommandIds.StyleNormalAfterHeading && result.CaretObjectId != null)
                {
                    Schedule(command, result.CaretObjectId, RetryDelayMilliseconds, first: false);
                }
            }
            catch (Exception ex)
            {
                EngineLog.Error("Command \"" + command + "\" from the keyboard hook threw an exception.", ex);
            }
            finally
            {
                _running = false;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardHookStruct
        {
            public uint VirtualKeyCode;
            public uint ScanCode;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

        private const uint DoNotChangeKeyboardState = 0x04;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr module, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? moduleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint threadId);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int virtualKey);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ToUnicodeEx(uint virtualKey, uint scanCode, byte[] keyState, StringBuilder buffer, int bufferSize, uint flags, IntPtr layout);
    }
}
