using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public static class HeadingNormalizer
    {
        public static int Apply(PageDocument page, StyleCatalog catalog, ISet<XElement> skip)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentNullException.ThrowIfNull(skip);

            int changed = 0;

            foreach (XElement outlineElement in page.AllOutlineElements().ToList())
            {
                if (skip.Contains(outlineElement) || !outlineElement.Elements(page.One + "T").Any())
                {
                    continue;
                }

                StyleDefinition? style = WantedStyle(page, catalog, outlineElement);

                if (style == null || !OeStyleWriter.NeedsParagraphStyle(outlineElement, style, page.One, RunFormattingMode.Preserve))
                {
                    continue;
                }

                OeStyleWriter.ApplyParagraphStyle(outlineElement, style, page.One, RunFormattingMode.Preserve);
                changed++;
            }

            return changed;
        }

        private static StyleDefinition? WantedStyle(PageDocument page, StyleCatalog catalog, XElement outlineElement)
        {
            int level = HeadingLevelOf(page, outlineElement);
            if (level == 0)
            {
                return null;
            }

            string? name = StyleNames.OfHeadingLevel(level);

            return name == null ? null : catalog.Get(name);
        }

        public static int HeadingLevelOf(PageDocument page, XElement outlineElement)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(outlineElement);

            XElement? definition = page.FindQuickStyleByIndex((string?)outlineElement.Attribute("quickStyleIndex"));
            string? name = (string?)definition?.Attribute("name");

            if (name == null || name.Length != 2 || (name[0] != 'h' && name[0] != 'H') || name[1] < '1' || name[1] > '6')
            {
                return 0;
            }

            return name[1] - '0';
        }
    }
}
