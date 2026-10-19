using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.AddIn
{
    /// <summary>
    /// The ribbon's labels and tips in the interface language. They come from the same language files as the
    /// engine's texts (OneNoteMomtion.Core/Localization, embedded in this assembly too) and the language is read
    /// here, from styles.json, so that a ribbon callback never waits for the engine to start or answer.
    /// The language choice mirrors UiLanguage.Resolve in OneNoteMomtion.Core, which this assembly cannot reference.
    /// </summary>
    internal static class RibbonTexts
    {
        private const string ResourcePrefix = "OneNoteMomtion.AddIn.Localization.";
        private const string ResourceSuffix = ".json";
        private const string TextPrefix = "ribbon.";
        private const string English = "en";
        private const string Auto = "auto";

        // styles.json may hold comments, which no JSON reader shipped with .NET Framework accepts - and the one
        // value needed here is a plain top-level string.
        private static readonly Regex LanguageSetting = new("\"language\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static IReadOnlyDictionary<string, string> Load()
        {
            string language = ResolveLanguage(ReadLanguageSetting());
            Dictionary<string, string> texts = ReadLanguage(English);

            if (language != English)
            {
                foreach (KeyValuePair<string, string> pair in ReadLanguage(language))
                {
                    texts[pair.Key] = pair.Value;
                }
            }

            return texts;
        }

        private static string? ReadLanguageSetting()
        {
            try
            {
                if (!File.Exists(SettingsWatcher.SettingsPath))
                {
                    return null;
                }

                Match match = LanguageSetting.Match(File.ReadAllText(SettingsWatcher.SettingsPath));
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (IOException ex)
            {
                ShimLog.Warn("Failed to read the interface language from styles.json: " + ex.Message);
                return null;
            }
        }

        private static string ResolveLanguage(string? setting)
        {
            string wanted = string.IsNullOrWhiteSpace(setting) || string.Equals(setting!.Trim(), Auto, StringComparison.OrdinalIgnoreCase) ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName : setting.Trim();
            wanted = wanted.Split('-', '_')[0];

            return AvailableLanguages().FirstOrDefault(language => string.Equals(language, wanted, StringComparison.OrdinalIgnoreCase)) ?? English;
        }

        private static IEnumerable<string> AvailableLanguages()
        {
            return typeof(RibbonTexts).Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                .Select(name => name.Substring(ResourcePrefix.Length, name.Length - ResourcePrefix.Length - ResourceSuffix.Length));
        }

        private static Dictionary<string, string> ReadLanguage(string language)
        {
            using Stream? stream = typeof(RibbonTexts).Assembly.GetManifestResourceStream(ResourcePrefix + language + ResourceSuffix);
            if (stream == null)
            {
                ShimLog.Error("Missing language file " + language + ResourceSuffix + " in the assembly.");
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            DataContractJsonSerializer serializer = new(typeof(Dictionary<string, string>), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            Dictionary<string, string> all = (Dictionary<string, string>)serializer.ReadObject(stream);

            return all.Where(pair => pair.Key.StartsWith(TextPrefix, StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        }
    }
}
