using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;

namespace OneNoteMomtion.Core.Xml
{
    public sealed class PageDocument
    {
        private readonly XDocument _document;

        private PageDocument(XDocument document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));

            XElement? root = document.Root ?? throw new ArgumentException("The page document has no root element.", nameof(document));
            One = root.Name.Namespace;
            if (!OneNoteNs.IsOneNoteNamespace(One))
            {
                throw new ArgumentException("Unrecognised OneNote page namespace: " + One.NamespaceName, nameof(document));
            }
        }

        public XNamespace One { get; }

        public XDocument Document => _document;

        public string? PageId => (string?)_document.Root?.Attribute("ID");

        public string? LastModifiedTime => (string?)_document.Root?.Attribute("lastModifiedTime");

        public static PageDocument Parse(string pageXml)
        {
            if (string.IsNullOrEmpty(pageXml))
            {
                throw new ArgumentException("The page XML is empty.", nameof(pageXml));
            }

            return new PageDocument(XDocument.Parse(pageXml, LoadOptions.PreserveWhitespace));
        }

        public static bool TryParse(string pageXml, [NotNullWhen(true)] out PageDocument? page)
        {
            try
            {
                page = Parse(pageXml);
                return true;
            }
            catch (Exception)
            {
                page = null;
                return false;
            }
        }

        public IEnumerable<XElement> AllOutlineElements()
        {
            return _document.Descendants(One + "OE");
        }

        public IEnumerable<XElement> AllTextRuns()
        {
            return _document.Descendants(One + "T");
        }

        public IReadOnlyList<XElement> SelectedTextRuns()
        {
            XName t = One + "T";

            List<XElement> exact = [.. _document.Descendants(t).Where(run => IsSelected(run, false))];

            if (exact.Count > 0)
            {
                return exact;
            }

            return [.. SelectedOutlineElements().SelectMany(oe => oe.Elements(t))];
        }

        public IReadOnlyList<XElement> SelectedOutlineElements()
        {
            XName oe = One + "OE";

            List<XElement> marked = [.. _document.Descendants(oe).Where(element => IsSelected(element, true))];
            if (marked.Count == 0)
            {
                return [];
            }

            List<XElement> chosen = [.. marked.Where(element => IsSelected(element, false) || !marked.Any(other => !ReferenceEquals(other, element) && IsAncestorOf(element, other)))];
            List<XElement> paragraphs = [.. chosen.Where(element => element.Elements(One + "T").Any())];

            if (paragraphs.Count > 0)
            {
                return paragraphs;
            }

            return chosen.Count > 0 ? chosen : marked;
        }

        public XElement? ActiveOutlineElement()
        {
            IReadOnlyList<XElement> selected = SelectedOutlineElements();
            return selected.Count == 1 ? selected[0] : null;
        }

        public const string NormalQuickStyleName = "p";

        public XElement? FindQuickStyleByName(string name, XElement? near = null)
        {
            List<XElement> candidates = [.. _document.Descendants(One + "QuickStyleDef").Where(definition => string.Equals((string?)definition.Attribute("name"), name, StringComparison.OrdinalIgnoreCase))];

            return ChooseQuickStyle(candidates, near);
        }

        public static XElement? ChooseQuickStyle(IReadOnlyList<XElement> candidates, XElement? near)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            if (candidates.Count <= 1)
            {
                return candidates.Count == 1 ? candidates[0] : null;
            }

            return (near == null ? null : FindSiblingQuickStyle(near, candidates)) ?? candidates[0];
        }

        private static XElement? FindSiblingQuickStyle(XElement outlineElement, IReadOnlyList<XElement> candidates)
        {
            XElement? parent = outlineElement.Parent;
            if (parent == null)
            {
                return null;
            }

            Dictionary<string, XElement> candidatesByIndex = candidates
                .Where(candidate => candidate.Attribute("index") != null)
                .ToDictionary(candidate => (string)candidate.Attribute("index")!, candidate => candidate);

            List<XElement> siblings = [.. parent.Elements(outlineElement.Name)];
            int position = siblings.IndexOf(outlineElement);

            for (int offset = 1; offset < siblings.Count; offset++)
            {
                int before = position - offset;
                if (before >= 0 && TryMatchQuickStyle(siblings[before], candidatesByIndex, out XElement? beforeMatch))
                {
                    return beforeMatch;
                }

                int after = position + offset;
                if (after < siblings.Count && TryMatchQuickStyle(siblings[after], candidatesByIndex, out XElement? afterMatch))
                {
                    return afterMatch;
                }
            }

            return null;
        }

        private static bool TryMatchQuickStyle(XElement sibling, Dictionary<string, XElement> candidatesByIndex, out XElement? match)
        {
            string? index = (string?)sibling.Attribute("quickStyleIndex");
            if (index != null && candidatesByIndex.TryGetValue(index, out match))
            {
                return true;
            }

            match = null;
            return false;
        }

        public XElement? FindQuickStyleByIndex(string? index)
        {
            if (string.IsNullOrEmpty(index))
            {
                return null;
            }

            return _document.Descendants(One + "QuickStyleDef").FirstOrDefault(definition => (string?)definition.Attribute("index") == index);
        }

        public IReadOnlyList<XElement> QuickStyleDefinitions()
        {
            return [.. _document.Descendants(One + "QuickStyleDef")];
        }

        public void AddQuickStyleDefinition(XElement definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            IReadOnlyList<XElement> existing = QuickStyleDefinitions();

            if (existing.Count > 0)
            {
                existing[^1].AddAfterSelf(definition);
            }
            else
            {
                _document.Root!.AddFirst(definition);
            }
        }

        public string ToXmlString()
        {
            return _document.ToString(SaveOptions.DisableFormatting);
        }

        public XElement Element(string localName, params object[] content)
        {
            return new XElement(One + localName, content);
        }

        public XElement TextRun(string html)
        {
            return new XElement(One + "T", new XCData(html ?? string.Empty));
        }

        public static string GetRunHtml(XElement? textRun)
        {
            if (textRun == null)
            {
                return string.Empty;
            }

            return textRun.FirstNode is XCData cdata ? cdata.Value : textRun.Value;
        }

        public static void SetRunHtml(XElement textRun, string? html)
        {
            ArgumentNullException.ThrowIfNull(textRun);

            textRun.ReplaceNodes(new XCData(html ?? string.Empty));
        }

        private static bool IsSelected(XElement element, bool includePartial)
        {
            string? selected = (string?)element.Attribute("selected");
            if (selected == null)
            {
                return false;
            }

            if (selected == "all")
            {
                return true;
            }

            return includePartial && selected == "partial";
        }

        private static bool IsAncestorOf(XElement candidate, XElement descendant)
        {
            for (XElement? parent = descendant.Parent; parent != null; parent = parent.Parent)
            {
                if (ReferenceEquals(parent, candidate))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
