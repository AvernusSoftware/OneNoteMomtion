using System;
using System.Globalization;
using System.Text;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Tests
{
    internal static class PageXml
    {
        public const string Namespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        public static string Page(params string[] outlineElements)
        {
            StringBuilder builder = new();
            builder.Append("<?xml version=\"1.0\"?>");
            builder.Append("<one:Page xmlns:one=\"").Append(Namespace).Append("\" ");
            builder.Append("ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" ");
            builder.Append("lastModifiedTime=\"2026-08-03T10:00:00.000Z\">");
            builder.Append("<one:Outline><one:OEChildren>");

            foreach (string outlineElement in outlineElements)
            {
                builder.Append(outlineElement);
            }

            builder.Append("</one:OEChildren></one:Outline></one:Page>");
            return builder.ToString();
        }

        public static string PageWithQuickStyles(string quickStyleDefs, params string[] outlineElements)
        {
            StringBuilder builder = new();
            builder.Append("<?xml version=\"1.0\"?>");
            builder.Append("<one:Page xmlns:one=\"").Append(Namespace).Append("\" ");
            builder.Append("ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" ");
            builder.Append("lastModifiedTime=\"2026-08-03T10:00:00.000Z\">");
            builder.Append(quickStyleDefs);
            builder.Append("<one:Outline><one:OEChildren>");

            foreach (string outlineElement in outlineElements)
            {
                builder.Append(outlineElement);
            }

            builder.Append("</one:OEChildren></one:Outline></one:Page>");
            return builder.ToString();
        }

        public static string PageWithTitle(string titleHtml, params string[] outlineElements)
        {
            StringBuilder builder = new();
            builder.Append("<?xml version=\"1.0\"?>");
            builder.Append("<one:Page xmlns:one=\"").Append(Namespace).Append("\" ");
            builder.Append("ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" ");
            builder.Append("lastModifiedTime=\"2026-08-03T10:00:00.000Z\">");
            builder.Append("<one:Title><one:OE><one:T><![CDATA[").Append(titleHtml).Append("]]></one:T></one:OE></one:Title>");
            builder.Append("<one:Outline><one:OEChildren>");

            foreach (string outlineElement in outlineElements)
            {
                builder.Append(outlineElement);
            }

            builder.Append("</one:OEChildren></one:Outline></one:Page>");
            return builder.ToString();
        }

        public static string QuickStyle(int index, string name, string font, double fontSize)
        {
            StringBuilder builder = new();
            builder.Append("<one:QuickStyleDef index=\"").Append(index.ToString(CultureInfo.InvariantCulture)).Append('"');
            builder.Append(" name=\"").Append(name).Append('"');
            builder.Append(" fontColor=\"automatic\" highlightColor=\"automatic\"");
            builder.Append(" font=\"").Append(font).Append('"');
            builder.Append(" fontSize=\"").Append(StyleDefinition.FormatPoints(fontSize)).Append('"');
            builder.Append(" spaceBefore=\"0.0\" spaceAfter=\"0.0\" />");
            return builder.ToString();
        }

        public static string StyledOe(string[] runs, string? style = null, int? quickStyleIndex = null)
        {
            StringBuilder builder = new();
            builder.Append("<one:OE");

            if (style != null)
            {
                builder.Append(" style=\"").Append(style).Append('"');
            }

            if (quickStyleIndex.HasValue)
            {
                builder.Append(" quickStyleIndex=\"").Append(quickStyleIndex.Value.ToString(CultureInfo.InvariantCulture)).Append('"');
            }

            builder.Append('>');

            foreach (string run in runs)
            {
                builder.Append("<one:T><![CDATA[").Append(run).Append("]]></one:T>");
            }

            builder.Append("</one:OE>");
            return builder.ToString();
        }

        public static string StyledOe(string html, string? style = null, int? quickStyleIndex = null)
        {
            return StyledOe([html], style, quickStyleIndex);
        }

        public static string Oe(string html, bool selected = true, string? style = null, string? objectId = null)
        {
            return Oe([html], selected, style, objectId);
        }

        public static string Oe(string[] runs, bool selected = true, string? style = null, string? objectId = null)
        {
            StringBuilder builder = new();
            builder.Append("<one:OE");

            if (style != null)
            {
                builder.Append(" style=\"").Append(style).Append('"');
            }

            if (objectId != null)
            {
                builder.Append(" objectID=\"").Append(objectId).Append('"');
            }

            if (selected)
            {
                builder.Append(" selected=\"all\"");
            }

            builder.Append('>');

            foreach (string run in runs)
            {
                builder.Append("<one:T");
                if (selected)
                {
                    builder.Append(" selected=\"all\"");
                }

                builder.Append("><![CDATA[").Append(run).Append("]]></one:T>");
            }

            builder.Append("</one:OE>");
            return builder.ToString();
        }

        public static string FontSizeOf(string styleName)
        {
            StyleDefinition style = StyleCatalog.CreateDefault().Get(styleName)
                ?? throw new InvalidOperationException("Style " + styleName + " is missing from the default configuration.");

            if (!style.FontSize.HasValue)
            {
                throw new InvalidOperationException("Style " + styleName + " does not set a font size.");
            }

            return "font-size:" + StyleDefinition.FormatPoints(style.FontSize.Value) + "pt";
        }

        public static string? AttributeOfFirst(string xml, string localName, string attributeName)
        {
            System.Xml.Linq.XDocument document = System.Xml.Linq.XDocument.Parse(xml);
            System.Xml.Linq.XElement? element = null;

            foreach (System.Xml.Linq.XElement candidate in document.Descendants())
            {
                if (candidate.Name.LocalName == localName)
                {
                    element = candidate;
                    break;
                }
            }

            if (element == null)
            {
                throw new InvalidOperationException("Element " + localName + " was not found in the resulting XML.");
            }

            return (string?)element.Attribute(attributeName);
        }
    }
}
