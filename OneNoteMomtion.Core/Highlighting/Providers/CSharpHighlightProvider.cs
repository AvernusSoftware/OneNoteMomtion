using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Highlighting.Providers
{
    public sealed class CSharpHighlightProvider : RegexTokenizerBase
    {
        private static readonly IReadOnlyList<TokenRule> RuleSet = new[]
        {
            TokenRule.Create(@"\s+", TokenKind.Plain),

            TokenRule.Create(@"/\*[\s\S]*?\*/", TokenKind.Comment),
            TokenRule.Create(@"//[^\r\n]*", TokenKind.Comment),
            TokenRule.Create(@"\#[ \t]*(?:if|elif|else|endif|define|undef|warning|error|line|region|endregion|pragma|nullable)\b[^\r\n]*", TokenKind.Preprocessor),
            TokenRule.Create(@"""""""[\s\S]*?""""""", TokenKind.String),
            TokenRule.Create(@"[$@]{1,2}""(?:[^""]|"""")*""", TokenKind.String),
            TokenRule.Create(@"\$?""(?:\\.|[^""\\\r\n])*""", TokenKind.String),
            TokenRule.Create(@"'(?:\\.|[^'\\\r\n])*'", TokenKind.String),
            TokenRule.Create(@"\b(?:0[xX][0-9a-fA-F_]+|0[bB][01_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?)(?:[uUlLfFdDmM]{1,2})?\b", TokenKind.Number),

            TokenRule.Words(
                TokenKind.ControlKeyword,
                RegexOptions.None,
                "if", "else", "switch", "case", "default", "for", "foreach", "in", "while", "do",
                "break", "continue", "return", "goto", "throw", "try", "catch", "finally", "yield", "await", "when"),

            TokenRule.Words(
                TokenKind.Keyword,
                RegexOptions.None,
                "abstract", "add", "as", "async", "base", "checked", "class", "const", "delegate", "enum",
                "event", "explicit", "extern", "false", "file", "fixed", "get", "global", "implicit", "init",
                "interface", "internal", "is", "lock", "nameof", "namespace", "new", "null", "operator", "out",
                "params", "partial", "private", "protected", "public", "readonly", "record", "ref", "remove",
                "required", "scoped", "sealed", "set", "sizeof", "stackalloc", "static", "struct", "this",
                "true", "typeof", "unchecked", "unsafe", "using", "value", "var", "virtual", "volatile",
                "where", "with"),

            TokenRule.Words(
                TokenKind.Type,
                RegexOptions.None,
                "bool", "byte", "char", "decimal", "double", "dynamic", "float", "int", "long", "nint",
                "nuint", "object", "sbyte", "short", "string", "uint", "ulong", "ushort", "void"),

            TokenRule.Create(@"[A-Za-z_][A-Za-z0-9_]*", TokenKind.Identifier),
            TokenRule.Create(@"=>|\?\?=|\?\?|\+\+|--|<<=|>>=|[-+*/%&|^!=<>]=|&&|\|\||[-+*/%&|^!~=<>?:]", TokenKind.Operator),
            TokenRule.Create(@"[{}()\[\];,.]", TokenKind.Punctuation),
        };

        public override string LanguageId => "csharp";

        public override string DisplayName => "C#";

        protected override IReadOnlyList<TokenRule> Rules => RuleSet;
    }
}
