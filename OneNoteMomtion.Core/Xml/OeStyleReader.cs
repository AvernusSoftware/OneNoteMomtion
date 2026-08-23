using System;
using System.Collections.Generic;
using System.Xml.Linq;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Xml
{
    public static class OeStyleReader
    {
        private static readonly string[] HeadingStyleNames =
        [
            StyleNames.Heading1,
            StyleNames.Heading2,
            StyleNames.Heading3,
        ];

        private static readonly string[] IdentifyingProperties =
        [
            "font-family",
            "font-size",
        ];

        public static bool IsHeading(PageDocument page, XElement outlineElement, StyleCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(page);

            ArgumentNullException.ThrowIfNull(catalog);

            if (outlineElement == null)
            {
                return false;
            }

            double? normalFontSize = BuiltInNormalFontSize(page, outlineElement);

            foreach (string name in HeadingStyleNames)
            {
                StyleDefinition? heading = catalog.Get(name);
                if (heading != null && DiffersInSizeFrom(heading, normalFontSize) && MatchesStyle(page, outlineElement, heading))
                {
                    return true;
                }
            }

            return HasBuiltInHeadingQuickStyle(page, outlineElement);
        }

        private static double? BuiltInNormalFontSize(PageDocument page, XElement outlineElement)
        {
            XElement? definition = page.FindQuickStyleByName(PageDocument.NormalQuickStyleName, outlineElement);
            if (definition == null)
            {
                return null;
            }

            return CssProperties.TryParsePoints((string?)definition.Attribute("fontSize"), out double points) ? points : (double?)null;
        }

        private static bool DiffersInSizeFrom(StyleDefinition heading, double? normalFontSize)
        {
            if (!heading.FontSize.HasValue)
            {
                return false;
            }

            if (!normalFontSize.HasValue)
            {
                return true;
            }

            return Math.Abs(normalFontSize.Value - heading.FontSize.Value) >= 0.05;
        }

        public static bool IsEmpty(PageDocument page, XElement outlineElement)
        {
            ArgumentNullException.ThrowIfNull(page);

            if (outlineElement == null)
            {
                return false;
            }

            foreach (XElement run in outlineElement.Elements(page.One + "T"))
            {
                if (HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(run)).Length > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesStyle(PageDocument page, XElement outlineElement, StyleDefinition style)
        {
            return MatchesFontOf(QuickStyleProperties.Effective(page, outlineElement), style);
        }

        public static bool MatchesFontOf(Dictionary<string, string> actual, StyleDefinition style)
        {
            ArgumentNullException.ThrowIfNull(actual);

            ArgumentNullException.ThrowIfNull(style);

            Dictionary<string, string> expected = CssProperties.Parse(style.ToCss());

            foreach (string name in IdentifyingProperties)
            {
                if (!expected.TryGetValue(name, out string? wanted))
                {
                    continue;
                }

                if (!actual.TryGetValue(name, out string? value) || !CssProperties.ValuesMatch(name, wanted, value))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasBuiltInHeadingQuickStyle(PageDocument page, XElement outlineElement)
        {
            XElement? definition = page.FindQuickStyleByIndex((string?)outlineElement.Attribute("quickStyleIndex"));

            return definition != null && IsHeadingQuickStyleName((string?)definition.Attribute("name"));
        }

        private static bool IsHeadingQuickStyleName(string? name)
        {
            return name != null && name.Length == 2 && (name[0] == 'h' || name[0] == 'H') && name[1] >= '1' && name[1] <= '6';
        }
    }
}
