using System;
using System.Text;

namespace OneNoteMomtion.Core.Text
{
    public static class WhitespaceTransformer
    {
        private const char NoBreakSpace = (char)0x00A0;
        private const char EnDash = (char)0x2013;
        private const string BulletMarkers = "*+-•·▪‣";
        private const string OpeningBrackets = "([{";
        private const string ClosingBrackets = ")]}";
        private const string CurlyDoubleQuotes = "“”„‟″";
        private const string CurlySingleQuotes = "‘’‚‛′";

        public static string Apply(string text, TextCleanupOptions options, bool isParagraphStart, bool isParagraphEnd, char? previousCharacter)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }

            string result = text;

            if (options.NormalizeNonBreakingSpaces)
            {
                result = result.Replace(NoBreakSpace, ' ');
            }

            if (options.NormalizeListMarkers && isParagraphStart)
            {
                result = NormalizeLeadingMarker(result);
            }

            if (options.CollapseSpaces)
            {
                result = CollapseSpaceRuns(result, previousCharacter);
            }

            if (options.SpaceAroundBrackets)
            {
                result = SpaceOutBrackets(result, previousCharacter);
            }

            if (options.NormalizeDashes)
            {
                result = ReplaceDoubleHyphens(result);
            }

            if (options.NormalizeQuotes)
            {
                result = StraightenQuotes(result);
            }

            if (options.TrimTrailing && isParagraphEnd)
            {
                result = result.TrimEnd(' ', '\t', NoBreakSpace);
            }

            return result;
        }

        private static string NormalizeLeadingMarker(string text)
        {
            int textStart = 0;
            while (textStart < text.Length && IsCollapsibleSpace(text[textStart]))
            {
                textStart++;
            }

            int markerEnd = ReadMarker(text, textStart);
            if (markerEnd < 0)
            {
                return textStart == 0 ? text : text[textStart..];
            }

            int contentStart = markerEnd;
            while (contentStart < text.Length && IsCollapsibleSpace(text[contentStart]))
            {
                contentStart++;
            }

            if (contentStart == markerEnd)
            {
                return textStart == 0 ? text : text[textStart..];
            }

            return text[textStart..markerEnd] + " " + text[contentStart..];
        }

        private static int ReadMarker(string text, int start)
        {
            if (start >= text.Length)
            {
                return -1;
            }

            if (BulletMarkers.Contains(text[start]))
            {
                return start + 1;
            }

            int digitEnd = start;
            while (digitEnd < text.Length && digitEnd - start < 3 && char.IsAsciiDigit(text[digitEnd]))
            {
                digitEnd++;
            }

            if (digitEnd > start && digitEnd < text.Length && (text[digitEnd] == '.' || text[digitEnd] == ')'))
            {
                return digitEnd + 1;
            }

            return -1;
        }

        private static string CollapseSpaceRuns(string text, char? previousCharacter)
        {
            StringBuilder builder = new(text.Length);
            bool carriedSpace = previousCharacter.HasValue && IsCollapsibleSpace(previousCharacter.Value);
            int i = 0;

            while (i < text.Length)
            {
                if (!IsCollapsibleSpace(text[i]))
                {
                    builder.Append(text[i]);
                    carriedSpace = false;
                    i++;
                    continue;
                }

                int start = i;
                while (i < text.Length && IsCollapsibleSpace(text[i]))
                {
                    i++;
                }

                if (carriedSpace)
                {
                    continue;
                }

                builder.Append(i - start == 1 ? text[start] : ' ');
                carriedSpace = true;
            }

            return builder.ToString();
        }

        private static string SpaceOutBrackets(string text, char? previousCharacter)
        {
            StringBuilder? builder = null;
            char? before = previousCharacter;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (before.HasValue && NeedsSpaceBetween(before.Value, c, text, i))
                {
                    builder ??= new StringBuilder(text.Length + 4).Append(text, 0, i);
                    builder.Append(' ');
                }

                builder?.Append(c);
                before = c;
            }

            return builder == null ? text : builder.ToString();
        }

        private static bool NeedsSpaceBetween(char before, char after, string text, int at)
        {
            if (ClosingBrackets.Contains(before))
            {
                return IsWordCharacter(after) || OpeningBrackets.Contains(after);
            }

            if (OpeningBrackets.Contains(after))
            {
                return IsWordCharacter(before) && !ClosesImmediately(after, text, at);
            }

            return false;
        }

        private static bool ClosesImmediately(char opening, string text, int at)
        {
            if (at + 1 >= text.Length)
            {
                return false;
            }

            return text[at + 1] == ClosingBrackets[OpeningBrackets.IndexOf(opening)];
        }

        private static bool IsWordCharacter(char c)
        {
            return char.IsLetterOrDigit(c);
        }

        private static string ReplaceDoubleHyphens(string text)
        {
            if (text.IndexOf("--", StringComparison.Ordinal) < 0)
            {
                return text;
            }

            StringBuilder builder = new(text.Length);
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] != '-')
                {
                    builder.Append(text[i]);
                    i++;
                    continue;
                }

                int start = i;
                while (i < text.Length && text[i] == '-')
                {
                    i++;
                }

                if (i - start == 2)
                {
                    builder.Append(EnDash);
                }
                else
                {
                    builder.Append(text, start, i - start);
                }
            }

            return builder.ToString();
        }

        private static string StraightenQuotes(string text)
        {
            StringBuilder? builder = null;

            for (int i = 0; i < text.Length; i++)
            {
                char replacement = text[i];

                if (CurlyDoubleQuotes.Contains(replacement))
                {
                    replacement = '"';
                }
                else if (CurlySingleQuotes.Contains(replacement))
                {
                    replacement = '\'';
                }

                if (builder == null && replacement == text[i])
                {
                    continue;
                }

                builder ??= new StringBuilder(text.Length).Append(text, 0, i);
                builder.Append(replacement);
            }

            return builder == null ? text : builder.ToString();
        }

        private static bool IsCollapsibleSpace(char c)
        {
            return c == ' ' || c == '\t';
        }
    }
}