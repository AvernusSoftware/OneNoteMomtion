using System;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Highlighting
{
    public static class CodeBlockLink
    {
        public const string Scheme = "momtion";

        public const string CopyAction = "copy";

        private const string CopyPrefix = Scheme + ":" + CopyAction + "?id=";

        private const string LanguageParameter = "&lang=";

        public static string CopyHref(string blockId)
        {
            return CopyHref(blockId, null);
        }

        public static string CopyHref(string blockId, string? languageId)
        {
            if (string.IsNullOrEmpty(blockId))
            {
                throw new ArgumentException("The block identifier must not be empty.", nameof(blockId));
            }

            if (string.IsNullOrWhiteSpace(languageId) || !IsPlainIdentifier(languageId!))
            {
                return CopyPrefix + blockId;
            }

            return CopyPrefix + blockId + LanguageParameter + languageId;
        }

        public static bool TryGetCopyBlockId(string? uri, out string blockId)
        {
            return TryParse(uri, out blockId, out _);
        }

        public static bool TryGetCopyLanguageId(string? uri, out string languageId)
        {
            return TryParse(uri, out _, out languageId) && languageId.Length > 0;
        }

        private static bool TryParse(string? uri, out string blockId, out string languageId)
        {
            blockId = string.Empty;
            languageId = string.Empty;

            if (string.IsNullOrEmpty(uri))
            {
                return false;
            }

            string trimmed = uri!.Trim().TrimEnd('/');
            if (!trimmed.StartsWith(CopyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string rest = trimmed[CopyPrefix.Length..];
            string candidateId = rest;
            string candidateLanguage = string.Empty;

            int separator = rest.IndexOf(LanguageParameter, StringComparison.OrdinalIgnoreCase);
            if (separator >= 0)
            {
                candidateId = rest[..separator];
                candidateLanguage = rest[(separator + LanguageParameter.Length)..];
            }

            if (candidateId.Length == 0 || !IsPlainIdentifier(candidateId))
            {
                return false;
            }

            blockId = candidateId;
            languageId = IsPlainIdentifier(candidateLanguage) ? candidateLanguage : string.Empty;
            return true;
        }

        public static bool RunLinksToBlock(string? runHtml, string blockId)
        {
            if (string.IsNullOrEmpty(runHtml) || string.IsNullOrEmpty(blockId))
            {
                return false;
            }

            string needle = CopyPrefix + blockId;
            int at = runHtml!.IndexOf(needle, StringComparison.OrdinalIgnoreCase);

            if (at < 0)
            {
                return false;
            }

            int after = at + needle.Length;
            return after >= runHtml.Length || !IsIdentifierCharacter(runHtml[after]);
        }

        public static bool RunContainsCopyLink(string? runHtml)
        {
            if (string.IsNullOrEmpty(runHtml))
            {
                return false;
            }

            return runHtml.Contains(CopyPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetRunLanguageId(string? runHtml, out string languageId)
        {
            return TryGetRunLink(runHtml, out _, out languageId) && languageId.Length > 0;
        }

        public static bool TryGetRunLink(string? runHtml, out string blockId, out string languageId)
        {
            blockId = string.Empty;
            languageId = string.Empty;

            if (string.IsNullOrEmpty(runHtml))
            {
                return false;
            }

            int at = runHtml!.IndexOf(CopyPrefix, StringComparison.OrdinalIgnoreCase);
            if (at < 0)
            {
                return false;
            }

            int end = at;
            while (end < runHtml.Length && !IsHrefTerminator(runHtml[end]))
            {
                end++;
            }

            return TryParse(HtmlRunRewriter.Decode(runHtml[at..end]), out blockId, out languageId);
        }

        private static bool IsHrefTerminator(char c)
        {
            return c == '"' || c == '\'' || c == '>' || c == '<' || char.IsWhiteSpace(c);
        }

        private static bool IsIdentifierCharacter(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '-';
        }

        private static bool IsPlainIdentifier(string value)
        {
            foreach (char c in value)
            {
                if (!IsIdentifierCharacter(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
