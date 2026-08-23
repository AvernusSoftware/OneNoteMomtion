namespace OneNoteMomtion.Server.Ui
{
    internal sealed class PaletteResponse
    {
        public string? RequestId { get; set; }

        public bool Success { get; set; }

        public bool Changed { get; set; }

        public string? Message { get; set; }

        public object? Data { get; set; }
    }
}
