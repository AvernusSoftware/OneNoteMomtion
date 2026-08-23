using System;
using System.Collections.Generic;
using System.Xml.Linq;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public static class PastedFormattingCleaner
    {
        private static readonly string[] PastedProperties =
        [
            "background-color",
        ];

        public static int Apply(PageDocument page, ISet<XElement> skip)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(skip);

            int changed = 0;

            foreach (XElement outlineElement in page.AllOutlineElements())
            {
                if (skip.Contains(outlineElement))
                {
                    continue;
                }

                foreach (XElement run in outlineElement.Elements(page.One + "T"))
                {
                    string html = PageDocument.GetRunHtml(run);

                    if (CodeBlockLink.RunContainsCopyLink(html))
                    {
                        continue;
                    }

                    string rewritten = HtmlRunRewriter.RemoveEmptySpans(HtmlRunRewriter.RemoveStyleProperties(html, PastedProperties));

                    if (!string.Equals(html, rewritten, StringComparison.Ordinal))
                    {
                        PageDocument.SetRunHtml(run, rewritten);
                        changed++;
                    }
                }
            }

            return changed;
        }
    }
}
