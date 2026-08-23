using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OneNoteMomtion.Server.OneNote;

namespace OneNoteMomtion.Server.Input
{
    internal sealed class CaretRestorer
    {
        public static readonly IntPtr InjectedMarker = new(0x4D4F4D54);

        private const uint InputKeyboard = 1;
        private const uint KeyEventExtendedKey = 0x0001;
        private const uint KeyEventKeyUp = 0x0002;

        private readonly ForegroundMonitor _foreground;

        public CaretRestorer(ForegroundMonitor foreground)
        {
            _foreground = foreground ?? throw new ArgumentNullException(nameof(foreground));
        }

        public void PlaceAtStartOf(OneNoteSession session, string pageId, string objectId)
        {
            ArgumentNullException.ThrowIfNull(session);

            if (!_foreground.HostInForeground)
            {
                return;
            }

            session.NavigateTo(pageId, objectId);
            PressArrow(Keys.Left);
        }

        public void MakeCaretVisible()
        {
            if (!_foreground.HostInForeground)
            {
                return;
            }

            PressArrow(Keys.Left);
            PressArrow(Keys.Right);
        }

        private static void PressArrow(Keys key)
        {
            Input[] events = new Input[2];
            events[0].Type = InputKeyboard;
            events[0].Union.Keyboard.VirtualKey = (ushort)key;
            events[0].Union.Keyboard.Flags = KeyEventExtendedKey;
            events[0].Union.Keyboard.ExtraInfo = InjectedMarker;

            events[1] = events[0];
            events[1].Union.Keyboard.Flags = KeyEventExtendedKey | KeyEventKeyUp;

            uint sent = SendInput((uint)events.Length, events, Marshal.SizeOf<Input>());
            if (sent != events.Length)
            {
                EngineLog.Warn($"Failed to inject the caret keystroke (error {Marshal.GetLastWin32Error()}) - the caret may stay invisible until the next keystroke.");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public ushort VirtualKey;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MouseInput Mouse;

            [FieldOffset(0)]
            public KeyboardInput Keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public uint Type;
            public InputUnion Union;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, Input[] inputs, int size);
    }
}
