using System;
using System.Collections.Generic;
using OneNoteMomtion.Core.Localization;

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

        public string Describe(Localizer texts)
        {
            ArgumentNullException.ThrowIfNull(texts);

            if (Total == 0)
            {
                return texts.Get(TextKeys.EngineReformatNothing);
            }

            List<string> parts = [];
            Add(parts, texts, Headings, TextKeys.EngineReformatHeadings);
            Add(parts, texts, Paragraphs, TextKeys.EngineReformatParagraphs);
            Add(parts, texts, TextFixes, TextKeys.EngineReformatTextFixes);
            Add(parts, texts, BlankParagraphs, TextKeys.EngineReformatBlankParagraphs);
            Add(parts, texts, CodeBlocks, TextKeys.EngineReformatCodeBlocks);

            return texts.Format(TextKeys.EngineReformatDone, ("parts", string.Join(", ", parts)));
        }

        private static void Add(List<string> parts, Localizer texts, int count, string key)
        {
            if (count > 0)
            {
                parts.Add(texts.Plural(key, count));
            }
        }
    }
}
