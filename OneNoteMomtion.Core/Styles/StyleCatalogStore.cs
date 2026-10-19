using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneNoteMomtion.Core.Styles
{
    public static class StyleCatalogStore
    {
        public const string FolderName = "OneNoteMomtion";

        public const string FileName = "styles.json";

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static string DefaultPath
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(Path.Combine(appData, FolderName), FileName);
            }
        }

        public static StyleCatalog Load(string? path = null)
        {
            return LoadDetailed(path).Catalog;
        }

        public static StyleCatalogLoadResult LoadDetailed(string? path = null)
        {
            string target = string.IsNullOrEmpty(path) ? DefaultPath : path!;

            if (!File.Exists(target))
            {
                StyleCatalog defaults = StyleCatalog.CreateDefault();
                string? writeError = null;

                try
                {
                    Save(defaults, target);
                }
                catch (Exception ex)
                {
                    writeError = "Failed to create the configuration file: " + ex.Message;
                }

                return new StyleCatalogLoadResult(defaults, target, true, writeError);
            }

            try
            {
                string json = File.ReadAllText(target, Encoding.UTF8);
                StyleCatalog catalog = JsonSerializer.Deserialize<StyleCatalog>(json, ReadOptions) ?? StyleCatalog.CreateDefault();
                bool outdated = catalog.Version < StyleCatalog.CurrentVersion;
                catalog.FillMissingWithDefaults();

                // Writing a migrated file back at once keeps the migration from running again on a value the
                // user sets by hand afterwards.
                string? writeError = null;
                if (outdated)
                {
                    try
                    {
                        Save(catalog, target);
                    }
                    catch (Exception ex)
                    {
                        writeError = "Failed to save the migrated configuration file: " + ex.Message;
                    }
                }

                return new StyleCatalogLoadResult(catalog, target, false, writeError);
            }
            catch (Exception ex)
            {
                return new StyleCatalogLoadResult(
                    StyleCatalog.CreateDefault(),
                    target,
                    true,
                    "Failed to read " + target + ": " + ex.Message);
            }
        }

        public static void Save(StyleCatalog catalog, string? path = null)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            string target = string.IsNullOrEmpty(path) ? DefaultPath : path!;
            string? directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = Serialize(catalog);
            string temporary = target + ".tmp";

            File.WriteAllText(temporary, json, new UTF8Encoding(false));

            if (File.Exists(target))
            {
                File.Delete(target);
            }

            File.Move(temporary, target);
        }

        public static string Serialize(StyleCatalog catalog)
        {
            return JsonSerializer.Serialize(catalog, WriteOptions);
        }

        public static StyleCatalog Deserialize(string json)
        {
            StyleCatalog catalog = JsonSerializer.Deserialize<StyleCatalog>(json, ReadOptions) ?? StyleCatalog.CreateDefault();
            catalog.FillMissingWithDefaults();
            return catalog;
        }
    }
}
