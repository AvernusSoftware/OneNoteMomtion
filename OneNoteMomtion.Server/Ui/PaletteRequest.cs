using OneNoteMomtion.Server.Commands;

namespace OneNoteMomtion.Server.Ui
{
    internal sealed class PaletteRequest
    {
        public string? RequestId { get; set; }

        public string? Command { get; set; }

        public string? Argument { get; set; }
    }
}
