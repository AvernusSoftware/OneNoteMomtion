using System.Collections.Generic;

namespace OneNoteMomtion.Core.Localization
{
    /// <remarks>
    /// Category names follow CLDR, the same ones i18next and <c>Intl.PluralRules</c> use, so a text key
    /// such as <c>engine.reformat.headings_few</c> means the same thing in the engine and in the palette.
    /// A language not listed here gets the English rule.
    /// </remarks>
    public static class PluralRules
    {
        public const string One = "one";
        public const string Few = "few";
        public const string Many = "many";
        public const string Other = "other";

        public static string CategoryOf(string language, int count)
        {
            switch (language)
            {
                case "pl":
                    return PolishCategoryOf(count);
                default:
                    return count == 1 ? One : Other;
            }
        }

        public static IReadOnlyList<string> CategoriesOf(string language)
        {
            switch (language)
            {
                case "pl":
                    return [One, Few, Many];
                default:
                    return [One, Other];
            }
        }

        private static string PolishCategoryOf(int count)
        {
            if (count == 1)
            {
                return One;
            }

            int lastDigit = count % 10;
            int lastTwoDigits = count % 100;

            return lastDigit >= 2 && lastDigit <= 4 && (lastTwoDigits < 12 || lastTwoDigits > 14) ? Few : Many;
        }
    }
}
