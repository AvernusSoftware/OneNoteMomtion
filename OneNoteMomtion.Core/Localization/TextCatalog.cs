using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace OneNoteMomtion.Core.Localization
{
    /// <remarks>
    /// Every <c>Localization/xx.json</c> embedded in this assembly is a language, named by its file. Adding a
    /// language means adding its file - and, if its plural forms differ from the existing ones, a rule in
    /// <see cref="PluralRules"/>.
    /// </remarks>
    public static class TextCatalog
    {
        private const string ResourcePrefix = "OneNoteMomtion.Core.Localization.";
        private const string ResourceSuffix = ".json";

        private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Languages = new(Load);
        private static readonly ConcurrentDictionary<string, IReadOnlyList<string>> Translations = new(StringComparer.Ordinal);

        public static IReadOnlyCollection<string> Available => Languages.Value.Keys;

        public static IReadOnlyDictionary<string, string> TextsOf(string language)
        {
            return Languages.Value.TryGetValue(language, out Dictionary<string, string>? texts) ? texts : Languages.Value[UiLanguage.English];
        }

        public static IReadOnlyList<string> EveryTranslationOf(string key)
        {
            return Translations.GetOrAdd(key, _ => [.. Languages.Value.Values.Select(texts => texts.TryGetValue(key, out string? text) ? text : null).OfType<string>().Where(text => text.Length > 0).Distinct()]);
        }

        private static Dictionary<string, Dictionary<string, string>> Load()
        {
            Assembly assembly = typeof(TextCatalog).Assembly;
            Dictionary<string, Dictionary<string, string>> languages = new(StringComparer.OrdinalIgnoreCase);

            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                using Stream stream = assembly.GetManifestResourceStream(name)!;
                string language = name[ResourcePrefix.Length..^ResourceSuffix.Length];
                languages[language] = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
            }

            return languages;
        }
    }
}
