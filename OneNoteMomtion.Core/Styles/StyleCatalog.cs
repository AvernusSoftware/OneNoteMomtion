using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OneNoteMomtion.Core.Highlighting.Detection;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Core.Text;

namespace OneNoteMomtion.Core.Styles
{
    public sealed class StyleCatalog
    {
        public const int CurrentVersion = 2;

        private const string LegacyCopyButtonText = "Copy";

        public int Version { get; set; } = CurrentVersion;

        public string Language { get; set; } = UiLanguage.Auto;

        public Dictionary<string, StyleDefinition> Styles { get; set; } = new Dictionary<string, StyleDefinition>(StringComparer.OrdinalIgnoreCase);

        public CodeBlockOptions CodeBlock { get; set; } = new CodeBlockOptions();

        public BodyTextOptions BodyText { get; set; } = new BodyTextOptions();

        public TextCleanupOptions TextCleanup { get; set; } = new TextCleanupOptions();

        public CodeDetectionOptions CodeDetection { get; set; } = new CodeDetectionOptions();

        public Dictionary<string, string> HighlightTheme { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public PaletteSettings Palette { get; set; } = new PaletteSettings();

        public bool AutoMarkdownTyping { get; set; } = true;

        public bool AutoNormalAfterHeading { get; set; } = true;

        public int TypingApplyDelayMs { get; set; } = 120;

        public string PaletteHotkey { get; set; } = "Ctrl+Space";

        public StyleDefinition? Get(string? name)
        {
            if (string.IsNullOrEmpty(name) || Styles == null)
            {
                return null;
            }

            return Styles.TryGetValue(name!, out StyleDefinition? style) ? style : null;
        }

        public void FillMissingWithDefaults()
        {
            if (string.IsNullOrWhiteSpace(Language))
            {
                Language = UiLanguage.Auto;
            }

            Styles = WithOrdinalIgnoreCase(Styles);
            HighlightTheme = WithOrdinalIgnoreCase(HighlightTheme);

            StyleCatalog defaults = CreateDefault();
            foreach (KeyValuePair<string, StyleDefinition> pair in defaults.Styles)
            {
                if (!Styles.TryGetValue(pair.Key, out StyleDefinition? value) || value == null)
                {
                    value = pair.Value;
                    Styles[pair.Key] = value;
                }
            }

            Styles.Remove(StyleNames.Normal);

            foreach (StyleDefinition style in Styles.Values)
            {
                style?.MigrateLegacyColors();
            }

            CodeBlock ??= defaults.CodeBlock;

            // Before version 2 every file got "Copy" written out as the default. Clearing it lets the button
            // follow the interface language; a "Copy" written after that is the user's own choice and stays.
            if (Version < CurrentVersion && CodeBlock.CopyButtonText == LegacyCopyButtonText)
            {
                CodeBlock.CopyButtonText = null;
            }

            Version = CurrentVersion;
            BodyText ??= new BodyTextOptions();
            TextCleanup ??= new TextCleanupOptions();
            CodeDetection ??= new CodeDetectionOptions();

            Palette ??= new PaletteSettings();
            Palette.FillMissingWithDefaults();

            foreach (KeyValuePair<string, string> pair in defaults.HighlightTheme)
            {
                if (!HighlightTheme.ContainsKey(pair.Key))
                {
                    HighlightTheme[pair.Key] = pair.Value;
                }
            }

            if (TypingApplyDelayMs < 50)
            {
                TypingApplyDelayMs = 50;
            }
            else if (TypingApplyDelayMs > 1000)
            {
                TypingApplyDelayMs = 1000;
            }
        }

        private static Dictionary<string, TValue> WithOrdinalIgnoreCase<TValue>(Dictionary<string, TValue> source)
        {
            if (source == null)
            {
                return new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
            }

            return ReferenceEquals(source.Comparer, StringComparer.OrdinalIgnoreCase) ? source : new Dictionary<string, TValue>(source, StringComparer.OrdinalIgnoreCase);
        }

        public static StyleCatalog CreateDefault()
        {
            const string HeadingFontColor = "#2E75B5";

            StyleCatalog catalog = new()
            {
                Styles = new Dictionary<string, StyleDefinition>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        StyleNames.Heading1,
                        new StyleDefinition
                        {
                            DisplayName = "Heading 1",
                            Font = "Segoe UI",
                            FontSize = 14.5,
                            FontColor = HeadingFontColor,
                            Bold = true,
                            SpaceBefore = 10.0,
                            SpaceAfter = 4.0,
                        }
                    },
                    {
                        StyleNames.Heading2,
                        new StyleDefinition
                        {
                            DisplayName = "Heading 2",
                            Font = "Segoe UI",
                            FontSize = 13.5,
                            FontColor = HeadingFontColor,
                            Bold = false,
                            SpaceBefore = 10.0,
                            SpaceAfter = 2.0,
                        }
                    },
                    {
                        StyleNames.Heading3,
                        new StyleDefinition
                        {
                            DisplayName = "Heading 3",
                            Font = "Segoe UI",
                            FontSize = 11.5,
                            FontColor = HeadingFontColor,
                            Bold = false,
                            SpaceBefore = 4.0,
                            SpaceAfter = 2.0,
                        }
                    },
                },
                CodeBlock = new CodeBlockOptions(),
                HighlightTheme = CreateDefaultTheme(),
                Palette = PaletteSettings.CreateDefault(),
            };

            return catalog;
        }

        public static Dictionary<string, string> CreateDefaultTheme()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Plain", "#1F2328" },
                { "Keyword", "#0033B3" },
                { "ControlKeyword", "#9E2FA0" },
                { "Type", "#008080" },
                { "String", "#067D17" },
                { "Number", "#1750EB" },
                { "Comment", "#8C8C8C" },
                { "Operator", "#1F2328" },
                { "Punctuation", "#5A5A5A" },
                { "Identifier", "#1F2328" },
                { "Preprocessor", "#9E880D" },
                { "Annotation", "#9E880D" },
                { "PropertyName", "#871094" },
                { "TagName", "#0033B3" },
                { "AttributeName", "#174AD4" },
            };
        }

    }
}
