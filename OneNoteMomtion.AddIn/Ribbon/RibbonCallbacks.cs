using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Core;
using OneNoteMomtion.AddIn.Ipc;

namespace OneNoteMomtion.AddIn
{
    public partial class AddIn
    {
        private const string LanguageSelectionCommand = "code.format";
        private const string PageReformatCommand = "page.reformat";
        private const string NavigateRevealCommand = "nav.reveal";
        private const string NavigateCollapseAllCommand = "nav.collapseAll";
        private const string RibbonTextsCommand = "texts.ribbon";
        private const string RibbonTextPrefix = "ribbon.";
        private const int DefaultTimeoutMilliseconds = 4000;
        private const int PageReformatTimeoutMilliseconds = 30000;

        private const string LanguageMenuControlId = "languageMenu";

        private IRibbonUI? _ribbon;
        private List<KeyValuePair<string, string>>? _languages;
        private string _selectedLanguageId = string.Empty;
        private Dictionary<string, string>? _ribbonTexts;
        private SettingsWatcher? _settingsWatcher;

        public void OnRibbonLoad(IRibbonUI ribbon)
        {
            _ribbon = ribbon;
            ShimLog.Info("Ribbon loaded.");

            try
            {
                _settingsWatcher = new SettingsWatcher();
                _settingsWatcher.Changed += (_, _) => RefreshRibbonTextsIfChanged();
            }
            catch (Exception ex)
            {
                ShimLog.Warn("Failed to watch styles.json - a new interface language shows on the ribbon after OneNote restarts: " + ex.Message);
            }
        }

        private void ReleaseRibbon()
        {
            _settingsWatcher?.Dispose();
            _settingsWatcher = null;

            if (_ribbon == null)
            {
                return;
            }

            try
            {
                Marshal.FinalReleaseComObject(_ribbon);
            }
            catch (Exception ex)
            {
                ShimLog.Warn("Failed to release the ribbon reference: " + ex.Message);
            }
            finally
            {
                _ribbon = null;
            }
        }

        public void OnCommand(IRibbonControl control)
        {
            try
            {
                string command = control?.Tag ?? string.Empty;
                if (command.Length == 0)
                {
                    ShimLog.Warn("Ribbon button without a tag attribute - nothing to send.");
                    return;
                }

                if (command == NavigateRevealCommand || command == NavigateCollapseAllCommand)
                {
                    RunInBackground(command);
                    return;
                }

                EngineResponse response = Send(command, null);
                if (!response.Success)
                {
                    ShimLog.Warn("Command \"" + command + "\" failed: " + response.Message);
                    return;
                }

                RefreshLanguageListIfStillEmpty();
                RefreshRibbonTextsIfStillEmpty();
            }
            catch (Exception ex)
            {
                ShimLog.Error("Error handling ribbon button.", ex);
            }
        }

        public string OnGetLabel(IRibbonControl control)
        {
            string id = control?.Id ?? string.Empty;
            return RibbonText(id, "label") ?? id;
        }

        public string OnGetScreentip(IRibbonControl control)
        {
            return RibbonText(control?.Id ?? string.Empty, "screentip") ?? string.Empty;
        }

        public string OnGetSupertip(IRibbonControl control)
        {
            return RibbonText(control?.Id ?? string.Empty, "supertip") ?? string.Empty;
        }

        private void RunInBackground(string command)
        {
            Task.Run(() =>
            {
                try
                {
                    EngineResponse response = Send(command, null);
                    if (!response.Success)
                    {
                        ShimLog.Warn("Command \"" + command + "\" failed: " + response.Message);
                    }
                }
                catch (Exception ex)
                {
                    ShimLog.Error("Error handling ribbon button.", ex);
                }
            });
        }

        public string OnLanguageMenuContent(IRibbonControl control)
        {
            try
            {
                List<KeyValuePair<string, string>> languages = Languages();
                StringBuilder xml = new();
                xml.Append("<menu xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\">");

                foreach (KeyValuePair<string, string> language in languages)
                {
                    xml.Append("<toggleButton id=\"lang_").Append(ControlIdOf(language.Key)).Append('"')
                        .Append(" label=\"").Append(XmlAttribute(language.Value)).Append('"')
                        .Append(" tag=\"").Append(XmlAttribute(language.Key)).Append('"')
                        .Append(" getPressed=\"OnLanguagePressed\" onAction=\"OnLanguagePicked\" />");
                }

                xml.Append("</menu>");
                return xml.ToString();
            }
            catch (Exception ex)
            {
                ShimLog.Error("Failed to build the language menu.", ex);
                return "<menu xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\" />";
            }
        }

        public bool OnLanguagePressed(IRibbonControl control)
        {
            string languageId = control?.Tag ?? string.Empty;
            return languageId.Length > 0 && string.Equals(languageId, _selectedLanguageId, StringComparison.OrdinalIgnoreCase);
        }

        public void OnLanguagePicked(IRibbonControl control, bool pressed)
        {
            try
            {
                string languageId = control?.Tag ?? string.Empty;
                if (languageId.Length == 0)
                {
                    ShimLog.Warn("Language menu item without a tag attribute - nothing to send.");
                    return;
                }

                _selectedLanguageId = languageId;

                EngineResponse response = Send(LanguageSelectionCommand, _selectedLanguageId);
                if (!response.Success)
                {
                    ShimLog.Warn("Automatic formatting after selecting the language failed: " + response.Message);
                }

                InvalidateLanguageMenu();
            }
            catch (Exception ex)
            {
                ShimLog.Error("Error while automatically formatting after selecting the language.", ex);
            }
        }

        private List<KeyValuePair<string, string>> Languages()
        {
            if (_languages != null)
            {
                return _languages;
            }

            EngineResponse response = Send("languages.list", null);
            if (!response.Success || response.Fields.Length == 0)
            {
                return [];
            }

            List<KeyValuePair<string, string>> languages = new(response.Fields.Length);

            foreach (string field in response.Fields)
            {
                int separator = field.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                languages.Add(new KeyValuePair<string, string>(field.Substring(0, separator), field.Substring(separator + 1)));
            }

            if (languages.Count == 0)
            {
                return languages;
            }

            if (_selectedLanguageId.Length == 0)
            {
                _selectedLanguageId = languages[0].Key;
            }

            _languages = languages;
            return languages;
        }

        private string? RibbonText(string controlId, string kind)
        {
            try
            {
                _ribbonTexts ??= FetchRibbonTexts();
                return _ribbonTexts.TryGetValue(RibbonTextPrefix + controlId + "." + kind, out string? text) ? text : null;
            }
            catch (Exception ex)
            {
                ShimLog.Error("Failed to read the ribbon texts.", ex);
                return null;
            }
        }

        private Dictionary<string, string> FetchRibbonTexts()
        {
            Dictionary<string, string> texts = new(StringComparer.Ordinal);

            EngineResponse response = Send(RibbonTextsCommand, null);
            if (!response.Success)
            {
                ShimLog.Warn("The engine did not return the ribbon texts: " + response.Message);
                return texts;
            }

            foreach (string field in response.Fields)
            {
                int separator = field.IndexOf('=');
                if (separator > 0)
                {
                    texts[field.Substring(0, separator)] = field.Substring(separator + 1);
                }
            }

            return texts;
        }

        private void RefreshRibbonTextsIfStillEmpty()
        {
            if (_ribbonTexts == null || _ribbonTexts.Count > 0)
            {
                return;
            }

            _ribbonTexts = null;
            InvalidateRibbon();
        }

        private void RefreshRibbonTextsIfChanged()
        {
            try
            {
                Dictionary<string, string> current = FetchRibbonTexts();
                Dictionary<string, string>? previous = _ribbonTexts;

                if (current.Count == 0 || (previous != null && SameTexts(previous, current)))
                {
                    return;
                }

                ShimLog.Info("The interface texts changed - refreshing the ribbon.");
                _ribbonTexts = current;
                _languages = null;
                InvalidateRibbon();
            }
            catch (Exception ex)
            {
                ShimLog.Error("Failed to refresh the ribbon after styles.json changed.", ex);
            }
        }

        private static bool SameTexts(Dictionary<string, string> left, Dictionary<string, string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, string> pair in left)
            {
                if (!right.TryGetValue(pair.Key, out string? value) || value != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void InvalidateRibbon()
        {
            if (_ribbon == null)
            {
                return;
            }

            try
            {
                _ribbon.Invalidate();
            }
            catch (Exception ex)
            {
                ShimLog.Warn("Failed to invalidate the ribbon: " + ex.Message);
            }
        }

        private static string ControlIdOf(string languageId)
        {
            StringBuilder id = new(languageId.Length);

            foreach (char c in languageId)
            {
                id.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return id.ToString();
        }

        private static string XmlAttribute(string value)
        {
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private void RefreshLanguageListIfStillEmpty()
        {
            if (_languages != null)
            {
                return;
            }

            InvalidateLanguageMenu();
        }

        private void InvalidateLanguageMenu()
        {
            if (_ribbon == null)
            {
                return;
            }

            try
            {
                _ribbon.InvalidateControl(LanguageMenuControlId);
            }
            catch (Exception ex)
            {
                ShimLog.Warn("Failed to invalidate the language list: " + ex.Message);
            }
        }

        private EngineResponse Send(string command, string? argument)
        {
            return Send(command, argument, TimeoutFor(command));
        }

        private EngineResponse Send(string command, string? argument, int timeoutMilliseconds)
        {
            if (_engine == null)
            {
                return EngineResponse.Error("The add-in is not connected to OneNote.");
            }

            _engine.EnsureRunning();

            return _engine.Client.Send(command, argument, timeoutMilliseconds);
        }

        private static int TimeoutFor(string command)
        {
            return command == PageReformatCommand ? PageReformatTimeoutMilliseconds : DefaultTimeoutMilliseconds;
        }
    }
}