namespace OneNoteMomtion.Core.Highlighting.Detection
{
    public sealed class CodeDetectionOptions
    {
        public bool Enabled { get; set; } = true;

        public bool ConvertForeignBlocks { get; set; } = true;

        public double MinScore { get; set; } = 0.35;

        public double MinMargin { get; set; } = 0.15;
    }
}