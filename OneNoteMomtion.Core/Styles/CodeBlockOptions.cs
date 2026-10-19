namespace OneNoteMomtion.Core.Styles
{
    public sealed class CodeBlockOptions
    {
        public string Font { get; set; } = "Consolas";

        public double FontSize { get; set; } = 11.0;

        public string ForegroundColor { get; set; } = "#1F2328";

        public bool WrapInTable { get; set; } = true;

        public string ShadingColor { get; set; } = "#F7F6F3";

        public bool BordersVisible { get; set; }

        public double TableWidth { get; set; } = 600.0;

        public int TabWidth { get; set; } = 4;

        public bool DisableSpellCheck { get; set; } = true;

        public bool ShowLanguageHeader { get; set; } = true;

        public bool ShowCopyButton { get; set; } = true;

        /// <remarks><c>null</c> shows the button text of the interface language.</remarks>
        public string? CopyButtonText { get; set; }

        public double HeaderFontSize { get; set; } = 9.0;

        public bool HeaderBold { get; set; } = true;

        public string HeaderForegroundColor { get; set; } = "#6E7781";

        public string HeaderShadingColor { get; set; } = "#EDEBE5";
    }
}
