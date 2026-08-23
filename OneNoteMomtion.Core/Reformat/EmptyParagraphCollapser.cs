using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public static class EmptyParagraphCollapser
    {
        public static int Apply(PageDocument page, ISet<XElement> skip)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(skip);

            int removed = 0;

            foreach (XElement children in page.Document.Descendants(page.One + "OEChildren").ToList())
            {
                List<XElement> run = [];

                foreach (XElement outlineElement in children.Elements(page.One + "OE").ToList())
                {
                    if (!skip.Contains(outlineElement) && IsBlank(page, outlineElement))
                    {
                        run.Add(outlineElement);
                        continue;
                    }

                    removed += CollapseRun(run);
                    run = [];
                }

                removed += CollapseRun(run);
            }

            return removed;
        }

        private static int CollapseRun(List<XElement> run)
        {
            if (run.Count < 2)
            {
                return 0;
            }

            for (int i = 1; i < run.Count; i++)
            {
                run[i].Remove();
            }

            return run.Count - 1;
        }

        private static bool IsBlank(PageDocument page, XElement outlineElement)
        {
            if (!outlineElement.Elements().All(child => child.Name == page.One + "T"))
            {
                return false;
            }

            return OeStyleReader.IsEmpty(page, outlineElement);
        }
    }
}
