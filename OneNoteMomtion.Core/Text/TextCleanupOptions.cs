namespace OneNoteMomtion.Core.Text
{
    public sealed class TextCleanupOptions
    {
        public bool TrimTrailing { get; set; } = true;

        public bool CollapseSpaces { get; set; } = true;

        public bool NormalizeListMarkers { get; set; } = true;

        public bool SpaceAroundBrackets { get; set; } = true;

        public bool CollapseEmptyParagraphs { get; set; } = true;

        public bool CleanPastedFormatting { get; set; } = true;

        public bool NormalizeNonBreakingSpaces { get; set; } = true;

        public bool NormalizeDashes { get; set; } = true;

        public bool NormalizeQuotes { get; set; } = true;

        public bool RewritesText => TrimTrailing || CollapseSpaces || NormalizeListMarkers || SpaceAroundBrackets || NormalizeNonBreakingSpaces || NormalizeDashes || NormalizeQuotes;
    }
}
