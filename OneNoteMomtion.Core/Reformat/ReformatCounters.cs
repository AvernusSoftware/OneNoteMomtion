using System.Collections.Generic;
using System.Globalization;

namespace OneNoteMomtion.Core.Reformat
{
    public sealed class ReformatCounters
    {
        public int Headings { get; set; }

        public int Paragraphs { get; set; }

        public int TextFixes { get; set; }

        public int BlankParagraphs { get; set; }

        public int CodeBlocks { get; set; }

        public int Total => Headings + Paragraphs + TextFixes + BlankParagraphs + CodeBlocks;

        public string Describe()
        {
            if (Total == 0)
            {
                return "Nothing on this page needed reformatting.";
            }

            List<string> parts = [];
            Add(parts, Headings, "heading", "headings");
            Add(parts, Paragraphs, "paragraph", "paragraphs");
            Add(parts, TextFixes, "text fix", "text fixes");
            Add(parts, BlankParagraphs, "blank paragraph", "blank paragraphs");
            Add(parts, CodeBlocks, "code block", "code blocks");

            return "Reformatted " + string.Join(", ", parts) + ".";
        }

        private static void Add(List<string> parts, int count, string singular, string plural)
        {
            if (count > 0)
            {
                parts.Add(count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? singular : plural));
            }
        }
    }
}
