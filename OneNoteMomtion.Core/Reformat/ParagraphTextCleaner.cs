using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Text;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public static class ParagraphTextCleaner
    {
        public static int Apply(PageDocument page, TextCleanupOptions options, ISet<XElement> skip)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(skip);

            if (!options.RewritesText)
            {
                return 0;
            }

            int changed = 0;

            foreach (XElement outlineElement in page.AllOutlineElements())
            {
                if (!skip.Contains(outlineElement) && CleanParagraph(page, outlineElement, options))
                {
                    changed++;
                }
            }

            return changed;
        }

        private static bool CleanParagraph(PageDocument page, XElement outlineElement, TextCleanupOptions options)
        {
            List<XElement> runs = [.. outlineElement.Elements(page.One + "T")];
            if (runs.Count == 0)
            {
                return false;
            }

            List<string> html = new(runs.Count);
            List<IReadOnlyList<string>> segments = new(runs.Count);
            int total = 0;

            foreach (XElement run in runs)
            {
                string runHtml = PageDocument.GetRunHtml(run);

                IReadOnlyList<string> runSegments = CodeBlockLink.RunContainsCopyLink(runHtml) ? [] : HtmlRunRewriter.ReadTextSegments(runHtml);

                html.Add(runHtml);
                segments.Add(runSegments);
                total += runSegments.Count;
            }

            if (total == 0)
            {
                return false;
            }

            List<string[]> rewritten = new(runs.Count);
            char? previous = null;
            int index = 0;

            foreach (IReadOnlyList<string> runSegments in segments)
            {
                string[] output = new string[runSegments.Count];

                for (int i = 0; i < runSegments.Count; i++)
                {
                    output[i] = WhitespaceTransformer.Apply(runSegments[i], options, index == 0, index == total - 1, previous);

                    if (output[i].Length > 0)
                    {
                        previous = output[i][^1];
                    }

                    index++;
                }

                rewritten.Add(output);
            }

            bool changed = false;

            for (int i = 0; i < runs.Count; i++)
            {
                if (segments[i].Count == 0)
                {
                    continue;
                }

                string updated = HtmlRunRewriter.WriteTextSegments(html[i], rewritten[i]);

                if (!string.Equals(html[i], updated, StringComparison.Ordinal))
                {
                    PageDocument.SetRunHtml(runs[i], updated);
                    changed = true;
                }
            }

            return changed;
        }
    }
}
