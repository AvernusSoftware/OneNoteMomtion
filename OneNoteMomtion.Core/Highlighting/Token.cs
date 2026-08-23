using System;

namespace OneNoteMomtion.Core.Highlighting
{
    public struct Token : IEquatable<Token>
    {
        public Token(TokenKind kind, string text, int start)
        {
            Kind = kind;
            Text = text ?? string.Empty;
            Start = start;
        }

        public TokenKind Kind { get; }

        public string Text { get; }

        public int Start { get; }

        public readonly int End => Start + Text.Length;

        public Token WithText(string text, int start)
        {
            return new Token(Kind, text, start);
        }

        public bool Equals(Token other)
        {
            return Kind == other.Kind && Start == other.Start && string.Equals(Text, other.Text, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is Token token && Equals(token);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ Start;
                hash = (hash * 397) ^ (Text != null ? Text.GetHashCode() : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return Kind + "(" + Text + ")";
        }
    }
}
