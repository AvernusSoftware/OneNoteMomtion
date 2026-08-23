using System.Collections.Generic;

namespace OneNoteMomtion.Core.Highlighting.Providers
{
    public sealed class JsonHighlightProvider : RegexTokenizerBase
    {
        private static readonly IReadOnlyList<TokenRule> RuleSet = new[]
        {
            TokenRule.Create(@"\s+", TokenKind.Plain),
            TokenRule.Create(@"""(?:\\.|[^""\\])*""(?=\s*:)", TokenKind.PropertyName),
            TokenRule.Create(@"""(?:\\.|[^""\\])*""", TokenKind.String),
            TokenRule.Create(@"-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b", TokenKind.Number),
            TokenRule.Words(TokenKind.Keyword, System.Text.RegularExpressions.RegexOptions.None, "true", "false", "null"),
            TokenRule.Create(@"[{}\[\],:]", TokenKind.Punctuation),
            TokenRule.Create(@"//[^\r\n]*", TokenKind.Comment),
            TokenRule.Create(@"/\*[\s\S]*?\*/", TokenKind.Comment),
        };

        public override string LanguageId => "json";

        public override string DisplayName => "JSON";

        protected override IReadOnlyList<TokenRule> Rules => RuleSet;
    }
}
