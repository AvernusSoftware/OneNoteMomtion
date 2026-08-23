using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Xml
{
    public static partial class CssProperties
    {
        private static readonly Regex HexColorPattern = HexColorPatternRegex();

        private static readonly Dictionary<string, string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
        {
            ["black"] = "#000000",
            ["white"] = "#FFFFFF",
            ["red"] = "#FF0000",
            ["green"] = "#008000",
            ["blue"] = "#0000FF",
            ["yellow"] = "#FFFF00",
            ["cyan"] = "#00FFFF",
            ["aqua"] = "#00FFFF",
            ["magenta"] = "#FF00FF",
            ["fuchsia"] = "#FF00FF",
            ["gray"] = "#808080",
            ["grey"] = "#808080",
            ["silver"] = "#C0C0C0",
            ["maroon"] = "#800000",
            ["olive"] = "#808000",
            ["lime"] = "#00FF00",
            ["teal"] = "#008080",
            ["navy"] = "#000080",
            ["purple"] = "#800080",
            ["orange"] = "#FFA500",
        };

        public static string NormalizeQuickStyleColor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "automatic";
            }

            string trimmed = value.Trim();

            if (string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "automatic", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.ToLowerInvariant();
            }

            if (HexColorPattern.IsMatch(trimmed))
            {
                return trimmed.ToUpperInvariant();
            }

            return NamedColors.TryGetValue(trimmed, out string? hex) ? hex : "automatic";
        }

        public static Dictionary<string, string> Parse(string? css)
        {
            Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(css))
            {
                return properties;
            }

            foreach (string declaration in css!.Split(';'))
            {
                int separator = declaration.IndexOf(':');
                if (separator < 1)
                {
                    continue;
                }

                string name = declaration[..separator].Trim();
                if (name.Length > 0)
                {
                    properties[name] = declaration[(separator + 1)..].Trim();
                }
            }

            return properties;
        }

        public static string Format(IEnumerable<KeyValuePair<string, string>> properties)
        {
            ArgumentNullException.ThrowIfNull(properties);

            StringBuilder builder = new();

            foreach (KeyValuePair<string, string> property in properties)
            {
                if (builder.Length > 0)
                {
                    builder.Append(';');
                }

                builder.Append(property.Key).Append(':').Append(property.Value);
            }

            return builder.ToString();
        }

        public static bool ValuesMatch(string name, string expected, string actual)
        {
            if (string.Equals(name, "font-size", StringComparison.Ordinal))
            {
                return TryParsePoints(expected, out double expectedPoints) && TryParsePoints(actual, out double actualPoints) && Math.Abs(expectedPoints - actualPoints) < 0.05;
            }

            return string.Equals(Normalize(expected), Normalize(actual), StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryParsePoints(string? value, out double points)
        {
            points = 0.0;

            if (value == null)
            {
                return false;
            }

            string trimmed = value.Trim();
            if (trimmed.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^2];
            }

            return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out points);
        }

        private static string Normalize(string value)
        {
            return value.Trim().Trim('\'', '"');
        }

        [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled)]
        private static partial Regex HexColorPatternRegex();
    }
}