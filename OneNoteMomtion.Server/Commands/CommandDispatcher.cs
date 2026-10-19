using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using OneNoteMomtion.Core;
using OneNoteMomtion.Core.Hierarchy;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Text;
using OneNoteMomtion.Server.Input;
using OneNoteMomtion.Server.OneNote;
using OneNoteMomtion.Server.Ui;

namespace OneNoteMomtion.Server.Commands
{
    internal sealed class CommandDispatcher
    {
        private const int BreadcrumbToastMilliseconds = 5000;

        private const int ReformatToastMilliseconds = 4000;

        private readonly EngineServices _services;
        private readonly OneNoteConnection _oneNote;
        private readonly IPaletteController _palette;
        private readonly CaretRestorer _caret;
        private readonly NavigationPaneAutomation _navigationPane;

        public CommandDispatcher(EngineServices services, OneNoteConnection oneNote, IPaletteController palette, CaretRestorer caret, NavigationPaneAutomation navigationPane)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _oneNote = oneNote ?? throw new ArgumentNullException(nameof(oneNote));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _caret = caret ?? throw new ArgumentNullException(nameof(caret));
            _navigationPane = navigationPane ?? throw new ArgumentNullException(nameof(navigationPane));
        }

        public CommandResult Execute(string command, string? argument)
        {
            try
            {
                return Dispatch(command, argument);
            }
            catch (Exception ex)
            {
                EngineLog.Error("Command \"" + command + "\" threw an exception.", ex);
                return CommandResult.Failed(ex.Message);
            }
        }

        private CommandResult Dispatch(string command, string? argument)
        {
            switch (command)
            {
                case CommandIds.StyleHeading1:
                    return RunOnPage((editor, xml) => editor.ApplyStyle(xml, StyleNames.Heading1));
                case CommandIds.StyleHeading2:
                    return RunOnPage((editor, xml) => editor.ApplyStyle(xml, StyleNames.Heading2));
                case CommandIds.StyleHeading3:
                    return RunOnPage((editor, xml) => editor.ApplyStyle(xml, StyleNames.Heading3));
                case CommandIds.StyleNormal:
                    return RunOnPage((editor, xml) => editor.ApplyStyle(xml, StyleNames.Normal));

                case CommandIds.MarkdownApply:
                    return RunOnPage((editor, xml) => editor.ApplyMarkdownShortcuts(xml));
                case CommandIds.MarkdownApplyPage:
                    return RunOnPage((editor, xml) => editor.ApplyMarkdownShortcuts(xml, selectionOnly: false));
                case CommandIds.MarkdownTyping:
                    return RunOnPage((editor, xml) => editor.ApplyMarkdownAtCaret(xml));
                case CommandIds.StyleNormalAfterHeading:
                    return RunOnPage((editor, xml) => editor.ApplyNormalAfterHeading(xml, argument));

                case CommandIds.CaseUpper:
                    return RunOnPage((editor, xml) => editor.ApplyTextCase(xml, TextCaseMode.Upper));
                case CommandIds.CaseLower:
                    return RunOnPage((editor, xml) => editor.ApplyTextCase(xml, TextCaseMode.Lower));
                case CommandIds.CaseCapitalize:
                    return RunOnPage((editor, xml) => editor.ApplyTextCase(xml, TextCaseMode.Capitalize));
                case CommandIds.CaseSentence:
                    return RunOnPage((editor, xml) => editor.ApplyTextCase(xml, TextCaseMode.Sentence));
                case CommandIds.CaseToggle:
                    return RunOnPage((editor, xml) => editor.ApplyTextCase(xml, TextCaseMode.Toggle));

                case CommandIds.SpellDisable:
                    return RunOnPage((editor, xml) => editor.SetSpellCheck(xml, enabled: false));
                case CommandIds.SpellEnable:
                    return RunOnPage((editor, xml) => editor.SetSpellCheck(xml, enabled: true));

                case CommandIds.PageReformat:
                    return ReformatPage();

                case CommandIds.CodeFormat:
                    return FormatCode(argument);

                case CommandIds.CodeCopyById:
                    return CopyCode(argument);

                case CommandIds.LanguagesList:
                    return CommandResult.Payload(LanguageList());

                case CommandIds.NavigateReveal:
                    return RevealCurrentPage();

                case CommandIds.NavigateCollapseAll:
                    return CollapseNavigationTree();

                case CommandIds.NavigateDump:
                    return DumpNavigationTree();

                case CommandIds.SettingsGet:
                    return CommandResult.Payload(StyleCatalogStore.Serialize(_services.Catalog));

                case CommandIds.SettingsSave:
                    return SaveSettings(argument);

                case CommandIds.SettingsOpenFolder:
                    return OpenSettingsFolder();

                case CommandIds.SettingsOpen:
                    _palette.Show("settings");
                    return CommandResult.NothingToDo();

                case CommandIds.PaletteConfig:
                    return CommandResult.Payload(_services.Catalog.Palette);

                case CommandIds.TextsPalette:
                    return CommandResult.Payload(new TextsPayload(_services.Texts.Language, _services.Texts.WithPrefix(TextKeys.PalettePrefix)));

                case CommandIds.PaletteShow:
                    _palette.Show("actions");
                    return CommandResult.NothingToDo();

                case CommandIds.PaletteClose:
                    _palette.Hide();
                    return CommandResult.NothingToDo();

                case CommandIds.EnginePing:
                    return CommandResult.NothingToDo(_services.Texts.Get(TextKeys.EngineRunning));

                case CommandIds.EnginePid:
                    return CommandResult.Payload(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

                default:
                    return CommandResult.Failed(_services.Texts.Format(TextKeys.EngineUnknownCommand, ("command", command)));
            }
        }

        private CommandResult ReformatPage()
        {
            CommandResult result = RunOnPage((editor, xml) => editor.ReformatPage(xml, _services.ReformatOptions));

            if (result.Message != null)
            {
                ToastWindow.Show(result.Message, ReformatToastMilliseconds);
            }

            return result;
        }

        private CommandResult FormatCode(string? languageId)
        {
            if (string.IsNullOrWhiteSpace(languageId))
            {
                return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineNoLanguageChosen));
            }

            return RunOnPage((editor, xml) => editor.FormatCode(xml, languageId!));
        }

        private CommandResult CopyCode(string? blockId)
        {
            if (string.IsNullOrWhiteSpace(blockId))
            {
                return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineNoCodeBlockId));
            }

            CommandResult result = WithSession(session => CodeCopyOperation.Run(_services.Editor, session, blockId!));

            ToastWindow.Show(result.Message ?? _services.Texts.Get(TextKeys.EngineCodeCopied));

            return result;
        }

        private CommandResult RevealCurrentPage()
        {
            return WithSession(session =>
            {
                string? pageId = session.GetCurrentPageId();
                if (pageId is null)
                {
                    return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineCurrentPageUnreadable));
                }

                string? hierarchyXml = session.GetHierarchyXml(null, HierarchyScope.Pages);
                if (string.IsNullOrEmpty(hierarchyXml) || !HierarchyDocument.TryParse(hierarchyXml!, out HierarchyDocument? hierarchy))
                {
                    return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineNotebookTreeUnreadable));
                }

                if (!hierarchy.TryFindPagePath(pageId, out HierarchyPath? path))
                {
                    return CommandResult.Failed(_services.Texts.Get(TextKeys.EnginePageNotInOpenNotebook));
                }

                session.NavigateToPage(pageId);

                if (_navigationPane.TryReveal(path))
                {
                    return CommandResult.Applied();
                }

                EngineLog.Warn("nav.reveal: could not expand the navigation pane for \"" + path.ToBreadcrumb() + "\".");
                return CommandResult.NothingToDo();
            });
        }

        private CommandResult CollapseNavigationTree()
        {
            if (!_navigationPane.TryCollapseAll(out int collapsed))
            {
                EngineLog.Warn("nav.collapseAll: the navigation pane could not be collapsed.");
                return CommandResult.NothingToDo();
            }

            return collapsed > 0 ? CommandResult.Applied() : CommandResult.NothingToDo();
        }

        private CommandResult DumpNavigationTree()
        {
            if (!UiaTreeDump.TryDump(_navigationPane.FindOneNoteWindow(), _services.Texts, out string message))
            {
                return CommandResult.Failed(message);
            }

            ToastWindow.Show(message, BreadcrumbToastMilliseconds);
            return CommandResult.NothingToDo(message);
        }

        private IReadOnlyList<string> LanguageList()
        {
            List<string> entries = [];

            foreach (ISyntaxHighlightProvider provider in _services.Registry.Selectable())
            {
                entries.Add(provider.LanguageId + "=" + _services.Texts.LanguageName(provider));
            }

            return entries;
        }

        private CommandResult SaveSettings(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineNothingToSave));
            }

            try
            {
                _services.SaveCatalog(StyleCatalogStore.Deserialize(json!));
                return CommandResult.Applied(_services.Texts.Get(TextKeys.EngineSettingsSaved));
            }
            catch (Exception ex)
            {
                return CommandResult.Failed(_services.Texts.Format(TextKeys.EngineSettingsUnreadable, ("error", ex.Message)));
            }
        }

        private CommandResult OpenSettingsFolder()
        {
            try
            {
                string? folder = Path.GetDirectoryName(_services.SettingsPath);
                if (string.IsNullOrEmpty(folder))
                {
                    return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineSettingsFolderUnknown));
                }

                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
                return CommandResult.NothingToDo();
            }
            catch (Exception ex)
            {
                return CommandResult.Failed(ex.Message);
            }
        }

        private CommandResult RunOnPage(Func<PageEditor, string, PageEditResult> operation)
        {
            return WithSession(session =>
            {
                if (!session.TryGetCurrentPage(out string pageId, out string pageXml))
                {
                    return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineCurrentPageUnreadable));
                }

                PageEditResult result = operation(_services.Editor, pageXml);
                if (!result.Changed)
                {
                    return CommandResult.NothingToDo(result.Message, result.CaretObjectId);
                }

                if (!session.UpdatePageXml(result.Xml!))
                {
                    return CommandResult.Failed(_services.Texts.Get(TextKeys.EnginePageNotSaved));
                }

                if (result.FocusObjectId != null)
                {
                    _caret.PlaceAtStartOf(session, pageId, result.FocusObjectId);
                }
                else if (result.NudgeCaret)
                {
                    _caret.MakeCaretVisible();
                }

                return CommandResult.Applied(result.Message);
            });
        }

        private CommandResult WithSession(Func<OneNoteSession, CommandResult> operation)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                OneNoteSession? session = _oneNote.TryGetSession();
                if (session is null)
                {
                    return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineNoConnection));
                }

                try
                {
                    return operation(session);
                }
                catch (Exception ex) when (OneNoteConnection.IsDisconnected(ex) && attempt == 0)
                {
                    EngineLog.Warn("The connection to OneNote was broken - reconnecting.");
                    _oneNote.Reset();
                }
            }

            return CommandResult.Failed(_services.Texts.Get(TextKeys.EngineReconnectFailed));
        }
    }
}
