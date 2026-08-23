using System.Collections.Generic;

namespace OneNoteMomtion.Core.Styles
{
    public sealed class PaletteEntry
    {
        public int Order { get; set; }

        public bool Enabled { get; set; } = true;

        public string? Label { get; set; }

        public string? Hint { get; set; }

        public List<string>? Keywords { get; set; }

        public PaletteEntry Clone()
        {
            return new PaletteEntry
            {
                Order = Order,
                Enabled = Enabled,
                Label = Label,
                Hint = Hint,
                Keywords = Keywords == null ? null : [.. Keywords],
            };
        }
    }
}
