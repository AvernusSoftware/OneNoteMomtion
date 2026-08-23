using System;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Highlighting
{
    public sealed class TokenRule
    {
        private readonly Func<Match, TokenKind>? _kindSelector;

        private TokenRule(Regex pattern, TokenKind kind, Func<Match, TokenKind>? kindSelector)
        {
            Pattern = pattern;
            Kind = kind;
            _kindSelector = kindSelector;
        }

        public Regex Pattern { get; }

        public TokenKind Kind { get; }

        public static TokenRule Create(string pattern, TokenKind kind, RegexOptions options = RegexOptions.None)
        {
            return new TokenRule(Compile(pattern, options), kind, null);
        }

        public static TokenRule Create(string pattern, Func<Match, TokenKind> kindSelector, RegexOptions options = RegexOptions.None)
        {
            ArgumentNullException.ThrowIfNull(kindSelector);

            return new TokenRule(Compile(pattern, options), TokenKind.Plain, kindSelector);
        }

        public static TokenRule Words(TokenKind kind, RegexOptions options, params string[] words)
        {
            if (words == null || words.Length == 0)
            {
                throw new ArgumentException("The word list must not be empty.", nameof(words));
            }

            string[] escaped = new string[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                escaped[i] = Regex.Escape(words[i]);
            }

            return Create(@"\b(?:" + string.Join("|", escaped) + @")\b", kind, options);
        }

        public TokenKind ResolveKind(Match match)
        {
            return _kindSelector != null ? _kindSelector(match) : Kind;
        }

        private static Regex Compile(string pattern, RegexOptions options)
        {
            return new Regex(@"\G(?:" + pattern + ")", options | RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }
    }
}
