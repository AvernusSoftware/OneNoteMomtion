using System;
using System.IO;
using OneNoteMomtion.Core;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Localization;
using OneNoteMomtion.Core.Reformat;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Server.OneNote;

namespace OneNoteMomtion.Server.Commands
{
    internal sealed class EngineServices
    {
        private readonly object _gate = new();
        private readonly string _settingsPath;

        private StyleCatalog _catalog;
        private PageEditor _editor;
        private DateTime _lastWriteUtc;
        private ReformatOptions? _reformatOptions;

        public EngineServices(string? settingsPath = null)
        {
            _settingsPath = string.IsNullOrEmpty(settingsPath) ? StyleCatalogStore.DefaultPath : settingsPath!;
            Registry = SyntaxHighlighterRegistry.CreateDefault();

            StyleCatalogLoadResult result = StyleCatalogStore.LoadDetailed(_settingsPath);
            if (result.ErrorMessage != null)
            {
                EngineLog.Warn(result.ErrorMessage);
            }

            _catalog = result.Catalog;
            _editor = new PageEditor(_catalog, Registry);
            _lastWriteUtc = ReadSettingsLastWriteUtc();
        }

        public SyntaxHighlighterRegistry Registry { get; }

        public string SettingsPath => _settingsPath;

        public StyleCatalog Catalog
        {
            get
            {
                ReloadIfFileChanged();
                lock (_gate)
                {
                    return _catalog;
                }
            }
        }

        public PageEditor Editor
        {
            get
            {
                ReloadIfFileChanged();
                lock (_gate)
                {
                    return _editor;
                }
            }
        }

        public Localizer Texts => Editor.Texts;

        public ReformatOptions ReformatOptions
        {
            get
            {
                StyleCatalog catalog = Catalog;

                lock (_gate)
                {
                    _reformatOptions ??= ReformatOptions.FromCatalog(catalog, OneNoteDefaultFont.Read());
                    return _reformatOptions;
                }
            }
        }

        public void SaveCatalog(StyleCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            catalog.FillMissingWithDefaults();
            StyleCatalogStore.Save(catalog, _settingsPath);

            lock (_gate)
            {
                _catalog = catalog;
                _editor = new PageEditor(catalog, Registry);
                _reformatOptions = null;
                _lastWriteUtc = ReadSettingsLastWriteUtc();
            }
        }

        private void ReloadIfFileChanged()
        {
            DateTime lastWriteUtc = ReadSettingsLastWriteUtc();

            lock (_gate)
            {
                if (lastWriteUtc == _lastWriteUtc)
                {
                    return;
                }

                _lastWriteUtc = lastWriteUtc;
            }

            StyleCatalogLoadResult result = StyleCatalogStore.LoadDetailed(_settingsPath);
            if (result.ErrorMessage != null)
            {
                EngineLog.Warn(result.ErrorMessage);
            }

            lock (_gate)
            {
                _catalog = result.Catalog;
                _editor = new PageEditor(result.Catalog, Registry);
                _reformatOptions = null;
            }

            EngineLog.Info("Reloaded configuration from " + _settingsPath);
        }

        private DateTime ReadSettingsLastWriteUtc()
        {
            try
            {
                FileInfo file = new(_settingsPath);
                return file.Exists ? file.LastWriteTimeUtc : DateTime.MinValue;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }
    }
}
