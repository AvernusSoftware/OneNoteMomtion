using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Highlighting.Providers
{
    public sealed class JavaHighlightProvider : RegexTokenizerBase
    {
        private static readonly IReadOnlyList<TokenRule> RuleSet = new[]
        {
            TokenRule.Create(@"\s+", TokenKind.Plain),

            TokenRule.Create(@"/\*[\s\S]*?\*/", TokenKind.Comment),
            TokenRule.Create(@"//[^\r\n]*", TokenKind.Comment),
            TokenRule.Create(@"""""""[\s\S]*?""""""", TokenKind.String),
            TokenRule.Create(@"""(?:\\.|[^""\\\r\n])*""", TokenKind.String),
            TokenRule.Create(@"'(?:\\.|[^'\\\r\n])*'", TokenKind.String),
            TokenRule.Create(@"@[A-Za-z_][A-Za-z0-9_.]*", TokenKind.Annotation),
            TokenRule.Create(@"\b(?:0[xX][0-9a-fA-F_]+|0[bB][01_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?)[lLfFdD]?\b", TokenKind.Number),

            TokenRule.Words(
                TokenKind.ControlKeyword,
                RegexOptions.None,
                "if", "else", "switch", "case", "default", "for", "while", "do", "break", "continue",
                "return", "throw", "throws", "try", "catch", "finally", "yield"),

            TokenRule.Words(
                TokenKind.Keyword,
                RegexOptions.None,
                "abstract", "assert", "class", "const", "enum", "extends", "final", "false", "implements",
                "import", "instanceof", "interface", "native", "new", "null", "package", "permits", "private",
                "protected", "public", "record", "sealed", "static", "strictfp", "super", "synchronized",
                "this", "transient", "true", "var", "volatile"),

            TokenRule.Words(
                TokenKind.Type,
                RegexOptions.None,
                "boolean", "byte", "char", "double", "float", "int", "long", "short", "void",
                "Boolean", "Byte", "Character", "Double", "Float", "Integer", "Long", "Object", "Short",
                "String", "StringBuilder", "List", "ArrayList", "Map", "HashMap", "Set", "HashSet", "Optional"),

            TokenRule.Create(@"[A-Za-z_$][A-Za-z0-9_$]*", TokenKind.Identifier),
            TokenRule.Create(@"->|::|\+\+|--|<<=|>>>=|>>=|>>>|[-+*/%&|^!=<>]=|&&|\|\||[-+*/%&|^!~=<>?:]", TokenKind.Operator),
            TokenRule.Create(@"[{}()\[\];,.]", TokenKind.Punctuation),
        };

        public override string LanguageId => "java";

        public override string DisplayName => "Java";

        protected override IReadOnlyList<TokenRule> Rules => RuleSet;
    }
}
