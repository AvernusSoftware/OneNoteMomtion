using System;
using System.Xml.Linq;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Spelling
{
    public static class SpellCheckDisabler
    {
        public const string NoSpellCheckLanguage = "yo";

        public static int Disable(PageDocument page)
        {
            return SetLanguage(page, NoSpellCheckLanguage);
        }

        public static int Enable(PageDocument page)
        {
            return SetLanguage(page, null);
        }

        private static int SetLanguage(PageDocument page, string? language)
        {
            ArgumentNullException.ThrowIfNull(page);

            int changed = 0;

            foreach (XElement run in page.SelectedTextRuns())
            {
                if (SetLanguageAttribute(run, language))
                {
                    changed++;
                }

                XElement? outlineElement = run.Parent;
                if (outlineElement != null && outlineElement.Name == page.One + "OE")
                {
                    if (SetLanguageAttribute(outlineElement, language))
                    {
                        changed++;
                    }

                    changed += UnhoistAncestorLanguage(page, outlineElement, language);
                }
            }

            return changed;
        }

        private static int UnhoistAncestorLanguage(PageDocument page, XElement outlineElement, string? language)
        {
            int changed = 0;
            XName oe = page.One + "OE";

            for (XElement? ancestor = outlineElement.Parent; ancestor != null && ancestor.Name != page.One + "Page"; ancestor = ancestor.Parent)
            {
                XAttribute? ancestorLang = ancestor.Attribute("lang");
                if (ancestorLang == null || string.Equals(ancestorLang.Value, language, StringComparison.Ordinal))
                {
                    continue;
                }

                string inheritedLanguage = ancestorLang.Value;

                foreach (XElement sibling in ancestor.Descendants(oe))
                {
                    if (!ReferenceEquals(sibling, outlineElement) && sibling.Attribute("lang") == null && SetLanguageAttribute(sibling, inheritedLanguage))
                    {
                        changed++;
                    }
                }

                ancestorLang.Remove();
                changed++;
            }

            return changed;
        }

        private static bool SetLanguageAttribute(XElement element, string? language)
        {
            XAttribute? existing = element.Attribute("lang");
            string? current = existing?.Value;

            if (string.Equals(current, language, StringComparison.Ordinal))
            {
                return false;
            }

            element.SetAttributeValue("lang", language);
            return true;
        }
    }
}
