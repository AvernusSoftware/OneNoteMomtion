using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Highlighting.Providers
{
    public sealed class PowerShellHighlightProvider : RegexTokenizerBase
    {
        private const RegexOptions IgnoreCase = RegexOptions.IgnoreCase;

        private static readonly IReadOnlyList<TokenRule> RuleSet = new[]
        {
            TokenRule.Create(@"\s+", TokenKind.Plain),

            TokenRule.Create(@"<#[\s\S]*?#>", TokenKind.Comment),
            TokenRule.Create(@"#[^\r\n]*", TokenKind.Comment),
            TokenRule.Create(@"@""[\s\S]*?""@", TokenKind.String),
            TokenRule.Create(@"@'[\s\S]*?'@", TokenKind.String),
            TokenRule.Create(@"""(?:`.|[^""`])*""", TokenKind.String),
            TokenRule.Create(@"'(?:''|[^'])*'", TokenKind.String),
            TokenRule.Create(@"\$(?:\{[^}]+\}|[A-Za-z_][A-Za-z0-9_:]*|\$|\?|\^|_)", TokenKind.Annotation),
            TokenRule.Create(@"@[A-Za-z_][A-Za-z0-9_]*", TokenKind.Annotation),
            TokenRule.Create(@"\b0[xX][0-9a-fA-F]+\b|\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?(?:[kKmMgGtTpP][bB])?\b", TokenKind.Number),

            TokenRule.Create(
                @"(?<![A-Za-z0-9])-(?:eq|ne|gt|lt|ge|le|like|notlike|match|notmatch|contains|notcontains|" +
                @"in|notin|is|isnot|as|and|or|not|xor|replace|split|join|f|band|bor|bxor|bnot|shl|shr)\b",
                TokenKind.Operator,
                IgnoreCase),

            TokenRule.Create(@"(?<![A-Za-z0-9])-[A-Za-z][A-Za-z0-9]*", TokenKind.AttributeName),

            TokenRule.Words(
                TokenKind.ControlKeyword,
                IgnoreCase,
                "if", "elseif", "else", "switch", "for", "foreach", "in", "while", "do", "until",
                "break", "continue", "return", "try", "catch", "finally", "throw", "trap", "exit"),

            TokenRule.Words(
                TokenKind.Keyword,
                IgnoreCase,
                "function", "filter", "class", "enum", "using", "namespace", "workflow", "configuration",
                "param", "begin", "process", "end", "dynamicparam", "data", "hidden", "static",
                "private", "public", "module"),

            TokenRule.Create(@"\[[A-Za-z_][A-Za-z0-9_.]*(?:\[\])?\]", TokenKind.Type),
            TokenRule.Create(@"[A-Za-z_][A-Za-z0-9_]*", TokenKind.Identifier),
            TokenRule.Create(@"\.\.|::|\+=|-=|\*=|/=|%=|={1,2}|!=|\+\+|--|[+\-*/%=<>!&|]", TokenKind.Operator),
            TokenRule.Create(@"[{}()\[\];,.:]", TokenKind.Punctuation),
        };

        public override string LanguageId => "powershell";

        public override string DisplayName => "PowerShell";

        protected override IReadOnlyList<TokenRule> Rules => RuleSet;
    }
}
