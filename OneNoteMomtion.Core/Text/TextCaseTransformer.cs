using System;
using System.Globalization;
using System.Text;

namespace OneNoteMomtion.Core.Text
{
    public static class TextCaseTransformer
    {
        private const char RightSingleQuote = '’';
        private const char Ellipsis = '…';
        private const char LeftDoubleQuote = '“';
        private const char LeftSingleQuote = '‘';
        private const char LeftGuillemet = '«';

        public static string Apply(string? text, TextCaseMode mode, CultureInfo? culture = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }

            string value = text!;
            CultureInfo effective = culture ?? CultureInfo.CurrentCulture;

            switch (mode)
            {
                case TextCaseMode.Upper:
                    return MapChars(value, c => char.ToUpper(c, effective));

                case TextCaseMode.Lower:
                    return MapChars(value, c => char.ToLower(c, effective));

                case TextCaseMode.Toggle:
                    return MapChars(value, c => char.IsUpper(c) ? char.ToLower(c, effective) : char.ToUpper(c, effective));

                case TextCaseMode.Capitalize:
                    return Capitalize(value, effective);

                case TextCaseMode.Sentence:
                    return SentenceCase(value, effective);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown letter-case mode.");
            }
        }

        private static string Capitalize(string text, CultureInfo culture)
        {
            StringBuilder builder = new(text.Length);
            bool atWordStart = true;

            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(atWordStart ? char.ToUpper(c, culture) : char.ToLower(c, culture));
                    atWordStart = false;
                }
                else if (c == '\'' || c == RightSingleQuote)
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append(c);
                    atWordStart = true;
                }
            }

            return builder.ToString();
        }

        private static string SentenceCase(string text, CultureInfo culture)
        {
            StringBuilder builder = new(text.Length);
            bool atSentenceStart = true;

            foreach (char c in text)
            {
                if (IsSentenceTerminator(c))
                {
                    builder.Append(c);
                    atSentenceStart = true;
                    continue;
                }

                if (char.IsWhiteSpace(c) || IsSentenceOpener(c))
                {
                    builder.Append(c);
                    continue;
                }

                if (atSentenceStart && char.IsLetter(c))
                {
                    builder.Append(char.ToUpper(c, culture));
                    atSentenceStart = false;
                    continue;
                }

                builder.Append(char.ToLower(c, culture));
                atSentenceStart = false;
            }

            return builder.ToString();
        }

        private static string MapChars(string text, Func<char, char> map)
        {
            StringBuilder builder = new(text.Length);
            foreach (char c in text)
            {
                builder.Append(map(c));
            }

            return builder.ToString();
        }

        private static bool IsSentenceTerminator(char c)
        {
            return c == '.' || c == '!' || c == '?' || c == Ellipsis || c == '\n' || c == '\r';
        }

        private static bool IsSentenceOpener(char c)
        {
            return c == '"' || c == '\'' || c == '(' || c == '[' || c == '{' || c == LeftDoubleQuote || c == LeftSingleQuote || c == LeftGuillemet;
        }
    }
}
