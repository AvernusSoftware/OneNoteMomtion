using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Xml
{
    public static class QuickStyleReconciler
    {
        public static int Reconcile(PageDocument page, StyleCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(page);

            ArgumentNullException.ThrowIfNull(catalog);

            int reconciled = 0;

            foreach (XElement outlineElement in page.AllOutlineElements().ToList())
            {
                if (ReconcileOutlineElement(page, outlineElement, catalog))
                {
                    reconciled++;
                }
            }

            return reconciled;
        }

        private static bool ReconcileOutlineElement(PageDocument page, XElement outlineElement, StyleCatalog catalog)
        {
            Dictionary<string, string> inline = CssProperties.Parse((string?)outlineElement.Attribute("style"));
            Dictionary<string, string> runs = QuickStyleProperties.OfRuns(page, outlineElement);

            if (!CarriesQuickStyleProperty(inline) && !CarriesQuickStyleProperty(runs))
            {
                return false;
            }

            XElement? baseDefinition = page.FindQuickStyleByIndex((string?)outlineElement.Attribute("quickStyleIndex"));

            Dictionary<string, string> effective = QuickStyleProperties.OfDefinition(baseDefinition);

            Overlay(effective, inline);
            Overlay(effective, runs);
            NormalizeColors(effective);

            string name = (string?)baseDefinition?.Attribute("name") ?? HeadingNameOf(effective, catalog) ?? PageDocument.NormalQuickStyleName;
            XElement definition = FindOrCreateDefinition(page, name, effective, baseDefinition);

            string? index = (string?)definition.Attribute("index");
            if (string.Equals((string?)outlineElement.Attribute("quickStyleIndex"), index, StringComparison.Ordinal))
            {
                return false;
            }

            outlineElement.SetAttributeValue("quickStyleIndex", index);

            return true;
        }

        private static string? HeadingNameOf(Dictionary<string, string> effective, StyleCatalog catalog)
        {
            foreach (KeyValuePair<string, StyleDefinition> style in catalog.Styles)
            {
                string? name = StyleNames.QuickStyleNameOf(style.Key);

                if (name != null && style.Value?.FontSize != null && OeStyleReader.MatchesFontOf(effective, style.Value))
                {
                    return name;
                }
            }

            return null;
        }

        private static void NormalizeColors(Dictionary<string, string> effective)
        {
            if (effective.TryGetValue("color", out string? color))
            {
                effective["color"] = CssProperties.NormalizeQuickStyleColor(color);
            }

            if (effective.TryGetValue("background-color", out string? highlight))
            {
                effective["background-color"] = CssProperties.NormalizeQuickStyleColor(highlight);
            }
        }

        private static bool CarriesQuickStyleProperty(Dictionary<string, string> properties)
        {
            return properties.Keys.Any(name => QuickStyleProperties.Names.Contains(name, StringComparer.OrdinalIgnoreCase));
        }

        private static void Overlay(Dictionary<string, string> effective, Dictionary<string, string> properties)
        {
            foreach (KeyValuePair<string, string> property in properties)
            {
                if (QuickStyleProperties.Names.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
                {
                    effective[property.Key] = property.Value;
                }
            }
        }

        private static XElement FindOrCreateDefinition(PageDocument page, string name, Dictionary<string, string> effective, XElement? baseDefinition)
        {
            foreach (XElement candidate in page.QuickStyleDefinitions())
            {
                if (string.Equals((string?)candidate.Attribute("name"), name, StringComparison.OrdinalIgnoreCase) && QuickStyleProperties.Match(QuickStyleProperties.OfDefinition(candidate), effective))
                {
                    return candidate;
                }
            }

            XElement definition = new(page.One + "QuickStyleDef");

            definition.SetAttributeValue("index", NextIndex(page).ToString(CultureInfo.InvariantCulture));
            definition.SetAttributeValue("name", name);
            definition.SetAttributeValue("fontColor", CssProperties.NormalizeQuickStyleColor(effective.TryGetValue("color", out string? colour) ? colour : null));
            definition.SetAttributeValue("highlightColor", CssProperties.NormalizeQuickStyleColor(effective.TryGetValue("background-color", out string? highlight) ? highlight : null));

            if (effective.TryGetValue("font-family", out string? font))
            {
                definition.SetAttributeValue("font", QuickStyleProperties.Unquote(font));
            }

            if (effective.TryGetValue("font-size", out string? size) && CssProperties.TryParsePoints(size, out double points))
            {
                definition.SetAttributeValue("fontSize", points.ToString("0.0", CultureInfo.InvariantCulture));
            }

            if (effective.TryGetValue("font-weight", out string? weight) && string.Equals(weight, "bold", StringComparison.OrdinalIgnoreCase))
            {
                definition.SetAttributeValue("bold", "true");
            }

            if (effective.TryGetValue("font-style", out string? fontStyle) && string.Equals(fontStyle, "italic", StringComparison.OrdinalIgnoreCase))
            {
                definition.SetAttributeValue("italic", "true");
            }

            definition.SetAttributeValue("spaceBefore", (string?)baseDefinition?.Attribute("spaceBefore") ?? "0.0");
            definition.SetAttributeValue("spaceAfter", (string?)baseDefinition?.Attribute("spaceAfter") ?? "0.0");

            page.AddQuickStyleDefinition(definition);

            return definition;
        }

        private static int NextIndex(PageDocument page)
        {
            int highest = -1;

            foreach (XElement definition in page.QuickStyleDefinitions())
            {
                if (int.TryParse((string?)definition.Attribute("index"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) && index > highest)
                {
                    highest = index;
                }
            }

            return highest + 1;
        }
    }
}
