using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Highlighting
{
    public static class CodeTextExtractor
    {
        public static string Extract(PageDocument page, IEnumerable<XElement> outlineElements)
        {
            StringBuilder builder = new();
            bool first = true;

            foreach (XElement outlineElement in outlineElements)
            {
                if (!first)
                {
                    builder.Append('\n');
                }

                first = false;

                foreach (XElement run in outlineElement.Elements(page.One + "T"))
                {
                    builder.Append(HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(run)));
                }
            }

            return builder.ToString();
        }
    }
}
