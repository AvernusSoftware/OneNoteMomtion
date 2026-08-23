using System;
using System.Globalization;
using Microsoft.Win32;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Server.OneNote
{
    internal static class OneNoteDefaultFont
    {
        private static readonly string[] OfficeVersions = ["16.0", "15.0"];

        private const string EditingKeyFormat = @"SOFTWARE\Microsoft\Office\{0}\OneNote\Options\Editing";

        private const long SystemColorFlag = 0x80000000L;

        public static BodyTextOptions Read()
        {
            foreach (string version in OfficeVersions)
            {
                try
                {
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(string.Format(CultureInfo.InvariantCulture, EditingKeyFormat, version));

                    if (key == null)
                    {
                        continue;
                    }

                    BodyTextOptions options = ReadFrom(key);

                    if (options.HasAnyValue)
                    {
                        return options;
                    }
                }
                catch (Exception ex)
                {
                    EngineLog.Warn("The OneNote default font could not be read for Office " + version + ": " + ex.Message);
                }
            }

            return new BodyTextOptions();
        }

        private static BodyTextOptions ReadFrom(RegistryKey key)
        {
            BodyTextOptions options = new();

            foreach (string name in key.GetValueNames())
            {
                object? value = key.GetValue(name);

                if (value == null)
                {
                    continue;
                }

                if (Mentions(name, "fontface") || Mentions(name, "fontname"))
                {
                    options.Font ??= NullIfBlank(value as string);
                }
                else if (Mentions(name, "fontsize"))
                {
                    options.FontSize ??= ParseSize(value);
                }
                else if (Mentions(name, "fontcolor"))
                {
                    options.FontColor ??= ParseColor(value);
                }
            }

            return options;
        }

        private static bool Mentions(string name, string fragment)
        {
            return name.Contains(fragment, StringComparison.OrdinalIgnoreCase);
        }

        private static double? ParseSize(object value)
        {
            double points;

            if (value is int number)
            {
                points = number;
            }
            else if (value is string text)
            {
                if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out points))
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            return points <= 0.0 ? null : points;
        }

        private static string? ParseColor(object value)
        {
            if (value is string text)
            {
                string trimmed = text.Trim();
                return trimmed.StartsWith('#') && trimmed.Length == 7 ? trimmed.ToUpperInvariant() : null;
            }

            if (value is not int number)
            {
                return null;
            }

            long raw = (uint)number;

            if (raw >= SystemColorFlag)
            {
                return null;
            }

            long red = raw & 0xFF;
            long green = (raw >> 8) & 0xFF;
            long blue = (raw >> 16) & 0xFF;

            return "#" + red.ToString("X2", CultureInfo.InvariantCulture) + green.ToString("X2", CultureInfo.InvariantCulture) + blue.ToString("X2", CultureInfo.InvariantCulture);
        }

        private static string? NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
