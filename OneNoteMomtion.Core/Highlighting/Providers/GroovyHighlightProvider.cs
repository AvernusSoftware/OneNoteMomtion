using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Highlighting.Providers
{
    public sealed class GroovyHighlightProvider : RegexTokenizerBase
    {
        private static readonly IReadOnlyList<TokenRule> RuleSet = new[]
        {
            TokenRule.Create(@"\s+", TokenKind.Plain),

            TokenRule.Create(@"/\*[\s\S]*?\*/", TokenKind.Comment),
            TokenRule.Create(@"//[^\r\n]*", TokenKind.Comment),
            TokenRule.Create(@"^#![^\r\n]*", TokenKind.Comment, RegexOptions.Multiline),
            TokenRule.Create(@"""""""[\s\S]*?""""""", TokenKind.String),
            TokenRule.Create(@"'''[\s\S]*?'''", TokenKind.String),
            TokenRule.Create(@"""(?:\\.|[^""\\\r\n])*""", TokenKind.String),
            TokenRule.Create(@"'(?:\\.|[^'\\\r\n])*'", TokenKind.String),
            TokenRule.Create(@"@[A-Za-z_][A-Za-z0-9_.]*", TokenKind.Annotation),
            TokenRule.Create(@"\b(?:0[xX][0-9a-fA-F_]+|0[bB][01_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?)[lLfFdDgGiI]?\b", TokenKind.Number),

            TokenRule.Words(
                TokenKind.ControlKeyword,
                RegexOptions.None,
                "if", "else", "switch", "case", "default", "for", "in", "while", "do", "break", "continue",
                "return", "throw", "throws", "try", "catch", "finally"),

            TokenRule.Words(
                TokenKind.Keyword,
                RegexOptions.None,
                "abstract", "as", "assert", "class", "def", "enum", "extends", "final", "false", "implements",
                "import", "instanceof", "interface", "it", "new", "null", "package", "private", "protected",
                "public", "static", "super", "synchronized", "this", "trait", "transient", "true", "var", "volatile"),

            TokenRule.Words(
                TokenKind.Type,
                RegexOptions.None,
                "boolean", "byte", "char", "double", "float", "int", "long", "short", "void",
                "BigDecimal", "BigInteger", "Boolean", "Closure", "Date", "Double", "GString", "Integer",
                "List", "Map", "Number", "Object", "Range", "Set", "String"),

            TokenRule.Create(@"[A-Za-z_$][A-Za-z0-9_$]*", TokenKind.Identifier),
            TokenRule.Create(@"<=>|\*\.|\?\.|\?:|->|\.\.<|\.\.|=~|==~|\+\+|--|[-+*/%&|^!=<>]=|&&|\|\||[-+*/%&|^!~=<>?:]", TokenKind.Operator),
            TokenRule.Create(@"[{}()\[\];,.]", TokenKind.Punctuation),
        };

        public override string LanguageId => "groovy";

        public override string DisplayName => "Groovy";

        protected override IReadOnlyList<TokenRule> Rules => RuleSet;
    }
}
