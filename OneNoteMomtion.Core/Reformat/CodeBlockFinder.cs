using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Highlighting.Providers;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public static class CodeBlockFinder
    {
        private const int MinimumForeignLines = 2;

        public static IReadOnlyList<CodeBlockCandidate> Find(PageDocument page, StyleCatalog catalog, CodeBlockOptions codeBlock, bool includeForeign)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentNullException.ThrowIfNull(codeBlock);

            List<CodeBlockCandidate> candidates = [.. FindOwn(page, codeBlock)];

            if (includeForeign)
            {
                HashSet<XElement> covered = CoveredElements(page, candidates);
                candidates.AddRange(FindForeign(page, catalog, covered));
            }

            return candidates;
        }

        public static HashSet<XElement> CoveredElements(PageDocument page, IEnumerable<CodeBlockCandidate> candidates)
        {
            ArgumentNullException.ThrowIfNull(page);

            HashSet<XElement> covered = [];

            foreach (CodeBlockCandidate candidate in candidates)
            {
                foreach (XElement removed in candidate.Remove)
                {
                    covered.Add(removed);

                    foreach (XElement descendant in removed.Descendants(page.One + "OE"))
                    {
                        covered.Add(descendant);
                    }
                }
            }

            return covered;
        }

        private static IEnumerable<CodeBlockCandidate> FindOwn(PageDocument page, CodeBlockOptions codeBlock)
        {
            List<CodeBlockCandidate> found = [];
            HashSet<XElement> seen = [];

            foreach (XElement table in page.Document.Descendants(page.One + "Table").ToList())
            {
                if (!IsOurCodeTable(page, table, codeBlock))
                {
                    continue;
                }

                XElement root = OutermostWrapper(page, table);

                if (!seen.Add(root))
                {
                    continue;
                }

                List<XElement> codeLines = [.. CodeLinesOf(page, table)];
                if (codeLines.Count == 0)
                {
                    continue;
                }

                ReadHeader(page, table, codeBlock, out string? blockId, out string? languageId);

                found.Add(new CodeBlockCandidate(root, [root], codeLines, languageId, isOwn: true) { ExistingBlockId = blockId });
            }

            return found;
        }

        private static IEnumerable<CodeBlockCandidate> FindForeign(PageDocument page, StyleCatalog catalog, HashSet<XElement> covered)
        {
            List<CodeBlockCandidate> found = [];

            foreach (XElement children in page.Document.Descendants(page.One + "OEChildren").ToList())
            {
                List<XElement> run = [];

                foreach (XElement outlineElement in children.Elements(page.One + "OE"))
                {
                    if (!covered.Contains(outlineElement) && LooksLikeCode(page, catalog, outlineElement))
                    {
                        run.Add(outlineElement);
                        continue;
                    }

                    AddForeignRun(found, run);
                    run = [];
                }

                AddForeignRun(found, run);
            }

            return found;
        }

        private static void AddForeignRun(List<CodeBlockCandidate> found, List<XElement> run)
        {
            if (run.Count < MinimumForeignLines)
            {
                return;
            }

            found.Add(new CodeBlockCandidate(run[0], run, run, knownLanguageId: null, isOwn: false));
        }

        private static bool LooksLikeCode(PageDocument page, StyleCatalog catalog, XElement outlineElement)
        {
            if (!outlineElement.Elements().All(child => child.Name == page.One + "T"))
            {
                return false;
            }

            if (!outlineElement.Elements(page.One + "T").Any())
            {
                return false;
            }

            if (IsCodeQuickStyle(page, outlineElement))
            {
                return true;
            }

            Dictionary<string, string> effective = QuickStyleProperties.Effective(page, outlineElement);

            return effective.TryGetValue("font-family", out string? font) && MonospaceFonts.IsMonospace(font) && !OeStyleReader.IsHeading(page, outlineElement, catalog);
        }

        private static bool IsCodeQuickStyle(PageDocument page, XElement outlineElement)
        {
            XElement? definition = page.FindQuickStyleByIndex((string?)outlineElement.Attribute("quickStyleIndex"));
            string? name = (string?)definition?.Attribute("name");

            return string.Equals(name, "code", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "pre", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOurCodeTable(PageDocument page, XElement table, CodeBlockOptions codeBlock)
        {
            if (table.Descendants(page.One + "T").Any(run => CodeBlockLink.RunContainsCopyLink(PageDocument.GetRunHtml(run))))
            {
                return true;
            }

            List<XElement> rows = [.. table.Elements(page.One + "Row")];
            if (rows.Count is < 1 or > 2)
            {
                return false;
            }

            List<XElement> lines = [.. CodeLinesOf(page, table)];

            return lines.Count > 0 && lines.All(line => UsesCodeFont(line, codeBlock));
        }

        private static bool UsesCodeFont(XElement outlineElement, CodeBlockOptions codeBlock)
        {
            Dictionary<string, string> css = CssProperties.Parse((string?)outlineElement.Attribute("style"));

            return css.TryGetValue("font-family", out string? font) && string.Equals(font.Trim(), codeBlock.Font, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<XElement> CodeLinesOf(PageDocument page, XElement table)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(table);

            XElement? codeRow = table.Elements(page.One + "Row").LastOrDefault();

            return codeRow == null ? [] : codeRow.Descendants(page.One + "OE").Where(line => line.Elements(page.One + "T").Any());
        }

        private static XElement OutermostWrapper(PageDocument page, XElement table)
        {
            XElement current = table.Parent ?? table;
            XElement? cell = current.Ancestors(page.One + "Cell").FirstOrDefault();

            while (cell != null)
            {
                XElement outerTable = cell.Ancestors(page.One + "Table").First();
                current = outerTable.Parent ?? current;
                cell = current.Ancestors(page.One + "Cell").FirstOrDefault();
            }

            return current;
        }

        private static void ReadHeader(PageDocument page, XElement table, CodeBlockOptions codeBlock, out string? blockId, out string? languageId)
        {
            blockId = null;
            languageId = null;

            List<XElement> rows = [.. table.Elements(page.One + "Row")];
            if (rows.Count < 2)
            {
                return;
            }

            foreach (XElement run in rows[0].Descendants(page.One + "T"))
            {
                string html = PageDocument.GetRunHtml(run);

                if (CodeBlockLink.TryGetRunLink(html, out string id, out string language))
                {
                    blockId = id;
                    languageId = language.Length > 0 ? language : null;
                }

                languageId ??= DisplayNameOf(html, codeBlock);

                if (blockId != null || languageId != null)
                {
                    return;
                }
            }
        }

        private static string? DisplayNameOf(string headerHtml, CodeBlockOptions codeBlock)
        {
            string text = HtmlRunRewriter.ExtractPlainText(headerHtml);

            foreach (string label in CopyButtonLabels(codeBlock))
            {
                if (text.EndsWith(label, StringComparison.Ordinal))
                {
                    text = text[..^label.Length];
                    break;
                }
            }

            text = text.Trim();

            return text.Length == 0 ? null : text;
        }

        public static string? ResolveLanguageId(SyntaxHighlighterRegistry registry, string? known)
        {
            ArgumentNullException.ThrowIfNull(registry);

            if (string.IsNullOrWhiteSpace(known))
            {
                return null;
            }

            if (registry.TryGet(known, out ISyntaxHighlightProvider? byId))
            {
                return byId.LanguageId;
            }

            if (TextCatalog.EveryTranslationOf(TextKeys.PagePlainText).Contains(known.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return PlainTextHighlightProvider.Id;
            }

            return registry.TryGetByDisplayName(known, out ISyntaxHighlightProvider? byName) ? byName.LanguageId : null;
        }

        private static IEnumerable<string> CopyButtonLabels(CodeBlockOptions codeBlock)
        {
            if (!string.IsNullOrEmpty(codeBlock.CopyButtonText) && codeBlock.CopyButtonText != CodeBlockOptions.DefaultCopyButtonText)
            {
                return [codeBlock.CopyButtonText];
            }

            return TextCatalog.EveryTranslationOf(TextKeys.PageCopyButton);
        }
    }
}