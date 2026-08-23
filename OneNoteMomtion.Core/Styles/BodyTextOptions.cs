namespace OneNoteMomtion.Core.Styles
{
    public sealed class BodyTextOptions
    {
        public string? Font { get; set; }

        public double? FontSize { get; set; }

        public string? FontColor { get; set; }

        public bool HasAnyValue => !string.IsNullOrWhiteSpace(Font) || FontSize.HasValue || !string.IsNullOrWhiteSpace(FontColor);
    }
}
