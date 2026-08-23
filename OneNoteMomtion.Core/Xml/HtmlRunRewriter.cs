using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OneNoteMomtion.Core.Xml
{
    public static class HtmlRunRewriter
    {
        private const char Space = (char)0x20;
        private const char Tab = (char)0x09;
        private const char NoBreakSpace = (char)0xA0;
        private const string StyleAttributeName = "style";
        private const string SpanTagName = "<span";

        private struct TextSegment
        {
            public int RawStart;
            public int RawLength;
            public string Decoded;
        }

        public static string ExtractPlainText(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            StringBuilder builder = new(html.Length);
            foreach (TextSegment segment in ScanTextSegments(html))
            {
                builder.Append(segment.Decoded);
            }

            return builder.ToString();
        }

        public static string RewriteText(string html, Func<string, string> transform)
        {
            ArgumentNullException.ThrowIfNull(transform);

            if (string.IsNullOrEmpty(html))
            {
                return Encode(transform(string.Empty));
            }

            List<TextSegment> segments = ScanTextSegments(html);
            if (segments.Count == 0)
            {
                return html;
            }

            StringBuilder plainBuilder = new();
            foreach (TextSegment segment in segments)
            {
                plainBuilder.Append(segment.Decoded);
            }

            string plain = plainBuilder.ToString();
            string transformed = transform(plain) ?? string.Empty;
            if (string.Equals(plain, transformed, StringComparison.Ordinal))
            {
                return html;
            }

            StringBuilder result = new(html.Length + 16);
            int cursorInHtml = 0;
            int cursorInPlain = 0;
            bool lengthPreserved = transformed.Length == plain.Length;

            for (int i = 0; i < segments.Count; i++)
            {
                TextSegment segment = segments[i];
                result.Append(html, cursorInHtml, segment.RawStart - cursorInHtml);

                string replacement;
                if (lengthPreserved)
                {
                    replacement = transformed.Substring(cursorInPlain, segment.Decoded.Length);
                }
                else
                {
                    replacement = i == 0 ? transformed : string.Empty;
                }

                result.Append(Encode(replacement));
                cursorInPlain += segment.Decoded.Length;
                cursorInHtml = segment.RawStart + segment.RawLength;
            }

            result.Append(html, cursorInHtml, html.Length - cursorInHtml);
            return result.ToString();
        }

        public static string StripTags(string html)
        {
            return Encode(ExtractPlainText(html));
        }

        public static string RemoveLeadingCharacters(string html, ref int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(html))
            {
                return html ?? string.Empty;
            }

            List<TextSegment> segments = ScanTextSegments(html);
            if (segments.Count == 0)
            {
                return html;
            }

            StringBuilder result = new(html.Length);
            int cursorInHtml = 0;

            foreach (TextSegment segment in segments)
            {
                result.Append(html, cursorInHtml, segment.RawStart - cursorInHtml);

                if (count <= 0)
                {
                    result.Append(html, segment.RawStart, segment.RawLength);
                }
                else if (count >= segment.Decoded.Length)
                {
                    count -= segment.Decoded.Length;
                }
                else
                {
                    result.Append(Encode(segment.Decoded[count..]));
                    count = 0;
                }

                cursorInHtml = segment.RawStart + segment.RawLength;
            }

            result.Append(html, cursorInHtml, html.Length - cursorInHtml);
            return result.ToString();
        }

        public static string ReplaceText(string html, string newPlainText)
        {
            return RewriteText(html, _ => newPlainText);
        }

        public static string RewriteSegments(string html, Func<SegmentContext, string> transform)
        {
            ArgumentNullException.ThrowIfNull(transform);

            IReadOnlyList<string> segments = ReadTextSegments(html);
            if (segments.Count == 0)
            {
                return html ?? string.Empty;
            }

            string[] rewritten = new string[segments.Count];
            char? previous = null;

            for (int i = 0; i < segments.Count; i++)
            {
                rewritten[i] = transform(new SegmentContext(segments[i], i == 0, i == segments.Count - 1, previous)) ?? string.Empty;

                if (rewritten[i].Length > 0)
                {
                    previous = rewritten[i][^1];
                }
            }

            return WriteTextSegments(html, rewritten);
        }

        public static IReadOnlyList<string> ReadTextSegments(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return [];
            }

            List<TextSegment> segments = ScanTextSegments(html, keepNoBreakSpaces: true);
            List<string> decoded = new(segments.Count);

            foreach (TextSegment segment in segments)
            {
                decoded.Add(segment.Decoded);
            }

            return decoded;
        }

        public static string WriteTextSegments(string html, IReadOnlyList<string> replacements)
        {
            ArgumentNullException.ThrowIfNull(replacements);

            if (string.IsNullOrEmpty(html))
            {
                return html ?? string.Empty;
            }

            List<TextSegment> segments = ScanTextSegments(html, keepNoBreakSpaces: true);
            if (segments.Count != replacements.Count)
            {
                throw new ArgumentException("The run has " + segments.Count + " text segments but " + replacements.Count + " replacements were given.", nameof(replacements));
            }

            StringBuilder result = new(html.Length + 16);
            int cursorInHtml = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                TextSegment segment = segments[i];
                result.Append(html, cursorInHtml, segment.RawStart - cursorInHtml);
                result.Append(EncodePreservingNoBreakSpaces(replacements[i]));
                cursorInHtml = segment.RawStart + segment.RawLength;
            }

            result.Append(html, cursorInHtml, html.Length - cursorInHtml);
            return result.ToString();
        }

        public static string EncodePreservingNoBreakSpaces(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            StringBuilder builder = new(text.Length + 8);
            foreach (char c in text)
            {
                if (c == NoBreakSpace)
                {
                    builder.Append("&nbsp;");
                }
                else
                {
                    AppendEscaped(builder, c);
                }
            }

            return builder.ToString();
        }

        public static string Encode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            StringBuilder builder = new(text.Length + 8);
            foreach (char c in text)
            {
                AppendEscaped(builder, c);
            }

            return builder.ToString();
        }

        public static string EncodePreservingIndent(string text, int tabWidth = 4)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            StringBuilder builder = new(text.Length + 16);
            foreach (char c in text)
            {
                if (c == Space)
                {
                    builder.Append("&nbsp;");
                }
                else if (c == Tab)
                {
                    for (int t = 0; t < tabWidth; t++)
                    {
                        builder.Append("&nbsp;");
                    }
                }
                else
                {
                    AppendEscaped(builder, c);
                }
            }

            return builder.ToString();
        }

        public static string RemoveStyleProperties(string html, IReadOnlyCollection<string> propertyNames)
        {
            ArgumentNullException.ThrowIfNull(propertyNames);

            if (string.IsNullOrEmpty(html) || propertyNames.Count == 0 || html.IndexOf('<') < 0)
            {
                return html ?? string.Empty;
            }

            StringBuilder result = new(html.Length);
            int cursor = 0;
            int i = 0;
            bool anyChange = false;

            while (i < html.Length)
            {
                if (html[i] != '<')
                {
                    i++;
                    continue;
                }

                int close = html.IndexOf('>', i + 1);
                if (close < 0)
                {
                    break;
                }

                string tag = html.Substring(i, close - i + 1);
                string rewritten = RemoveStylePropertiesFromTag(tag, propertyNames);

                if (!string.Equals(tag, rewritten, StringComparison.Ordinal))
                {
                    result.Append(html, cursor, i - cursor);
                    result.Append(rewritten);
                    cursor = close + 1;
                    anyChange = true;
                }

                i = close + 1;
            }

            if (!anyChange)
            {
                return html;
            }

            result.Append(html, cursor, html.Length - cursor);
            return result.ToString();
        }

        public static string RemoveEmptySpans(string html)
        {
            if (string.IsNullOrEmpty(html) || html.IndexOf('<') < 0)
            {
                return html ?? string.Empty;
            }

            List<(int Start, int Length)> removals = [];
            Stack<(int Start, int Length, bool Bare)> spans = new();
            int i = 0;

            while (i < html.Length)
            {
                if (html[i] != '<')
                {
                    i++;
                    continue;
                }

                int close = html.IndexOf('>', i + 1);
                if (close < 0)
                {
                    break;
                }

                string tag = html.Substring(i, close - i + 1);

                if (IsClosingSpanTag(tag))
                {
                    if (spans.Count > 0)
                    {
                        (int Start, int Length, bool Bare) opened = spans.Pop();
                        if (opened.Bare)
                        {
                            removals.Add((opened.Start, opened.Length));
                            removals.Add((i, close - i + 1));
                        }
                    }
                }
                else if (IsOpeningSpanTag(tag, out bool bare))
                {
                    spans.Push((i, close - i + 1, bare));
                }

                i = close + 1;
            }

            if (removals.Count == 0)
            {
                return html;
            }

            removals.Sort((left, right) => left.Start.CompareTo(right.Start));

            StringBuilder result = new(html.Length);
            int cursor = 0;

            foreach ((int Start, int Length) removal in removals)
            {
                result.Append(html, cursor, removal.Start - cursor);
                cursor = removal.Start + removal.Length;
            }

            result.Append(html, cursor, html.Length - cursor);
            return result.ToString();
        }

        public static string Decode(string html)
        {
            return Decode(html, keepNoBreakSpaces: false);
        }

        private static string Decode(string html, bool keepNoBreakSpaces)
        {
            if (string.IsNullOrEmpty(html) || html.IndexOf('&') < 0)
            {
                return html ?? string.Empty;
            }

            StringBuilder builder = new(html.Length);
            int i = 0;
            while (i < html.Length)
            {
                char c = html[i];
                if (c != '&')
                {
                    builder.Append(c);
                    i++;
                    continue;
                }

                int end = html.IndexOf(';', i + 1);
                if (end < 0 || end - i > 10)
                {
                    builder.Append(c);
                    i++;
                    continue;
                }

                string? decoded = DecodeEntity(html.Substring(i + 1, end - i - 1), keepNoBreakSpaces);
                if (decoded == null)
                {
                    builder.Append(c);
                    i++;
                    continue;
                }

                builder.Append(decoded);
                i = end + 1;
            }

            return builder.ToString();
        }

        private static string RemoveStylePropertiesFromTag(string tag, IReadOnlyCollection<string> propertyNames)
        {
            int at = IndexOfStyleAttribute(tag);
            if (at < 0)
            {
                return tag;
            }

            int assignment = tag.IndexOf('=', at + StyleAttributeName.Length);
            if (assignment < 0)
            {
                return tag;
            }

            int valueStart = assignment + 1;
            while (valueStart < tag.Length && char.IsWhiteSpace(tag[valueStart]))
            {
                valueStart++;
            }

            if (valueStart >= tag.Length)
            {
                return tag;
            }

            char quote = tag[valueStart];
            int valueEnd;

            if (quote == '\'' || quote == '"')
            {
                valueStart++;
                valueEnd = tag.IndexOf(quote, valueStart);
                if (valueEnd < 0)
                {
                    return tag;
                }
            }
            else
            {
                quote = '\0';
                valueEnd = valueStart;
                while (valueEnd < tag.Length && !char.IsWhiteSpace(tag[valueEnd]) && tag[valueEnd] != '>' && tag[valueEnd] != '/')
                {
                    valueEnd++;
                }
            }

            Dictionary<string, string> properties = CssProperties.Parse(Decode(tag[valueStart..valueEnd]));
            bool removedAny = false;

            foreach (string name in propertyNames)
            {
                removedAny |= properties.Remove(name);
            }

            if (!removedAny)
            {
                return tag;
            }

            if (properties.Count > 0)
            {
                return tag[..valueStart] + Encode(CssProperties.Format(properties)) + tag[valueEnd..];
            }

            int attributeStart = at;
            while (attributeStart > 0 && char.IsWhiteSpace(tag[attributeStart - 1]))
            {
                attributeStart--;
            }

            return tag[..attributeStart] + tag[(quote == '\0' ? valueEnd : valueEnd + 1)..];
        }

        private static int IndexOfStyleAttribute(string tag)
        {
            int at = 0;

            while (true)
            {
                at = tag.IndexOf(StyleAttributeName, at, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                {
                    return -1;
                }

                bool startsAttribute = at > 0 && !char.IsLetterOrDigit(tag[at - 1]) && tag[at - 1] != '-';
                int after = at + StyleAttributeName.Length;

                while (after < tag.Length && char.IsWhiteSpace(tag[after]))
                {
                    after++;
                }

                if (startsAttribute && after < tag.Length && tag[after] == '=')
                {
                    return at;
                }

                at += StyleAttributeName.Length;
            }
        }

        private static bool IsOpeningSpanTag(string tag, out bool bare)
        {
            bare = false;

            if (tag.Length < SpanTagName.Length + 1 || !tag.StartsWith(SpanTagName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string inner = tag.Substring(SpanTagName.Length, tag.Length - SpanTagName.Length - 1).Trim();

            if (inner.EndsWith('/'))
            {
                return false;
            }

            if (inner.Length > 0 && !char.IsWhiteSpace(tag[SpanTagName.Length]))
            {
                return false;
            }

            bare = inner.Length == 0;
            return true;
        }

        private static bool IsClosingSpanTag(string tag)
        {
            if (tag.Length < 4 || !tag.StartsWith("</", StringComparison.Ordinal))
            {
                return false;
            }

            return string.Equals(tag[2..^1].Trim(), "span", StringComparison.OrdinalIgnoreCase);
        }

        private static void AppendEscaped(StringBuilder builder, char c)
        {
            switch (c)
            {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        private static string? DecodeEntity(string entity, bool keepNoBreakSpaces)
        {
            switch (entity)
            {
                case "amp":
                    return "&";
                case "lt":
                    return "<";
                case "gt":
                    return ">";
                case "quot":
                    return "\"";
                case "apos":
                    return "'";
                case "nbsp":
                    return keepNoBreakSpaces ? NoBreakSpace.ToString() : Space.ToString();
            }

            if (entity.Length > 1 && entity[0] == '#')
            {
                bool hex = entity.Length > 2 && (entity[1] == 'x' || entity[1] == 'X');
                string digits = hex ? entity[2..] : entity[1..];
                bool parsed = hex ? int.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code) : int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out code);

                if (parsed && code > 0 && code <= 0x10FFFF)
                {
                    if (code != NoBreakSpace)
                    {
                        return char.ConvertFromUtf32(code);
                    }

                    return keepNoBreakSpaces ? NoBreakSpace.ToString() : Space.ToString();
                }
            }

            return null;
        }

        private static List<TextSegment> ScanTextSegments(string html)
        {
            return ScanTextSegments(html, keepNoBreakSpaces: false);
        }

        private static List<TextSegment> ScanTextSegments(string html, bool keepNoBreakSpaces)
        {
            List<TextSegment> segments = [];
            int i = 0;

            while (i < html.Length)
            {
                if (html[i] == '<')
                {
                    int close = html.IndexOf('>', i + 1);
                    if (close < 0)
                    {
                        AddSegment(segments, html, i, html.Length - i, keepNoBreakSpaces);
                        break;
                    }

                    i = close + 1;
                    continue;
                }

                int next = html.IndexOf('<', i);
                int length = (next < 0 ? html.Length : next) - i;
                AddSegment(segments, html, i, length, keepNoBreakSpaces);
                i += length;
            }

            return segments;
        }

        private static void AddSegment(List<TextSegment> segments, string html, int start, int length, bool keepNoBreakSpaces)
        {
            if (length <= 0)
            {
                return;
            }

            segments.Add(new TextSegment
            {
                RawStart = start,
                RawLength = length,
                Decoded = Decode(html.Substring(start, length), keepNoBreakSpaces),
            });
        }
    }
}
