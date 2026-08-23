using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Hierarchy
{
    public sealed class HierarchyDocument
    {
        private HierarchyDocument(XDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            XElement root = document.Root ?? throw new ArgumentException("The hierarchy document has no root element.", nameof(document));

            One = root.Name.Namespace;
            if (!OneNoteNs.IsOneNoteNamespace(One))
            {
                throw new ArgumentException("Unrecognised OneNote hierarchy namespace: " + One.NamespaceName, nameof(document));
            }

            HierarchyNode? single = ReadNode(root, One);
            Notebooks = single is null ? ReadChildren(root, One) : [single];
        }

        public XNamespace One { get; }

        public IReadOnlyList<HierarchyNode> Notebooks { get; }

        public static HierarchyDocument Parse(string hierarchyXml)
        {
            if (string.IsNullOrEmpty(hierarchyXml))
            {
                throw new ArgumentException("The hierarchy XML is empty.", nameof(hierarchyXml));
            }

            return new HierarchyDocument(XDocument.Parse(hierarchyXml));
        }

        public static bool TryParse(string hierarchyXml, [NotNullWhen(true)] out HierarchyDocument? hierarchy)
        {
            try
            {
                hierarchy = Parse(hierarchyXml);
                return true;
            }
            catch (Exception)
            {
                hierarchy = null;
                return false;
            }
        }

        public bool TryFindPagePath(string pageId, [NotNullWhen(true)] out HierarchyPath? path)
        {
            path = null;

            if (string.IsNullOrEmpty(pageId))
            {
                return false;
            }

            List<HierarchyNode> trail = [];
            if (!TryDescend(Notebooks, pageId, trail))
            {
                return false;
            }

            path = new HierarchyPath(trail);
            return true;
        }

        private static bool TryDescend(IReadOnlyList<HierarchyNode> nodes, string pageId, List<HierarchyNode> trail)
        {
            foreach (HierarchyNode node in nodes)
            {
                trail.Add(node);

                if (node.Kind == HierarchyNodeKind.Page && node.Id == pageId)
                {
                    return true;
                }

                if (TryDescend(node.Children, pageId, trail))
                {
                    return true;
                }

                trail.RemoveAt(trail.Count - 1);
            }

            return false;
        }

        private static IReadOnlyList<HierarchyNode> ReadChildren(XElement parent, XNamespace one)
        {
            List<HierarchyNode> nodes = [];

            foreach (XElement child in parent.Elements())
            {
                HierarchyNode? node = ReadNode(child, one);
                if (node is not null)
                {
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        private static HierarchyNode? ReadNode(XElement element, XNamespace one)
        {
            if (element.Name.Namespace != one)
            {
                return null;
            }

            HierarchyNodeKind? kind = KindOf(element.Name.LocalName);
            if (kind is null)
            {
                return null;
            }

            if ((bool?)element.Attribute("isRecycleBin") == true || (bool?)element.Attribute("isInRecycleBin") == true)
            {
                return null;
            }

            string id = (string?)element.Attribute("ID") ?? string.Empty;
            IReadOnlyList<HierarchyNode> children = kind == HierarchyNodeKind.Page ? [] : ReadChildren(element, one);

            return new HierarchyNode(id, DisplayName(element, kind.Value), kind.Value, children);
        }

        private static HierarchyNodeKind? KindOf(string localName)
        {
            switch (localName)
            {
                case "Notebook":
                    return HierarchyNodeKind.Notebook;
                case "SectionGroup":
                    return HierarchyNodeKind.SectionGroup;
                case "Section":
                case "UnfiledNotes":
                    return HierarchyNodeKind.Section;
                case "Page":
                    return HierarchyNodeKind.Page;
                default:
                    return null;
            }
        }

        private static string DisplayName(XElement element, HierarchyNodeKind kind)
        {
            if (kind == HierarchyNodeKind.Notebook)
            {
                string? nickname = (string?)element.Attribute("nickname");
                if (!string.IsNullOrEmpty(nickname))
                {
                    return nickname!;
                }
            }

            return (string?)element.Attribute("name") ?? string.Empty;
        }
    }
}
