using System;
using System.Collections.Generic;
using System.Xml.Linq;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Xml
{
    public static class BodyQuickStyle
    {
        public static XElement? Find(PageDocument page, XElement? near, StyleCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(catalog);

            List<XElement> candidates = [];

            foreach (XElement definition in page.QuickStyleDefinitions())
            {
                if (IsBodyDefinition(definition) && !IsSizedLikeAHeading(definition, catalog))
                {
                    candidates.Add(definition);
                }
            }

            return PageDocument.ChooseQuickStyle(candidates, near) ?? page.FindQuickStyleByName(PageDocument.NormalQuickStyleName, near);
        }

        private static bool IsBodyDefinition(XElement definition)
        {
            return string.Equals((string?)definition.Attribute("name"), PageDocument.NormalQuickStyleName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSizedLikeAHeading(XElement definition, StyleCatalog catalog)
        {
            if (!CssProperties.TryParsePoints((string?)definition.Attribute("fontSize"), out double points))
            {
                return false;
            }

            foreach (StyleDefinition style in catalog.Styles.Values)
            {
                if (style?.FontSize != null && Math.Abs(style.FontSize.Value - points) < 0.05)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
