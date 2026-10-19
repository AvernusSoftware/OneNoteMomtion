using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Highlighting.Detection;
using OneNoteMomtion.Core.Highlighting.Providers;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public sealed class CodeBlockReformatter
    {
        private const char FieldSeparator = (char)0x1F;
        private const char RecordSeparator = (char)0x1E;

        private readonly StyleCatalog _catalog;
        private readonly SyntaxHighlighterRegistry _registry;
        private readonly LanguageDetector _detector;
        private readonly Localizer _texts;

        public CodeBlockReformatter(StyleCatalog catalog, SyntaxHighlighterRegistry registry, LanguageDetector? detector = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _detector = detector ?? new LanguageDetector();
            _texts = Localizer.For(catalog.Language);
        }

        public int Apply(PageDocument page, IReadOnlyList<CodeBlockCandidate> candidates, CodeDetectionOptions options)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(options);

            int changed = 0;

            foreach (CodeBlockCandidate candidate in candidates)
            {
                if (Rebuild(page, candidate, options))
                {
                    changed++;
                }
            }

            return changed;
        }

        private bool Rebuild(PageDocument page, CodeBlockCandidate candidate, CodeDetectionOptions options)
        {
            if (candidate.Anchor.Parent == null)
            {
                return false;
            }

            string sourceCode = CodeTextExtractor.Extract(page, candidate.SourceLines);
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return false;
            }

            ISyntaxHighlightProvider provider = ChooseProvider(sourceCode, candidate, options);

            string blockId = candidate.ExistingBlockId ?? Guid.NewGuid().ToString("N");
            CodeBlockRenderer renderer = new(HighlightTheme.FromCatalog(_catalog), _catalog.CodeBlock, () => blockId, _texts);
            CodeBlockRenderResult replacement = renderer.CreateOutlineElements(page.One, sourceCode, provider);

            if (candidate.IsOwn && SameRendering(page, candidate.Remove, replacement.RootElements))
            {
                return false;
            }

            candidate.Anchor.AddBeforeSelf(replacement.RootElements);

            foreach (XElement outlineElement in candidate.Remove)
            {
                outlineElement.Remove();
            }

            return true;
        }

        private ISyntaxHighlightProvider ChooseProvider(string sourceCode, CodeBlockCandidate candidate, CodeDetectionOptions options)
        {
            string? languageId = CodeBlockFinder.ResolveLanguageId(_registry, candidate.KnownLanguageId);

            if (languageId == null && options.Enabled)
            {
                LanguageDetectionResult detected = _detector.Detect(sourceCode, options);
                languageId = detected.LanguageId;
            }

            if (languageId != null && _registry.TryGet(languageId, out ISyntaxHighlightProvider? provider))
            {
                return provider;
            }

            return new PlainTextHighlightProvider();
        }

        private static bool SameRendering(PageDocument page, IReadOnlyList<XElement> existing, IReadOnlyList<XElement> replacement)
        {
            return string.Equals(Signature(page, existing), Signature(page, replacement), StringComparison.Ordinal);
        }

        private static string Signature(PageDocument page, IReadOnlyList<XElement> roots)
        {
            StringBuilder builder = new();

            foreach (XElement root in roots)
            {
                foreach (XElement outlineElement in Descendants(page, root))
                {
                    builder.Append((string?)outlineElement.Attribute("style")).Append(FieldSeparator);

                    foreach (XElement run in outlineElement.Elements(page.One + "T"))
                    {
                        builder.Append(PageDocument.GetRunHtml(run)).Append(FieldSeparator);
                    }

                    builder.Append(RecordSeparator);
                }
            }

            return builder.ToString();
        }

        private static IEnumerable<XElement> Descendants(PageDocument page, XElement root)
        {
            if (root.Name == page.One + "OE")
            {
                yield return root;
            }

            foreach (XElement outlineElement in root.Descendants(page.One + "OE"))
            {
                yield return outlineElement;
            }
        }
    }
}
