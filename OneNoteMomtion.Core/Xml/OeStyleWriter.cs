using System;
using System.Collections.Generic;
using System.Xml.Linq;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Xml
{
    public static class OeStyleWriter
    {
        private static readonly string[] CompetingRunProperties =
        [
            "font-family",
            "font-size",
            "color",
            "background-color",
        ];

        public static bool ApplyParagraphStyle(XElement outlineElement, StyleDefinition style, XNamespace one)
        {
            return ApplyParagraphStyle(outlineElement, style, one, RunFormattingMode.Strip);
        }

        public static bool ApplyParagraphStyle(XElement outlineElement, StyleDefinition style, XNamespace one, RunFormattingMode mode)
        {
            ArgumentNullException.ThrowIfNull(outlineElement);

            ArgumentNullException.ThrowIfNull(style);

            bool changed = SetOrRemoveAttribute(outlineElement, "style", NullIfEmpty(style.ToCss()));
            changed |= SetOrRemoveAttribute(outlineElement, "quickStyleIndex", null);
            changed |= SetOrRemoveAttribute(outlineElement, "spaceBefore", style.SpaceBefore.HasValue ? StyleDefinition.FormatPoints(style.SpaceBefore.Value) : null);
            changed |= SetOrRemoveAttribute(outlineElement, "spaceAfter", style.SpaceAfter.HasValue ? StyleDefinition.FormatPoints(style.SpaceAfter.Value) : null);
            changed |= mode == RunFormattingMode.Preserve ? ClearCompetingRunFormatting(outlineElement, one) : StripRunFormatting(outlineElement, one);

            return changed;
        }

        public static bool NeedsParagraphStyle(XElement outlineElement, StyleDefinition style, XNamespace one, RunFormattingMode mode)
        {
            ArgumentNullException.ThrowIfNull(outlineElement);

            ArgumentNullException.ThrowIfNull(style);

            if (!string.Equals((string?)outlineElement.Attribute("style"), NullIfEmpty(style.ToCss()), StringComparison.Ordinal))
            {
                return true;
            }

            if (!SpacingMatches(outlineElement, "spaceBefore", style.SpaceBefore) || !SpacingMatches(outlineElement, "spaceAfter", style.SpaceAfter))
            {
                return true;
            }

            foreach (XElement run in outlineElement.Elements(one + "T"))
            {
                if (run.Attribute("style") != null)
                {
                    return true;
                }

                string html = PageDocument.GetRunHtml(run);

                if (CodeBlockLink.RunContainsCopyLink(html))
                {
                    continue;
                }

                string rewritten = mode == RunFormattingMode.Preserve ? HtmlRunRewriter.RemoveEmptySpans(HtmlRunRewriter.RemoveStyleProperties(html, CompetingRunProperties)) : HtmlRunRewriter.StripTags(html);

                if (!string.Equals(html, rewritten, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SpacingMatches(XElement outlineElement, string name, double? points)
        {
            string? wanted = points.HasValue ? StyleDefinition.FormatPoints(points.Value) : null;

            return string.Equals((string?)outlineElement.Attribute(name), wanted, StringComparison.Ordinal);
        }

        public static bool ClearCompetingRunFormatting(XElement outlineElement, XNamespace one)
        {
            ArgumentNullException.ThrowIfNull(outlineElement);

            bool changed = false;

            foreach (XElement run in outlineElement.Elements(one + "T"))
            {
                if (run.Attribute("style") != null)
                {
                    run.Attributes("style").Remove();
                    changed = true;
                }

                string html = PageDocument.GetRunHtml(run);

                if (CodeBlockLink.RunContainsCopyLink(html))
                {
                    continue;
                }

                string rewritten = HtmlRunRewriter.RemoveEmptySpans(HtmlRunRewriter.RemoveStyleProperties(html, CompetingRunProperties));

                if (!string.Equals(html, rewritten, StringComparison.Ordinal))
                {
                    PageDocument.SetRunHtml(run, rewritten);
                    changed = true;
                }
            }

            return changed;
        }

        public static bool ApplyBuiltInNormal(PageDocument page, XElement outlineElement, StyleCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(outlineElement);

            bool changed = SetOrRemoveAttribute(outlineElement, "style", null);
            changed |= SetOrRemoveAttribute(outlineElement, "spaceBefore", null);
            changed |= SetOrRemoveAttribute(outlineElement, "spaceAfter", null);

            XElement? normalQuickStyle = BodyQuickStyle.Find(page, outlineElement, catalog);
            changed |= SetOrRemoveAttribute(outlineElement, "quickStyleIndex", normalQuickStyle == null ? null : (string?)normalQuickStyle.Attribute("index"));
            changed |= StripRunFormatting(outlineElement, page.One);

            return changed;
        }

        public static int ApplyBuiltInNormalToSelection(PageDocument page, StyleCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(page);

            int changed = 0;

            foreach (XElement outlineElement in page.SelectedOutlineElements())
            {
                if (ApplyBuiltInNormal(page, outlineElement, catalog))
                {
                    changed++;
                }
            }

            return changed;
        }

        private static bool StripRunFormatting(XElement outlineElement, XNamespace one)
        {
            bool changed = false;

            foreach (XElement run in outlineElement.Elements(one + "T"))
            {
                if (run.Attribute("style") != null)
                {
                    run.Attributes("style").Remove();
                    changed = true;
                }

                string html = PageDocument.GetRunHtml(run);
                string stripped = HtmlRunRewriter.StripTags(html);
                if (!string.Equals(html, stripped, StringComparison.Ordinal))
                {
                    PageDocument.SetRunHtml(run, stripped);
                    changed = true;
                }
            }

            return changed;
        }

        public static int ApplyToSelection(PageDocument page, StyleDefinition style)
        {
            ArgumentNullException.ThrowIfNull(page);

            int changed = 0;

            foreach (XElement outlineElement in page.SelectedOutlineElements())
            {
                if (ApplyParagraphStyle(outlineElement, style, page.One))
                {
                    changed++;
                }
            }

            return changed;
        }

        public static XElement CreateOutlineElement(XNamespace one, string? html, string? css, IDictionary<string, string>? extraAttributes = null, string? lang = null)
        {
            XElement outlineElement = new(one + "OE");

            if (!string.IsNullOrEmpty(css))
            {
                outlineElement.SetAttributeValue("style", css);
            }

            if (extraAttributes != null)
            {
                foreach (KeyValuePair<string, string> attribute in extraAttributes)
                {
                    outlineElement.SetAttributeValue(attribute.Key, attribute.Value);
                }
            }

            if (!string.IsNullOrEmpty(lang))
            {
                outlineElement.SetAttributeValue("lang", lang);
            }

            XElement textRun = new(one + "T", new XCData(html ?? string.Empty));
            if (!string.IsNullOrEmpty(lang))
            {
                textRun.SetAttributeValue("lang", lang);
            }

            outlineElement.Add(textRun);
            return outlineElement;
        }

        private static bool SetOrRemoveAttribute(XElement element, string name, string? value)
        {
            XAttribute? existing = element.Attribute(name);
            string? current = existing?.Value;

            if (string.Equals(current, value, StringComparison.Ordinal))
            {
                return false;
            }

            element.SetAttributeValue(name, value);
            return true;
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
