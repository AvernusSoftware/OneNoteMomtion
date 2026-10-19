using OneNoteMomtion.Core;
using OneNoteMomtion.Core.Localization;
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
                return CommandResult.Failed(editor.Texts.Get(TextKeys.EngineCurrentPageUnknown));
            }

            string? pageXml = session.GetPageXml(pageId, PageInfo.Basic);
            if (string.IsNullOrEmpty(pageXml))
            {
                return CommandResult.Failed(editor.Texts.Get(TextKeys.EngineCurrentPageUnreadable));
            }

            CodeExtractResult extracted = editor.ExtractCodeByBlockId(pageXml!, blockId);
            if (!extracted.Success)
            {
                return CommandResult.Failed(extracted.Message ?? editor.Texts.Get(TextKeys.EngineCodeBlockUnreadable));
            }

            if (!CodeClipboard.TrySet(extracted.Text!, editor.Texts, out string? error))
            {
                return CommandResult.Failed(error!);
            }

            return CommandResult.NothingToDo(editor.Texts.Plural(TextKeys.EngineCodeCopiedLines, extracted.LineCount));
        }
    }
}
