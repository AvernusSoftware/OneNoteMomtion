using OneNoteMomtion.Core;
using OneNoteMomtion.Server.OneNote;

namespace OneNoteMomtion.Server.Commands
{
    internal static class CodeCopyOperation
    {
        public static CommandResult Run(PageEditor editor, OneNoteSession session, string blockId)
        {
            string? pageId = session.GetCurrentPageId();
            if (pageId is null)
            {
                return CommandResult.Failed("The current page could not be determined.");
            }

            string? pageXml = session.GetPageXml(pageId, PageInfo.Basic);
            if (string.IsNullOrEmpty(pageXml))
            {
                return CommandResult.Failed("The current page could not be read.");
            }

            CodeExtractResult extracted = editor.ExtractCodeByBlockId(pageXml!, blockId);
            if (!extracted.Success)
            {
                return CommandResult.Failed(extracted.Message ?? "The code block could not be read.");
            }

            if (!CodeClipboard.TrySet(extracted.Text!, out string? error))
            {
                return CommandResult.Failed(error!);
            }

            return CommandResult.NothingToDo(DescribeCopied(extracted.LineCount));
        }

        private static string DescribeCopied(int lineCount)
        {
            return "Copied " + lineCount + (lineCount == 1 ? " line." : " lines.");
        }
    }
}
