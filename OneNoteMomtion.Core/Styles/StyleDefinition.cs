using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace OneNoteMomtion.Core.Styles
{
    public sealed class StyleDefinition
    {
        public string? DisplayName { get; set; }

        public string? Font { get; set; }

        public double? FontSize { get; set; }

        public string? FontColor { get; set; }

        public string? HighlightColor { get; set; }

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public double? SpaceBefore { get; set; }

        public double? SpaceAfter { get; set; }

        [JsonPropertyName("fontColorLight")]
        public string? LegacyFontColorLight { get; set; }

        [JsonPropertyName("highlightColorLight")]
        public string? LegacyHighlightColorLight { get; set; }

        public void MigrateLegacyColors()
        {
            if (string.IsNullOrWhiteSpace(FontColor))
            {
                FontColor = LegacyFontColorLight;
            }

            if (string.IsNullOrWhiteSpace(HighlightColor))
            {
                HighlightColor = LegacyHighlightColorLight;
            }

            LegacyFontColorLight = null;
            LegacyHighlightColorLight = null;
        }

        public string ToCss()
        {
            List<string> parts = new(6);

            if (!string.IsNullOrWhiteSpace(Font))
            {
                parts.Add("font-family:" + Font);
            }

            if (FontSize.HasValue)
            {
                parts.Add("font-size:" + FormatPoints(FontSize.Value) + "pt");
            }

            if (!string.IsNullOrWhiteSpace(FontColor))
            {
                parts.Add("color:" + FontColor);
            }

            if (!string.IsNullOrWhiteSpace(HighlightColor))
            {
                parts.Add("background-color:" + HighlightColor);
            }

            if (Bold)
            {
                parts.Add("font-weight:bold");
            }

            if (Italic)
            {
                parts.Add("font-style:italic");
            }

            return string.Join(";", parts.ToArray());
        }

        public static string FormatPoints(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
