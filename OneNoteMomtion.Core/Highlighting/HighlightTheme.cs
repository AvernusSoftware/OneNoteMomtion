using System;
using System.Collections.Generic;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Highlighting
{
    public sealed class HighlightTheme
    {
        private readonly Dictionary<string, string> _colors;

        public HighlightTheme(IDictionary<string, string>? colors, string? defaultColor)
        {
            _colors = colors != null ? new Dictionary<string, string>(colors, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            DefaultColor = string.IsNullOrWhiteSpace(defaultColor) ? "#1F2328" : defaultColor!;
        }

        public string DefaultColor { get; }

        public static HighlightTheme FromCatalog(StyleCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            return new HighlightTheme(catalog.HighlightTheme, catalog.CodeBlock?.ForegroundColor);
        }

        public string ColorFor(TokenKind kind)
        {
            if (_colors.TryGetValue(kind.ToString(), out string? color) && !string.IsNullOrWhiteSpace(color))
            {
                return color;
            }

            switch (kind)
            {
                case TokenKind.ControlKeyword:
                case TokenKind.Preprocessor:
                case TokenKind.Annotation:
                case TokenKind.TagName:
                    return ColorForOrDefault(TokenKind.Keyword);
                case TokenKind.PropertyName:
                case TokenKind.AttributeName:
                    return ColorForOrDefault(TokenKind.Identifier);
                default:
                    return DefaultColor;
            }
        }

        public bool NeedsExplicitColor(TokenKind kind)
        {
            return !string.Equals(ColorFor(kind), DefaultColor, StringComparison.OrdinalIgnoreCase);
        }

        private string ColorForOrDefault(TokenKind kind)
        {
            return _colors.TryGetValue(kind.ToString(), out string? color) && !string.IsNullOrWhiteSpace(color) ? color : DefaultColor;
        }
    }
}
