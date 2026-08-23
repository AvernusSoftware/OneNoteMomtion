using System.Text;

namespace OneNoteMomtion.Core.Tests
{
    internal static class HierarchyXml
    {
        public const string Namespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        public static string Notebooks(params string[] notebooks)
        {
            StringBuilder builder = new();
            builder.Append("<?xml version=\"1.0\"?>");
            builder.Append("<one:Notebooks xmlns:one=\"").Append(Namespace).Append("\">");

            foreach (string notebook in notebooks)
            {
                builder.Append(notebook);
            }

            builder.Append("</one:Notebooks>");
            return builder.ToString();
        }

        public static string Notebook(string name, string id, string? nickname, params string[] children)
        {
            StringBuilder builder = new();
            builder.Append("<one:Notebook name=\"").Append(name).Append("\" ID=\"").Append(id).Append('"');

            if (nickname != null)
            {
                builder.Append(" nickname=\"").Append(nickname).Append('"');
            }

            builder.Append('>');
            Append(builder, children);
            builder.Append("</one:Notebook>");
            return builder.ToString();
        }

        public static string SectionGroup(string name, string id, params string[] children)
        {
            return Container("SectionGroup", name, id, null, children);
        }

        public static string RecycleBin(string name, string id, params string[] children)
        {
            return Container("SectionGroup", name, id, "isRecycleBin=\"true\"", children);
        }

        public static string Section(string name, string id, params string[] pages)
        {
            return Container("Section", name, id, null, pages);
        }

        public static string Page(string name, string id)
        {
            return "<one:Page name=\"" + name + "\" ID=\"" + id + "\" pageLevel=\"1\" />";
        }

        private static string Container(string elementName, string name, string id, string? extraAttributes, string[] children)
        {
            StringBuilder builder = new();
            builder.Append("<one:").Append(elementName).Append(" name=\"").Append(name).Append("\" ID=\"").Append(id).Append('"');

            if (extraAttributes != null)
            {
                builder.Append(' ').Append(extraAttributes);
            }

            builder.Append('>');
            Append(builder, children);
            builder.Append("</one:").Append(elementName).Append('>');
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, string[] children)
        {
            foreach (string child in children)
            {
                builder.Append(child);
            }
        }
    }
}
