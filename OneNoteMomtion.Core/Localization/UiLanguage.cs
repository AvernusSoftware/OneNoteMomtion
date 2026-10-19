using System;
using System.Collections.Generic;
using System.Globalization;

namespace OneNoteMomtion.Core.Localization
{
    public static class UiLanguage
    {
        public const string Auto = "auto";

        public const string English = "en";

        public static string Resolve(string? setting, CultureInfo uiCulture, IEnumerable<string> available)
        {
            ArgumentNullException.ThrowIfNull(uiCulture);
            ArgumentNullException.ThrowIfNull(available);

            string wanted = string.IsNullOrWhiteSpace(setting) || string.Equals(setting.Trim(), Auto, StringComparison.OrdinalIgnoreCase) ? uiCulture.TwoLetterISOLanguageName : setting.Trim();

            // "pl-PL" or "pl_PL" mean the same language as "pl" - the files are named by language only.
            wanted = wanted.Split('-', '_')[0];

            foreach (string language in available)
            {
                if (string.Equals(language, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return language;
                }
            }

            return English;
        }
    }
}
