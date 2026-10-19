using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Core.Markdown;
using OneNoteMomtion.Core.Reformat;
using OneNoteMomtion.Core.Spelling;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Text;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core
{
    public sealed class PageEditor
    {
        private readonly StyleCatalog _catalog;
        private readonly SyntaxHighlighterRegistry _registry;
        private readonly Localizer _texts;

        public PageEditor(StyleCatalog catalog, SyntaxHighlighterRegistry registry)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _texts = Localizer.For(catalog.Language);
        }

        public StyleCatalog Catalog => _catalog;

        public Localizer Texts => _texts;

        public SyntaxHighlighterRegistry Registry => _registry;

        public PageEditResult ApplyStyle(string pageXml, string styleName)
        {
            bool restoreBuiltInNormal = string.Equals(styleName, StyleNames.Normal, StringComparison.OrdinalIgnoreCase);
            StyleDefinition? style = restoreBuiltInNormal ? null : _catalog.Get(styleName);

            if (!restoreBuiltInNormal && style == null)
            {
                return PageEditResult.NoChange(_texts.Format(TextKeys.EngineStyleNotFound, ("style", styleName)));
            }

            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return UnreadablePageResult();
            }

            UnwrapSelectedCodeBlocks(page);

            int changed = style == null ? OeStyleWriter.ApplyBuiltInNormalToSelection(page, _catalog) : OeStyleWriter.ApplyToSelection(page, style);

            return ResultOfChange(page, changed, _texts.Get(TextKeys.EngineNothingToFormat));
        }

        public PageEditResult ApplyTextCase(string pageXml, TextCaseMode mode)
        {
            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return UnreadablePageResult();
            }

            int changed = 0;
            foreach (XElement run in page.SelectedTextRuns())
            {
                string html = PageDocument.GetRunHtml(run);

                if (CodeBlockLink.RunContainsCopyLink(html))
                {
                    continue;
                }

                string rewritten = HtmlRunRewriter.RewriteText(html, text => TextCaseTransformer.Apply(text, mode));

                if (!string.Equals(html, rewritten, StringComparison.Ordinal))
                {
                    PageDocument.SetRunHtml(run, rewritten);
                    changed++;
                }
            }

            return ResultOfChange(page, changed, _texts.Get(TextKeys.EngineSelectTextForCase));
        }

        public PageEditResult ApplyMarkdownShortcuts(string pageXml, bool selectionOnly = true)
        {
            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return UnreadablePageResult();
            }

            MarkdownShortcutApplier applier = new(_catalog);
            int changed = applier.Apply(page, selectionOnly);

            return ResultOfChange(page, changed, _texts.Get(TextKeys.EngineNoMarkdownParagraph));
        }

        public PageEditResult ReformatPage(string pageXml, ReformatOptions? options = null)
        {
            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return UnreadablePageResult();
            }

            PageReformatter reformatter = new(_catalog, _registry);
            ReformatCounters counters = reformatter.Apply(page, options ?? ReformatOptions.FromCatalog(_catalog));

            if (counters.Total == 0)
            {
                return PageEditResult.NoChange(counters.Describe(_texts));
            }

            return PageEditResult.Updated(SaveXml(page), counters.Total, counters.Describe(_texts));
        }

        public PageEditResult ApplyMarkdownAtCaret(string pageXml)
        {
            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return UnreadablePageResult();
            }

            XElement? outlineElement = page.ActiveOutlineElement();
            if (outlineElement == null)
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineCaretNotInParagraph));
            }

            MarkdownShortcutApplier applier = new(_catalog);
            if (!applier.ApplyToOutlineElement(page, outlineElement))
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineParagraphNotMarkdown));
            }

            CaretWriter.PlaceAtStartOf(page, outlineElement);

            return PageEditResult.Updated(SaveXml(page), 1, focusObjectId: (string?)outlineElement.Attribute("objectID"));
        }

        public PageEditResult ApplyNormalAfterHeading(string pageXml, string? previousCaretObjectId = null)
        {
            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return UnreadablePageResult();
            }

            XElement? outlineElement = page.ActiveOutlineElement();
            if (outlineElement == null)
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineCaretNotInParagraph));
            }

            if (!OeStyleReader.IsHeading(page, outlineElement, _catalog))
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineParagraphNotHeading));
            }

            string? caretObjectId = (string?)outlineElement.Attribute("objectID");

            if (!OeStyleReader.IsEmpty(page, outlineElement) && !CaretMovedBetweenLooks(previousCaretObjectId, caretObjectId))
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineParagraphNotFromEnter), caretObjectId: caretObjectId);
            }

            if (!OeStyleWriter.ApplyBuiltInNormal(page, outlineElement, _catalog))
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineParagraphAlreadyNormal));
            }

            CaretWriter.PlaceAtStartOf(page, outlineElement);

            return PageEditResult.Updated(SaveXml(page), 1, nudgeCaret: true);
        }

        private static bool CaretMovedBetweenLooks(string? previousCaretObjectId, string? caretObjectId)
        {
            return previousCaretObjectId != null && caretObjectId != null && !string.Equals(previousCaretObjectId, caretObjectId, StringComparison.Ordinal);
        }

        public PageEditResult SetSpellCheck(string pageXml, bool enabled)
        {
            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return UnreadablePageResult();
            }

            if (page.SelectedTextRuns().Count == 0)
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineSelectTextForSpelling));
            }

            int changed = enabled ? SpellCheckDisabler.Enable(page) : SpellCheckDisabler.Disable(page);
            return changed > 0 ? PageEditResult.Updated(SaveXml(page), changed, nudgeCaret: true) : PageEditResult.NoChange(_texts.Get(TextKeys.EngineSpellingUnchanged));
        }

        public PageEditResult FormatCode(string pageXml, string languageId)
        {
            if (!_registry.TryGet(languageId, out ISyntaxHighlightProvider? provider))
            {
                return PageEditResult.NoChange(_texts.Format(TextKeys.EngineUnknownLanguage, ("language", languageId)));
            }

            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return UnreadablePageResult();
            }

            List<XElement> selected = [.. page.SelectedOutlineElements()];
            if (selected.Count == 0)
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineSelectCode));
            }

            XElement? codeBlock = FindEnclosingCodeBlock(page, selected[0]);

            XElement anchor;
            List<XElement> toRemove;
            IEnumerable<XElement> sourceElements;

            if (codeBlock != null)
            {
                anchor = codeBlock;
                toRemove = [codeBlock];
                sourceElements = GetCodeLineOutlineElements(page, codeBlock);
            }
            else
            {
                anchor = selected[0];
                toRemove = selected;
                sourceElements = selected;
            }

            if (anchor.Parent == null)
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineCannotFormatHere));
            }

            string sourceCode = ExtractSourceCode(page, sourceElements);
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return PageEditResult.NoChange(_texts.Get(TextKeys.EngineSelectionEmpty));
            }

            CodeBlockRenderer renderer = new(HighlightTheme.FromCatalog(_catalog), _catalog.CodeBlock, texts: _texts);
            CodeBlockRenderResult replacement = renderer.CreateOutlineElements(page.One, sourceCode, provider);

            foreach (XElement line in replacement.CodeLineElements)
            {
                line.SetAttributeValue("selected", "all");
                line.Element(page.One + "T")!.SetAttributeValue("selected", "all");
            }

            anchor.AddBeforeSelf(replacement.RootElements);
            foreach (XElement outlineElement in toRemove)
            {
                outlineElement.Remove();
            }

            return PageEditResult.Updated(SaveXml(page), selected.Count);
        }

        public CodeExtractResult ExtractCodeByBlockId(string pageXml, string blockId)
        {
            if (string.IsNullOrWhiteSpace(blockId))
            {
                return CodeExtractResult.Failed(_texts.Get(TextKeys.EngineNoCodeBlockId));
            }

            PageDocument? page = ParsePage(pageXml);
            if (page == null)
            {
                return CodeExtractResult.Failed(_texts.Get(TextKeys.EnginePageContentUnreadable));
            }

            XElement? linkRun = page.AllTextRuns().FirstOrDefault(run => CodeBlockLink.RunLinksToBlock(PageDocument.GetRunHtml(run), blockId));

            if (linkRun == null)
            {
                return CodeExtractResult.Failed(_texts.Get(TextKeys.EngineCodeBlockNotFound));
            }

            XElement? table = linkRun.Ancestors(page.One + "Table").FirstOrDefault();
            if (table == null)
            {
                return CodeExtractResult.Failed(_texts.Get(TextKeys.EngineCodeBlockUnexpectedShape));
            }

            List<XElement> lines = [.. GetCodeLinesOfTable(page, table)];
            string code = ExtractSourceCode(page, lines);

            if (string.IsNullOrEmpty(code))
            {
                return CodeExtractResult.Failed(_texts.Get(TextKeys.EngineCodeBlockEmpty));
            }

            return CodeExtractResult.Extracted(code, lines.Count);
        }

        private static void UnwrapSelectedCodeBlocks(PageDocument page)
        {
            List<XElement> codeBlocks = [];
            foreach (XElement selected in page.SelectedOutlineElements())
            {
                XElement? codeBlock = FindEnclosingCodeBlock(page, selected);
                if (codeBlock != null && !codeBlocks.Contains(codeBlock))
                {
                    codeBlocks.Add(codeBlock);
                }
            }

            foreach (XElement codeBlock in codeBlocks)
            {
                if (codeBlock.Parent == null)
                {
                    continue;
                }

                List<XElement> lines = [.. codeBlock.Descendants(page.One + "OE").Where(line => !IsCodeBlockHeaderLine(page, line))];

                foreach (XElement line in lines)
                {
                    RestoreSpellCheck(page, line);
                    line.Remove();
                }

                codeBlock.AddBeforeSelf(lines);
                codeBlock.Remove();
            }
        }

        private static void RestoreSpellCheck(PageDocument page, XElement outlineElement)
        {
            if ((string?)outlineElement.Attribute("lang") != SpellCheckDisabler.NoSpellCheckLanguage)
            {
                return;
            }

            outlineElement.Attributes("lang").Remove();
            foreach (XElement run in outlineElement.Elements(page.One + "T"))
            {
                if ((string?)run.Attribute("lang") == SpellCheckDisabler.NoSpellCheckLanguage)
                {
                    run.Attributes("lang").Remove();
                }
            }
        }

        private static bool IsCodeBlockHeaderLine(PageDocument page, XElement outlineElement)
        {
            foreach (XElement run in outlineElement.Elements(page.One + "T"))
            {
                if (CodeBlockLink.RunContainsCopyLink(PageDocument.GetRunHtml(run)))
                {
                    return true;
                }
            }

            return false;
        }

        private static XElement? FindEnclosingCodeBlock(PageDocument page, XElement element)
        {
            XElement? cell = element.Ancestors(page.One + "Cell").FirstOrDefault();
            if (cell == null)
            {
                return null;
            }

            XElement current = element;
            while (cell != null)
            {
                XElement table = cell.Ancestors(page.One + "Table").First();
                current = table.Parent ?? current;
                cell = current.Ancestors(page.One + "Cell").FirstOrDefault();
            }

            return current;
        }

        private static IEnumerable<XElement> GetCodeLineOutlineElements(PageDocument page, XElement codeBlock)
        {
            XElement? table = codeBlock.Descendants(page.One + "Table").FirstOrDefault();
            if (table == null)
            {
                return codeBlock.Descendants(page.One + "OE");
            }

            return GetCodeLinesOfTable(page, table);
        }

        private static IEnumerable<XElement> GetCodeLinesOfTable(PageDocument page, XElement table)
        {
            XElement codeRow = table.Elements(page.One + "Row").Last();
            return codeRow.Descendants(page.One + "OE");
        }

        public IReadOnlyList<KeyValuePair<string, string>> AvailableLanguages()
        {
            return [.. _registry.Selectable().Select(provider => new KeyValuePair<string, string>(provider.LanguageId, _texts.LanguageName(provider)))];
        }

        private static string ExtractSourceCode(PageDocument page, IEnumerable<XElement> outlineElements)
        {
            return CodeTextExtractor.Extract(page, outlineElements);
        }

        private static PageDocument? ParsePage(string pageXml)
        {
            return PageDocument.TryParse(pageXml, out PageDocument? page) ? page : null;
        }

        private PageEditResult UnreadablePageResult()
        {
            return PageEditResult.NoChange(_texts.Get(TextKeys.EngineOneNotePageUnreadable));
        }

        private PageEditResult ResultOfChange(PageDocument page, int changed, string nothingChangedMessage)
        {
            return changed > 0 ? PageEditResult.Updated(SaveXml(page), changed) : PageEditResult.NoChange(nothingChangedMessage);
        }

        private string SaveXml(PageDocument page)
        {
            QuickStyleReconciler.Reconcile(page, _catalog);

            return page.ToXmlString();
        }
    }
}