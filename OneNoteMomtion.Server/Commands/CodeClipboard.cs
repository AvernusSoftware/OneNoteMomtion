using System;
using System.Windows.Forms;
using OneNoteMomtion.Core.Localization;

namespace OneNoteMomtion.Server.Commands
{
    internal static class CodeClipboard
    {
        private const int RetryTimes = 10;

        private const int RetryDelayMilliseconds = 100;

        public static bool TrySet(string text, Localizer texts, out string? error)
        {
            error = null;

            if (string.IsNullOrEmpty(text))
            {
                error = texts.Get(TextKeys.EngineNothingToCopy);
                return false;
            }

            try
            {
                Clipboard.SetDataObject(NormalizeLineEndings(text), copy: true, retryTimes: RetryTimes, retryDelay: RetryDelayMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                EngineLog.Error("Failed to put the code on the clipboard.", ex);
                error = texts.Format(TextKeys.EngineClipboardUnavailable, ("error", ex.Message));
                return false;
            }
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }
    }
}
