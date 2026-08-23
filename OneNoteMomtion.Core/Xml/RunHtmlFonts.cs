using System;
using System.Collections.Generic;
using System.Linq;

namespace OneNoteMomtion.Core.Xml
{
    public static class RunHtmlFonts
    {
        public static Dictionary<string, string> Common(string? html)
        {
            Dictionary<string, string> common = new(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(html))
            {
                return common;
            }

            List<Dictionary<string, string>> perSegment = ReadSegments(html!);
            if (perSegment.Count == 0)
            {
                return common;
            }

            foreach (string name in QuickStyleProperties.RunNames)
            {
                string? agreed = null;

                foreach (Dictionary<string, string> segment in perSegment)
                {
                    if (!segment.TryGetValue(name, out string? value))
                    {
                        agreed = null;
                        break;
                    }

                    if (agreed == null)
                    {
                        agreed = value;
                        continue;
                    }

                    if (!CssProperties.ValuesMatch(name, agreed, value))
                    {
                        agreed = null;
                        break;
                    }
                }

                if (agreed != null)
                {
                    common[name] = agreed;
                }
            }

            return common;
        }

        private static List<Dictionary<string, string>> ReadSegments(string html)
        {
            List<Dictionary<string, string>> segments = [];
            List<Dictionary<string, string>> openElements = [];
            int i = 0;

            while (i < html.Length)
            {
                if (html[i] == '<')
                {
                    int close = html.IndexOf('>', i + 1);
                    if (close < 0)
                    {
                        break;
                    }

                    ReadTag(html.Substring(i + 1, close - i - 1), openElements);
                    i = close + 1;
                    continue;
                }

                int next = html.IndexOf('<', i);
                int end = next < 0 ? html.Length : next;

                if (CarriesText(HtmlRunRewriter.Decode(html[i..end])))
                {
                    segments.Add(openElements.Count == 0 ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : openElements[^1]);
                }

                i = end;
            }

            return segments;
        }

        private static void ReadTag(string tag, List<Dictionary<string, string>> openElements)
        {
            if (tag.Length == 0)
            {
                return;
            }

            if (tag[0] == '/')
            {
                if (openElements.Count > 0)
                {
                    openElements.RemoveAt(openElements.Count - 1);
                }

                return;
            }

            if (tag[^1] == '/')
            {
                return;
            }

            Dictionary<string, string> properties = openElements.Count == 0
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(openElements[^1], StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> property in CssProperties.Parse(ReadStyleAttribute(tag)))
            {
                properties[property.Key] = property.Value;
            }

            openElements.Add(properties);
        }

        private static string? ReadStyleAttribute(string tag)
        {
            int at = tag.IndexOf("style", StringComparison.OrdinalIgnoreCase);

            while (at >= 0)
            {
                int cursor = at + "style".Length;
                while (cursor < tag.Length && char.IsWhiteSpace(tag[cursor]))
                {
                    cursor++;
                }

                if (cursor < tag.Length && tag[cursor] == '=' && (at == 0 || !char.IsLetterOrDigit(tag[at - 1])))
                {
                    cursor++;
                    while (cursor < tag.Length && char.IsWhiteSpace(tag[cursor]))
                    {
                        cursor++;
                    }

                    if (cursor < tag.Length && (tag[cursor] == '\'' || tag[cursor] == '"'))
                    {
                        int end = tag.IndexOf(tag[cursor], cursor + 1);
                        if (end > 0)
                        {
                            return HtmlRunRewriter.Decode(tag.Substring(cursor + 1, end - cursor - 1));
                        }
                    }
                }

                at = tag.IndexOf("style", at + 1, StringComparison.OrdinalIgnoreCase);
            }

            return null;
        }

        private static bool CarriesText(string text)
        {
            return text.Any(char.IsLetterOrDigit);
        }
    }
}
