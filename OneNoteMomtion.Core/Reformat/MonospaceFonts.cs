using System;
using System.Collections.Generic;

namespace OneNoteMomtion.Core.Reformat
{
    public static class MonospaceFonts
    {
        private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
        {
            "monospace",
            "Consolas",
            "Courier",
            "Courier New",
            "Cascadia Code",
            "Cascadia Mono",
            "Lucida Console",
            "Lucida Sans Typewriter",
            "Menlo",
            "Monaco",
            "Fira Code",
            "Fira Mono",
            "JetBrains Mono",
            "Source Code Pro",
            "DejaVu Sans Mono",
            "Liberation Mono",
            "IBM Plex Mono",
            "SF Mono",
            "Roboto Mono",
            "Ubuntu Mono",
            "Andale Mono",
        };

        public static bool IsMonospace(string? fontFamily)
        {
            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                return false;
            }

            foreach (string candidate in fontFamily!.Split(','))
            {
                string name = candidate.Trim().Trim('\'', '"');

                if (name.Length > 0)
                {
                    return Names.Contains(name);
                }
            }

            return false;
        }
    }
}