using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Highlighting
{
    public abstract class RegexTokenizerBase : ISyntaxHighlightProvider
    {
        public abstract string LanguageId { get; }

        public abstract string DisplayName { get; }

        protected abstract IReadOnlyList<TokenRule> Rules { get; }

        public IEnumerable<Token> Tokenize(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                yield break;
            }

            IReadOnlyList<TokenRule> rules = Rules;
            int position = 0;
            int plainStart = -1;

            while (position < source.Length)
            {
                Match? match = null;
                TokenRule? matchedRule = null;

                for (int i = 0; i < rules.Count; i++)
                {
                    Match candidate = rules[i].Pattern.Match(source, position);
                    if (candidate.Success && candidate.Length > 0)
                    {
                        match = candidate;
                        matchedRule = rules[i];
                        break;
                    }
                }

                if (matchedRule == null || match == null)
                {
                    if (plainStart < 0)
                    {
                        plainStart = position;
                    }

                    position++;
                    continue;
                }

                if (plainStart >= 0)
                {
                    yield return new Token(TokenKind.Plain, source[plainStart..position], plainStart);
                    plainStart = -1;
                }

                yield return new Token(matchedRule.ResolveKind(match), match.Value, position);
                position += match.Length;
            }

            if (plainStart >= 0)
            {
                yield return new Token(TokenKind.Plain, source[plainStart..], plainStart);
            }
        }
    }
}
