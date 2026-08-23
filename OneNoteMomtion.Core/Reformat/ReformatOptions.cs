using System;
using OneNoteMomtion.Core.Highlighting.Detection;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Text;

namespace OneNoteMomtion.Core.Reformat
{
    public sealed class ReformatOptions
    {
        public BodyTextOptions BodyText { get; set; } = new BodyTextOptions();

        public TextCleanupOptions TextCleanup { get; set; } = new TextCleanupOptions();

        public CodeDetectionOptions CodeDetection { get; set; } = new CodeDetectionOptions();

        public static ReformatOptions FromCatalog(StyleCatalog catalog, BodyTextOptions? oneNoteDefaults = null)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            BodyTextOptions configured = catalog.BodyText ?? new BodyTextOptions();

            return new ReformatOptions
            {
                BodyText = new BodyTextOptions
                {
                    Font = FirstNotBlank(configured.Font, oneNoteDefaults?.Font),
                    FontSize = configured.FontSize ?? oneNoteDefaults?.FontSize,
                    FontColor = FirstNotBlank(configured.FontColor, oneNoteDefaults?.FontColor),
                },
                TextCleanup = catalog.TextCleanup ?? new TextCleanupOptions(),
                CodeDetection = catalog.CodeDetection ?? new CodeDetectionOptions(),
            };
        }

        private static string? FirstNotBlank(string? preferred, string? fallback)
        {
            return string.IsNullOrWhiteSpace(preferred) ? (string.IsNullOrWhiteSpace(fallback) ? null : fallback) : preferred;
        }
    }
}
