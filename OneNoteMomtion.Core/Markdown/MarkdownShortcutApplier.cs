using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace OneNoteMomtion.Core.Markdown
{
    public sealed class MarkdownShortcutApplier
    {
        private readonly StyleCatalog _catalog;

        public MarkdownShortcutApplier(StyleCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public int Apply(PageDocument page, bool selectionOnly = true)
        {
            ArgumentNullException.ThrowIfNull(page);

            IEnumerable<XElement> candidates = selectionOnly ? page.SelectedOutlineElements() : [.. page.AllOutlineElements()];

            int converted = 0;
            foreach (XElement outlineElement in candidates)
            {
                if (ApplyToOutlineElement(page, outlineElement))
                {
                    converted++;
                }
            }

            return converted;
        }

        public bool ApplyToOutlineElement(PageDocument page, XElement outlineElement)
        {
            ArgumentNullException.ThrowIfNull(page);

            if (outlineElement == null)
            {
                return false;
            }

            List<XElement> runs = [.. outlineElement.Elements(page.One + "T")];
            if (runs.Count == 0)
            {
                return false;
            }

            string plainText = ConcatPlainText(runs);

            if (!MarkdownShortcut.TryParse(plainText, out MarkdownBlockKind kind, out int prefixLength))
            {
                return false;
            }

            string? styleName = MarkdownShortcut.StyleNameFor(kind);
            StyleDefinition? style = _catalog.Get(styleName);
            if (style == null)
            {
                return false;
            }

            int remaining = prefixLength;
            foreach (XElement run in runs)
            {
                if (remaining <= 0)
                {
                    break;
                }

                string html = PageDocument.GetRunHtml(run);
                string trimmed = HtmlRunRewriter.RemoveLeadingCharacters(html, ref remaining);
                PageDocument.SetRunHtml(run, trimmed);
            }

            OeStyleWriter.ApplyParagraphStyle(outlineElement, style, page.One);
            return true;
        }

        private static string ConcatPlainText(IEnumerable<XElement> runs)
        {
            StringBuilder builder = new();
            foreach (XElement run in runs)
            {
                builder.Append(HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(run)));
            }

            return builder.ToString();
        }
    }
}