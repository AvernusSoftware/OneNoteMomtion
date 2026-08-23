namespace OneNoteMomtion.Core.Styles
{
    public sealed class StyleCatalogLoadResult
    {
        public StyleCatalogLoadResult(StyleCatalog catalog, string path, bool usedDefaults, string? errorMessage)
        {
            Catalog = catalog;
            Path = path;
            UsedDefaults = usedDefaults;
            ErrorMessage = errorMessage;
        }

        public StyleCatalog Catalog { get; }

        public string Path { get; }

        public bool UsedDefaults { get; }

        public string? ErrorMessage { get; }
    }
}
