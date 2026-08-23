using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Markdown
{
    public static class MarkdownShortcut
    {
        public const int MaxHeadingLevel = 3;

        public static bool TryParse(string plainText, out MarkdownBlockKind kind, out int prefixLength)
        {
            kind = MarkdownBlockKind.None;
            prefixLength = 0;

            if (string.IsNullOrEmpty(plainText))
            {
                return false;
            }

            int hashes = 0;
            while (hashes < plainText.Length && plainText[hashes] == '#')
            {
                hashes++;
            }

            if (hashes == 0 || hashes > MaxHeadingLevel)
            {
                return false;
            }

            if (hashes >= plainText.Length || plainText[hashes] != ' ')
            {
                return false;
            }

            kind = FromLevel(hashes);
            prefixLength = hashes + 1;
            return true;
        }

        public static MarkdownBlockKind FromLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return MarkdownBlockKind.Heading1;
                case 2:
                    return MarkdownBlockKind.Heading2;
                case 3:
                    return MarkdownBlockKind.Heading3;
                default:
                    return MarkdownBlockKind.None;
            }
        }

        public static string? StyleNameFor(MarkdownBlockKind kind)
        {
            switch (kind)
            {
                case MarkdownBlockKind.Heading1:
                    return StyleNames.Heading1;
                case MarkdownBlockKind.Heading2:
                    return StyleNames.Heading2;
                case MarkdownBlockKind.Heading3:
                    return StyleNames.Heading3;
                default:
                    return null;
            }
        }
    }
}
