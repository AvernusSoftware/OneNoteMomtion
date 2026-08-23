namespace OneNoteMomtion.Server.Commands
{
    internal static class CommandIds
    {
        public const string StyleHeading1 = "style.heading1";
        public const string StyleHeading2 = "style.heading2";
        public const string StyleHeading3 = "style.heading3";
        public const string StyleNormal = "style.normal";
        public const string StyleNormalAfterHeading = "style.normalAfterHeading";

        public const string MarkdownApply = "markdown.apply";
        public const string MarkdownApplyPage = "markdown.applyPage";
        public const string MarkdownTyping = "markdown.typing";

        public const string CaseUpper = "case.upper";
        public const string CaseLower = "case.lower";
        public const string CaseCapitalize = "case.capitalize";
        public const string CaseSentence = "case.sentence";
        public const string CaseToggle = "case.toggle";

        public const string SpellDisable = "spell.disable";
        public const string SpellEnable = "spell.enable";

        public const string PageReformat = "page.reformat";

        public const string CodeFormat = "code.format";
        public const string CodeCopyById = "code.copyById";

        public const string LanguagesList = "languages.list";

        public const string NavigateReveal = "nav.reveal";
        public const string NavigateCollapseAll = "nav.collapseAll";
        public const string NavigateDump = "nav.dump";

        public const string SettingsGet = "settings.get";
        public const string SettingsSave = "settings.save";
        public const string SettingsOpen = "settings.open";
        public const string SettingsOpenFolder = "settings.openFolder";

        public const string PaletteConfig = "palette.config";
        public const string PaletteShow = "palette.show";
        public const string PaletteClose = "palette.close";

        public const string EnginePing = "engine.ping";
        public const string EnginePid = "engine.pid";
        public const string EngineShutdown = "engine.shutdown";
    }
}