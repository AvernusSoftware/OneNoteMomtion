using System;
using System.Collections.Generic;

namespace OneNoteMomtion.Core.Input
{
    public readonly record struct HotkeyCombo(HotkeyModifiers Modifiers, uint VirtualKey)
    {
        private static readonly Dictionary<string, HotkeyModifiers> ModifierNames = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Ctrl", HotkeyModifiers.Control },
            { "Control", HotkeyModifiers.Control },
            { "Alt", HotkeyModifiers.Alt },
            { "Shift", HotkeyModifiers.Shift },
            { "Win", HotkeyModifiers.Win },
            { "Windows", HotkeyModifiers.Win },
        };

        private static readonly Dictionary<string, uint> KeyNames = BuildKeyNames();

        public static bool TryParse(string? text, out HotkeyCombo combo)
        {
            combo = default;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            HotkeyModifiers modifiers = HotkeyModifiers.None;
            uint? virtualKey = null;

            foreach (string part in parts)
            {
                if (ModifierNames.TryGetValue(part, out HotkeyModifiers modifier))
                {
                    modifiers |= modifier;
                    continue;
                }

                if (virtualKey != null)
                {
                    return false;
                }

                if (!KeyNames.TryGetValue(part, out uint key))
                {
                    return false;
                }

                virtualKey = key;
            }

            if (virtualKey == null || modifiers == HotkeyModifiers.None)
            {
                return false;
            }

            combo = new HotkeyCombo(modifiers, virtualKey.Value);
            return true;
        }

        public override string ToString()
        {
            List<string> parts = new(5);

            if (Modifiers.HasFlag(HotkeyModifiers.Control))
            {
                parts.Add("Ctrl");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Alt))
            {
                parts.Add("Alt");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Shift))
            {
                parts.Add("Shift");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Win))
            {
                parts.Add("Win");
            }

            foreach (KeyValuePair<string, uint> pair in KeyNames)
            {
                if (pair.Value == VirtualKey)
                {
                    parts.Add(pair.Key);
                    break;
                }
            }

            return string.Join("+", parts);
        }

        private static Dictionary<string, uint> BuildKeyNames()
        {
            Dictionary<string, uint> keys = new(StringComparer.OrdinalIgnoreCase)
            {
                { "Space", 0x20 },
                { "Tab", 0x09 },
                { "Enter", 0x0D },
                { "Return", 0x0D },
                { "Escape", 0x1B },
                { "Esc", 0x1B },
                { "Backspace", 0x08 },
                { "Insert", 0x2D },
                { "Ins", 0x2D },
                { "Delete", 0x2E },
                { "Del", 0x2E },
                { "Home", 0x24 },
                { "End", 0x23 },
                { "PageUp", 0x21 },
                { "PgUp", 0x21 },
                { "PageDown", 0x22 },
                { "PgDn", 0x22 },
                { "Up", 0x26 },
                { "Down", 0x28 },
                { "Left", 0x25 },
                { "Right", 0x27 },
            };

            for (char c = '0'; c <= '9'; c++)
            {
                keys[c.ToString()] = c;
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                keys[c.ToString()] = c;
            }

            for (int i = 1; i <= 24; i++)
            {
                keys["F" + i] = (uint)(0x70 + i - 1);
            }

            return keys;
        }
    }
}
