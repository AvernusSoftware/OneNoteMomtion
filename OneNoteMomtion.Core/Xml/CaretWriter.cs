using System;
using System.Linq;
using System.Xml.Linq;

namespace OneNoteMomtion.Core.Xml
{
    public static class CaretWriter
    {
        public static void PlaceAtStartOf(PageDocument page, XElement outlineElement)
        {
            ArgumentNullException.ThrowIfNull(page);

            ArgumentNullException.ThrowIfNull(outlineElement);

            ClearSelection(page);

            XElement? firstRun = outlineElement.Elements(page.One + "T").FirstOrDefault();
            if (firstRun != null && HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(firstRun)).Length == 0)
            {
                firstRun.SetAttributeValue("selected", "all");
                return;
            }

            XElement caret = page.TextRun(string.Empty);
            caret.SetAttributeValue("selected", "all");
            outlineElement.AddFirst(caret);
        }

        public static void ClearSelection(PageDocument page)
        {
            ArgumentNullException.ThrowIfNull(page);

            foreach (XAttribute attribute in page.Document.Descendants().Attributes("selected").ToList())
            {
                attribute.Remove();
            }
        }
    }
}
