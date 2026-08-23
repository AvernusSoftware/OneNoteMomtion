using System.Collections.Generic;

namespace OneNoteMomtion.Core.Highlighting.Providers
{
    public sealed class XmlHighlightProvider : RegexTokenizerBase
    {
        private static readonly IReadOnlyList<TokenRule> RuleSet = new[]
        {
            TokenRule.Create(@"<!--[\s\S]*?-->", TokenKind.Comment),
            TokenRule.Create(@"<!\[CDATA\[[\s\S]*?\]\]>", TokenKind.String),
            TokenRule.Create(@"<\?[\s\S]*?\?>", TokenKind.Preprocessor),
            TokenRule.Create(@"<!DOCTYPE[^>]*>", TokenKind.Preprocessor, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            TokenRule.Create(@"</?[A-Za-z_][A-Za-z0-9_.:-]*", TokenKind.TagName),
            TokenRule.Create(@"/?>", TokenKind.TagName),
            TokenRule.Create(@"[A-Za-z_][A-Za-z0-9_.:-]*(?=\s*=)", TokenKind.AttributeName),
            TokenRule.Create(@"""[^""]*""|'[^']*'", TokenKind.String),
            TokenRule.Create(@"&(?:[A-Za-z][A-Za-z0-9]*|\#[0-9]+|\#[xX][0-9a-fA-F]+);", TokenKind.Number),
            TokenRule.Create(@"=", TokenKind.Operator),
            TokenRule.Create(@"\s+", TokenKind.Plain),
            TokenRule.Create(@"[^<&\s]+", TokenKind.Plain),
        };

        public override string LanguageId => "xml";

        public override string DisplayName => "XML";

        protected override IReadOnlyList<TokenRule> Rules => RuleSet;
    }
}