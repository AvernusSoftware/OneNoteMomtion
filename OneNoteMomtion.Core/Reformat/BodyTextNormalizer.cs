using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public static class BodyTextNormalizer
    {
        public static int Apply(PageDocument page, StyleCatalog catalog, BodyTextOptions options, ISet<XElement> skip)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(skip);

            int changed = 0;

            foreach (XElement outlineElement in page.AllOutlineElements().ToList())
            {
                if (skip.Contains(outlineElement) || !outlineElement.Elements(page.One + "T").Any())
                {
                    continue;
                }

                if (HeadingNormalizer.HeadingLevelOf(page, outlineElement) > 0 || OeStyleReader.IsHeading(page, outlineElement, catalog))
                {
                    continue;
                }

                StyleDefinition? style = ResolveStyle(page, catalog, options, outlineElement);

                if (style == null || AlreadyReadsThatWay(page, outlineElement, style))
                {
                    continue;
                }

                if (OeStyleWriter.NeedsParagraphStyle(outlineElement, style, page.One, RunFormattingMode.Preserve))
                {
                    OeStyleWriter.ApplyParagraphStyle(outlineElement, style, page.One, RunFormattingMode.Preserve);
                    changed++;
                }
            }

            return changed;
        }

        public static StyleDefinition? ResolveStyle(PageDocument page, StyleCatalog catalog, BodyTextOptions options, XElement outlineElement)
        {
            string? font = string.IsNullOrWhiteSpace(options.Font) ? null : options.Font;
            double? size = options.FontSize;
            string? color = string.IsNullOrWhiteSpace(options.FontColor) ? null : options.FontColor;

            if (font == null || !size.HasValue)
            {
                XElement? definition = BodyQuickStyle.Find(page, outlineElement, catalog);

                font ??= NullIfBlank((string?)definition?.Attribute("font"));

                if (!size.HasValue && CssProperties.TryParsePoints((string?)definition?.Attribute("fontSize"), out double points))
                {
                    size = points;
                }
            }

            if (font == null && !size.HasValue && color == null)
            {
                return null;
            }

            return new StyleDefinition { Font = font, FontSize = size, FontColor = color };
        }

        private static bool AlreadyReadsThatWay(PageDocument page, XElement outlineElement, StyleDefinition style)
        {
            Dictionary<string, string> effective = QuickStyleProperties.Effective(page, outlineElement);
            Dictionary<string, string> wanted = CssProperties.Parse(style.ToCss());

            foreach (KeyValuePair<string, string> property in wanted)
            {
                if (!effective.TryGetValue(property.Key, out string? actual) || !CssProperties.ValuesMatch(property.Key, property.Value, actual))
                {
                    return false;
                }
            }

            return true;
        }

        private static string? NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
