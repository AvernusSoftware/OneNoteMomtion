using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace OneNoteMomtion.Core.Xml
{
    public static class QuickStyleProperties
    {
        public static readonly string[] Names = ["font-family", "font-size", "color", "background-color", "font-weight", "font-style"];

        public static readonly string[] RunNames = ["font-family", "font-size"];

        public static Dictionary<string, string> OfDefinition(XElement? definition)
        {
            Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase);

            if (definition == null)
            {
                return properties;
            }

            AddIfPresent(properties, "font-family", Unquote((string?)definition.Attribute("font")));
            AddIfPresent(properties, "font-size", PointsOrNull((string?)definition.Attribute("fontSize")));
            AddIfPresent(properties, "color", ColourOrNull((string?)definition.Attribute("fontColor")));
            AddIfPresent(properties, "background-color", ColourOrNull((string?)definition.Attribute("highlightColor")));

            if (IsTrue((string?)definition.Attribute("bold")))
            {
                properties["font-weight"] = "bold";
            }

            if (IsTrue((string?)definition.Attribute("italic")))
            {
                properties["font-style"] = "italic";
            }

            return properties;
        }

        public static Dictionary<string, string> OfRuns(PageDocument page, XElement outlineElement)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(outlineElement);

            Dictionary<string, string> common = new(StringComparer.OrdinalIgnoreCase);
            List<Dictionary<string, string>> runs = [.. outlineElement.Elements(page.One + "T").Select(OfRun)];

            if (runs.Count == 0)
            {
                return common;
            }

            foreach (string name in RunNames)
            {
                if (TryGetAgreedValue(runs, name, out string? value))
                {
                    common[name] = value!;
                }
            }

            return common;
        }

        public static Dictionary<string, string> Effective(PageDocument page, XElement outlineElement)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(outlineElement);

            Dictionary<string, string> effective = OfDefinition(page.FindQuickStyleByIndex((string?)outlineElement.Attribute("quickStyleIndex")));

            foreach (KeyValuePair<string, string> property in CssProperties.Parse((string?)outlineElement.Attribute("style")))
            {
                effective[property.Key] = property.Value;
            }

            foreach (KeyValuePair<string, string> property in OfRuns(page, outlineElement))
            {
                effective[property.Key] = property.Value;
            }

            return effective;
        }

        private static Dictionary<string, string> OfRun(XElement run)
        {
            Dictionary<string, string> properties = CssProperties.Parse((string?)run.Attribute("style"));

            foreach (KeyValuePair<string, string> property in RunHtmlFonts.Common(PageDocument.GetRunHtml(run)))
            {
                properties[property.Key] = property.Value;
            }

            return properties;
        }

        private static bool TryGetAgreedValue(List<Dictionary<string, string>> runs, string name, out string? value)
        {
            value = null;

            foreach (Dictionary<string, string> run in runs)
            {
                if (!run.TryGetValue(name, out string? candidate))
                {
                    value = null;
                    return false;
                }

                if (value == null)
                {
                    value = candidate;
                    continue;
                }

                if (!CssProperties.ValuesMatch(name, value, candidate))
                {
                    value = null;
                    return false;
                }
            }

            return value != null;
        }

        public static bool Match(Dictionary<string, string> left, Dictionary<string, string> right)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            foreach (string name in Names)
            {
                bool inLeft = left.TryGetValue(name, out string? leftValue);
                bool inRight = right.TryGetValue(name, out string? rightValue);

                if (inLeft != inRight)
                {
                    return false;
                }

                if (inLeft && !CssProperties.ValuesMatch(name, leftValue!, rightValue!))
                {
                    return false;
                }
            }

            return true;
        }

        public static string Unquote(string? value)
        {
            return value == null ? string.Empty : value.Trim().Trim('\'', '"');
        }

        private static void AddIfPresent(Dictionary<string, string> properties, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                properties[name] = value!;
            }
        }

        private static string? PointsOrNull(string? fontSize)
        {
            return string.IsNullOrWhiteSpace(fontSize) ? null : fontSize!.Trim() + "pt";
        }

        private static string? ColourOrNull(string? colour)
        {
            if (string.IsNullOrWhiteSpace(colour) || string.Equals(colour!.Trim(), "automatic", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return colour.Trim();
        }

        private static bool IsTrue(string? value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
