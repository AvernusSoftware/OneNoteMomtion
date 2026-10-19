using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Core.Spelling;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Highlighting
{
    public sealed class CodeBlockRenderer
    {
        private const string HeaderGap = "&nbsp;&nbsp;&nbsp;";

        private readonly HighlightTheme _theme;
        private readonly CodeBlockOptions _options;
        private readonly Func<string> _blockIdFactory;
        private readonly Localizer _texts;

        public CodeBlockRenderer(HighlightTheme theme, CodeBlockOptions options, Func<string>? blockIdFactory = null, Localizer? texts = null)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _blockIdFactory = blockIdFactory ?? (() => Guid.NewGuid().ToString("N"));
            _texts = texts ?? Localizer.English;
        }

        public string LineCss
        {
            get
            {
                StringBuilder builder = new();
                builder.Append("font-family:").Append(_options.Font);
                builder.Append(";font-size:").Append(StyleDefinition.FormatPoints(_options.FontSize)).Append("pt");

                if (!string.IsNullOrWhiteSpace(_options.ForegroundColor))
                {
                    builder.Append(";color:").Append(_options.ForegroundColor);
                }

                return builder.ToString();
            }
        }

        public IReadOnlyList<string> RenderLines(IEnumerable<Token> tokens)
        {
            ArgumentNullException.ThrowIfNull(tokens);

            List<StringBuilder> lines = [];
            StringBuilder current = new();

            foreach (Token token in tokens)
            {
                string normalized = NormalizeNewLines(token.Text);
                string[] parts = normalized.Split('\n');

                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0)
                    {
                        lines.Add(current);
                        current = new StringBuilder();
                    }

                    if (parts[i].Length > 0)
                    {
                        current.Append(RenderFragment(token.Kind, parts[i]));
                    }
                }
            }

            lines.Add(current);

            List<string> result = new(lines.Count);
            foreach (StringBuilder line in lines)
            {
                result.Add(line.ToString());
            }

            return result;
        }

        public CodeBlockRenderResult CreateOutlineElements(XNamespace one, string sourceCode, ISyntaxHighlightProvider provider)
        {
            ArgumentNullException.ThrowIfNull(one);

            ArgumentNullException.ThrowIfNull(provider);

            IReadOnlyList<string> lines = RenderLines(provider.Tokenize(sourceCode ?? string.Empty));
            string css = LineCss;
            string? lang = _options.DisableSpellCheck ? SpellCheckDisabler.NoSpellCheckLanguage : null;

            List<XElement> lineElements = new(lines.Count);
            foreach (string line in lines)
            {
                lineElements.Add(OeStyleWriter.CreateOutlineElement(one, line, css, lang: lang));
            }

            if (!_options.WrapInTable)
            {
                return new CodeBlockRenderResult(lineElements, lineElements);
            }

            string? blockId = _options.ShowCopyButton ? _blockIdFactory() : null;

            XElement root = new(one + "OE", CreateTable(one, lineElements, _texts.LanguageName(provider), blockId, provider.LanguageId));
            return new CodeBlockRenderResult([root], lineElements);
        }

        public XElement CreateTable(XNamespace one, IEnumerable<XElement> lineElements, string? languageDisplayName = null, string? copyBlockId = null, string? languageId = null)
        {
            XElement codeCell = new(one + "Cell", new XElement(one + "OEChildren", lineElements));

            if (!string.IsNullOrWhiteSpace(_options.ShadingColor))
            {
                codeCell.SetAttributeValue("shadingColor", _options.ShadingColor);
            }

            bool showLanguage = _options.ShowLanguageHeader && !string.IsNullOrWhiteSpace(languageDisplayName);
            bool showCopy = !string.IsNullOrEmpty(copyBlockId);
            bool showHeader = showLanguage || showCopy;

            List<XElement> rows = [];
            if (showHeader)
            {
                rows.Add(new XElement(
                    one + "Row",
                    CreateHeaderCell(one, showLanguage ? languageDisplayName! : string.Empty, copyBlockId, languageId)));
            }

            rows.Add(new XElement(one + "Row", codeCell));

            return new XElement(
                one + "Table",
                new XAttribute("bordersVisible", _options.BordersVisible ? "true" : "false"),
                new XAttribute("hasHeaderRow", showHeader ? "true" : "false"),
                new XElement(
                    one + "Columns",
                    new XElement(
                        one + "Column",
                        new XAttribute("index", "0"),
                        new XAttribute("width", _options.TableWidth.ToString("0.0", CultureInfo.InvariantCulture)))),
                rows);
        }

        private XElement CreateHeaderCell(XNamespace one, string languageDisplayName, string? copyBlockId, string? languageId)
        {
            StringBuilder css = new();
            css.Append("font-family:").Append(_options.Font);
            css.Append(";font-size:").Append(StyleDefinition.FormatPoints(_options.HeaderFontSize)).Append("pt");

            if (_options.HeaderBold)
            {
                css.Append(";font-weight:bold");
            }

            if (!string.IsNullOrWhiteSpace(_options.HeaderForegroundColor))
            {
                css.Append(";color:").Append(_options.HeaderForegroundColor);
            }

            XElement headerLine = OeStyleWriter.CreateOutlineElement(
                one,
                BuildHeaderHtml(languageDisplayName, copyBlockId, languageId),
                css.ToString(),
                lang: _options.DisableSpellCheck ? SpellCheckDisabler.NoSpellCheckLanguage : null);

            XElement cell = new(one + "Cell", new XElement(one + "OEChildren", headerLine));

            if (!string.IsNullOrWhiteSpace(_options.HeaderShadingColor))
            {
                cell.SetAttributeValue("shadingColor", _options.HeaderShadingColor);
            }

            return cell;
        }

        private string BuildHeaderHtml(string languageDisplayName, string? copyBlockId, string? languageId)
        {
            string language = HtmlRunRewriter.Encode(languageDisplayName ?? string.Empty);

            if (string.IsNullOrEmpty(copyBlockId))
            {
                return language;
            }

            string label = _options.CopyButtonText == CodeBlockOptions.DefaultCopyButtonText ? _texts.Get(TextKeys.PageCopyButton) : _options.CopyButtonText ?? string.Empty;
            if (label.Length == 0)
            {
                return language;
            }

            StringBuilder builder = new(language);

            if (language.Length > 0)
            {
                builder.Append(HeaderGap);
            }

            builder.Append("<a href=\"").Append(HtmlRunRewriter.Encode(CodeBlockLink.CopyHref(copyBlockId!, languageId))).Append("\">");
            builder.Append(HtmlRunRewriter.Encode(label));
            builder.Append("</a>");

            return builder.ToString();
        }

        private string RenderFragment(TokenKind kind, string text)
        {
            string encoded = HtmlRunRewriter.EncodePreservingIndent(text, _options.TabWidth);

            if (!_theme.NeedsExplicitColor(kind))
            {
                return encoded;
            }

            return "<span style='color:" + _theme.ColorFor(kind) + "'>" + encoded + "</span>";
        }

        private static string NormalizeNewLines(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('\r') < 0)
            {
                return text ?? string.Empty;
            }

            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}
