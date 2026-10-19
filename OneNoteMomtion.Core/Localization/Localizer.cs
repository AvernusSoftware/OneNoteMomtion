using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Highlighting.Providers;

namespace OneNoteMomtion.Core.Localization
{
    public sealed class Localizer
    {
        private readonly IReadOnlyDictionary<string, string> _texts;
        private readonly IReadOnlyDictionary<string, string> _fallback;

        public Localizer(string language)
        {
            ArgumentNullException.ThrowIfNull(language);

            Language = TextCatalog.Available.FirstOrDefault(available => string.Equals(available, language, StringComparison.OrdinalIgnoreCase)) ?? UiLanguage.English;
            _texts = TextCatalog.TextsOf(Language);
            _fallback = TextCatalog.TextsOf(UiLanguage.English);
        }

        public static Localizer English { get; } = new(UiLanguage.English);

        public string Language { get; }

        public static Localizer For(string? setting)
        {
            return new Localizer(UiLanguage.Resolve(setting, CultureInfo.CurrentUICulture, TextCatalog.Available));
        }

        public string Get(string key)
        {
            if (_texts.TryGetValue(key, out string? text) || _fallback.TryGetValue(key, out text))
            {
                return text;
            }

            return key;
        }

        public string Format(string key, params (string Name, string Value)[] values)
        {
            StringBuilder text = new(Get(key));

            foreach ((string name, string value) in values)
            {
                text.Replace("{{" + name + "}}", value);
            }

            return text.ToString();
        }

        public string Plural(string key, int count)
        {
            return Format(key + "_" + PluralRules.CategoryOf(Language, count), ("count", count.ToString(CultureInfo.InvariantCulture)));
        }

        public string LanguageName(ISyntaxHighlightProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            return provider.LanguageId == PlainTextHighlightProvider.Id ? Get(TextKeys.PagePlainText) : provider.DisplayName;
        }

        public IReadOnlyDictionary<string, string> WithPrefix(string prefix)
        {
            Dictionary<string, string> result = new(StringComparer.Ordinal);

            foreach (IReadOnlyDictionary<string, string> source in new[] { _fallback, _texts })
            {
                foreach (KeyValuePair<string, string> pair in source)
                {
                    if (pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        result[pair.Key] = pair.Value;
                    }
                }
            }

            return result;
        }
    }
}
