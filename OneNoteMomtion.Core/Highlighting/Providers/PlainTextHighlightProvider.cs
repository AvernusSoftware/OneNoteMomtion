using System.Collections.Generic;

namespace OneNoteMomtion.Core.Highlighting.Providers
{
    public sealed class PlainTextHighlightProvider : ISyntaxHighlightProvider
    {
        public const string Id = "plain";

        public string LanguageId => Id;

        public string DisplayName => "Plain text";

        public IEnumerable<Token> Tokenize(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                yield break;
            }

            yield return new Token(TokenKind.Plain, source, 0);
        }
    }
}
