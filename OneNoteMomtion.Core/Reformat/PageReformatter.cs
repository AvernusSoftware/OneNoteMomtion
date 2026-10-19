using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Highlighting.Detection;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public sealed class PageReformatter
    {
        private readonly StyleCatalog _catalog;
        private readonly SyntaxHighlighterRegistry _registry;
        private readonly LanguageDetector _detector;
        private readonly Localizer _texts;

        public PageReformatter(StyleCatalog catalog, SyntaxHighlighterRegistry registry, Localizer texts, LanguageDetector? detector = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _detector = detector ?? new LanguageDetector();
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        }

        public ReformatCounters Apply(PageDocument page, ReformatOptions options)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(options);

            ReformatCounters counters = new();

            HashSet<XElement> title = TitleElements(page);

            IReadOnlyList<CodeBlockCandidate> codeBlocks = options.CodeDetection.Enabled
                ? [.. CodeBlockFinder.Find(page, _catalog, _catalog.CodeBlock, options.CodeDetection.ConvertForeignBlocks).Where(candidate => !title.Contains(candidate.Anchor))]
                : [];

            HashSet<XElement> untouchable = CodeBlockFinder.CoveredElements(page, codeBlocks);
            untouchable.UnionWith(title);

            counters.Headings = HeadingNormalizer.Apply(page, _catalog, untouchable);
            counters.Paragraphs = BodyTextNormalizer.Apply(page, _catalog, options.BodyText, untouchable);

            if (options.TextCleanup.CleanPastedFormatting)
            {
                counters.Paragraphs += PastedFormattingCleaner.Apply(page, untouchable);
            }

            counters.TextFixes = ParagraphTextCleaner.Apply(page, options.TextCleanup, untouchable);

            if (options.TextCleanup.CollapseEmptyParagraphs)
            {
                counters.BlankParagraphs = BlankLinesAroundHeadings.Apply(page, _catalog, untouchable);
                counters.BlankParagraphs += EmptyParagraphCollapser.Apply(page, untouchable);
            }

            if (codeBlocks.Count > 0)
            {
                CodeBlockReformatter reformatter = new(_catalog, _registry, _texts, _detector);
                counters.CodeBlocks = reformatter.Apply(page, codeBlocks, options.CodeDetection);
            }

            return counters;
        }

        private static HashSet<XElement> TitleElements(PageDocument page)
        {
            HashSet<XElement> elements = [];

            foreach (XElement title in page.Document.Descendants(page.One + "Title"))
            {
                foreach (XElement outlineElement in title.Descendants(page.One + "OE"))
                {
                    elements.Add(outlineElement);
                }
            }

            return elements;
        }
    }
}
