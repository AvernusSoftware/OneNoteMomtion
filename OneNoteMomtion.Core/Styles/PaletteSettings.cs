using System;
using System.Collections.Generic;
using OneNoteMomtion.Core.Highlighting;

namespace OneNoteMomtion.Core.Styles
{
    public sealed class PaletteSettings
    {
        public const int OrderStep = 10;

        public Dictionary<string, PaletteEntry> Groups { get; set; } = new Dictionary<string, PaletteEntry>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, PaletteEntry> Actions { get; set; } = new Dictionary<string, PaletteEntry>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, PaletteEntry> Case { get; set; } = new Dictionary<string, PaletteEntry>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, PaletteEntry> Languages { get; set; } = new Dictionary<string, PaletteEntry>(StringComparer.OrdinalIgnoreCase);

        public static PaletteSettings CreateDefault()
        {
            return new PaletteSettings
            {
                Groups = Numbered("styles", "navigation", "code", "letterCase", "tools"),

                Actions = Merge(
                    Numbered("style.heading1", "style.heading2", "style.heading3", "style.normal"),
                    Numbered("nav.reveal", "nav.collapseAll", "nav.dump"),
                    Numbered("code.format"),
                    Numbered("case.menu"),
                    Numbered("page.reformat", "spell.disable", "spell.enable")),

                Case = Numbered("case.upper", "case.lower", "case.capitalize", "case.sentence", "case.toggle"),

                Languages = Numbered(DefaultLanguageIds()),
            };
        }

        public void FillMissingWithDefaults()
        {
            PaletteSettings defaults = CreateDefault();

            Groups = FillMissing(Groups, defaults.Groups);
            Actions = FillMissing(Actions, defaults.Actions);
            Case = FillMissing(Case, defaults.Case);
            Languages = FillMissing(Languages, defaults.Languages);
        }

        private static string[] DefaultLanguageIds()
        {
            IReadOnlyList<ISyntaxHighlightProvider> providers = SyntaxHighlighterRegistry.CreateDefault().Selectable();
            string[] ids = new string[providers.Count];

            for (int i = 0; i < providers.Count; i++)
            {
                ids[i] = providers[i].LanguageId;
            }

            return ids;
        }

        private static Dictionary<string, PaletteEntry> Numbered(params string[] keys)
        {
            Dictionary<string, PaletteEntry> entries = new(keys.Length, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < keys.Length; i++)
            {
                entries[keys[i]] = new PaletteEntry { Order = (i + 1) * OrderStep, Enabled = true };
            }

            return entries;
        }

        private static Dictionary<string, PaletteEntry> Merge(params Dictionary<string, PaletteEntry>[] parts)
        {
            Dictionary<string, PaletteEntry> merged = new(StringComparer.OrdinalIgnoreCase);

            foreach (Dictionary<string, PaletteEntry> part in parts)
            {
                foreach (KeyValuePair<string, PaletteEntry> pair in part)
                {
                    merged[pair.Key] = pair.Value;
                }
            }

            return merged;
        }

        private static Dictionary<string, PaletteEntry> FillMissing(Dictionary<string, PaletteEntry>? current, Dictionary<string, PaletteEntry> defaults)
        {
            Dictionary<string, PaletteEntry> result = current == null
                ? new Dictionary<string, PaletteEntry>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, PaletteEntry>(current, StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, PaletteEntry> pair in defaults)
            {
                if (!result.TryGetValue(pair.Key, out PaletteEntry? entry) || entry == null)
                {
                    result[pair.Key] = pair.Value.Clone();
                }
            }

            return result;
        }
    }
}
