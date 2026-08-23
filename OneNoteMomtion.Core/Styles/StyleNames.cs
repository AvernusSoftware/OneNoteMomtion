namespace OneNoteMomtion.Core.Styles
{
    public static class StyleNames
    {
        public const string Heading1 = "Heading1";
        public const string Heading2 = "Heading2";
        public const string Heading3 = "Heading3";
        public const string Normal = "Normal";

        public static readonly string[] Required =
        [
            Heading1,
            Heading2,
            Heading3,
        ];

        public static string? OfHeadingLevel(int level)
        {
            return level >= 1 && level <= Required.Length ? Required[level - 1] : null;
        }

        public static string? QuickStyleNameOf(string? styleName)
        {
            const string HeadingPrefix = "Heading";

            if (styleName == null || !styleName.StartsWith(HeadingPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string level = styleName[HeadingPrefix.Length..];

            return level.Length == 1 && level[0] >= '1' && level[0] <= '6' ? "h" + level : null;
        }
    }
}
