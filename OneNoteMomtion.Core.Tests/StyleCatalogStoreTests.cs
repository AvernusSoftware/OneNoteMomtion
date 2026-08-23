using System;
using System.IO;
using NUnit.Framework;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class StyleCatalogStoreTests
    {
        private string _directory = string.Empty;
        private string _path = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "OneNoteMomtion.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "styles.json");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(_directory, true);
            }
            catch (IOException)
            {
            }
        }

        [Test]
        public void SaveAndLoad_RoundTripsUserChanges()
        {
            StyleCatalog original = StyleCatalog.CreateDefault();
            original.Styles[StyleNames.Heading1].FontSize = 33.5;
            original.Styles[StyleNames.Heading1].Font = "Segoe UI";
            original.CodeBlock.ShadingColor = "#123456";
            original.AutoMarkdownTyping = false;
            original.AutoNormalAfterHeading = false;

            StyleCatalogStore.Save(original, _path);
            StyleCatalog reloaded = StyleCatalogStore.Load(_path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.Styles[StyleNames.Heading1].FontSize, Is.EqualTo(33.5));
                Assert.That(reloaded.Styles[StyleNames.Heading1].Font, Is.EqualTo("Segoe UI"));
                Assert.That(reloaded.CodeBlock.ShadingColor, Is.EqualTo("#123456"));
                Assert.That(reloaded.AutoMarkdownTyping, Is.False);
                Assert.That(reloaded.AutoNormalAfterHeading, Is.False);
            }
        }

        [Test]
        public void Load_CreatesFileWithDefaultsOnFirstRun()
        {
            Assert.That(File.Exists(_path), Is.False);

            StyleCatalogLoadResult result = StyleCatalogStore.LoadDetailed(_path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(_path), Is.True);
                Assert.That(result.UsedDefaults, Is.True);
                Assert.That(result.ErrorMessage, Is.Null);
            }

            foreach (string required in StyleNames.Required)
            {
                Assert.That(result.Catalog.Get(required), Is.Not.Null, "Required style " + required + " is missing.");
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.ReadAllText(_path), Does.Contain("\"palette\""));
                Assert.That(result.Catalog.Palette.Actions, Is.Not.Empty);
                Assert.That(result.Catalog.Palette.Groups, Is.Not.Empty);
            }
        }

        [Test]
        public void Load_FallsBackToDefaultsWithoutOverwritingACorruptFile()
        {
            const string Broken = "{ this is not json";
            File.WriteAllText(_path, Broken);

            StyleCatalogLoadResult result = StyleCatalogStore.LoadDetailed(_path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.UsedDefaults, Is.True);
                Assert.That(result.ErrorMessage, Is.Not.Null);
                Assert.That(result.Catalog.Get(StyleNames.Heading1), Is.Not.Null);
                Assert.That(File.ReadAllText(_path), Is.EqualTo(Broken));
            }
        }

        [Test]
        public void Load_FillsInStylesMissingFromAHandEditedFile()
        {
            File.WriteAllText(_path, "{ \"styles\": { \"Heading1\": { \"fontSize\": 40.0 } } }");

            StyleCatalog catalog = StyleCatalogStore.Load(_path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalog.Get(StyleNames.Heading1)!.FontSize, Is.EqualTo(40.0));
                Assert.That(catalog.Get(StyleNames.Heading2), Is.Not.Null);
                Assert.That(catalog.Get(StyleNames.Heading3), Is.Not.Null);
                Assert.That(catalog.CodeBlock, Is.Not.Null);
                Assert.That(catalog.HighlightTheme, Is.Not.Empty);
            }
        }

        [Test]
        public void Load_AcceptsCommentsAndTrailingCommas()
        {
            File.WriteAllText(_path, "{\n  // user settings\n  \"typingApplyDelayMs\": 300,\n}");

            Assert.That(StyleCatalogStore.Load(_path).TypingApplyDelayMs, Is.EqualTo(300));
        }

        [Test]
        public void Load_ClampsTheTypingDelayToASafeRange()
        {
            File.WriteAllText(_path, "{ \"typingApplyDelayMs\": 5 }");
            Assert.That(StyleCatalogStore.Load(_path).TypingApplyDelayMs, Is.EqualTo(50));

            File.WriteAllText(_path, "{ \"typingApplyDelayMs\": 90000 }");
            Assert.That(StyleCatalogStore.Load(_path).TypingApplyDelayMs, Is.EqualTo(1000));
        }

        [Test]
        public void StyleLookup_IgnoresLetterCaseAfterDeserialization()
        {
            StyleCatalogStore.Save(StyleCatalog.CreateDefault(), _path);

            StyleCatalog catalog = StyleCatalogStore.Load(_path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalog.Get("heading1"), Is.Not.Null);
                Assert.That(catalog.Get("HEADING1"), Is.Not.Null);
            }
        }

        [Test]
        public void ToCss_EmitsInvariantDecimalSeparator()
        {
            StyleDefinition style = new() { Font = "Calibri", FontSize = 20.5, FontColor = "#112233", Bold = true };

            Assert.That(style.ToCss(), Is.EqualTo("font-family:Calibri;font-size:20.5pt;color:#112233;font-weight:bold"));
        }

        [Test]
        public void ToCss_IsEmptyForAStyleThatSetsNoCharacterFormatting()
        {
            Assert.That(new StyleDefinition { SpaceBefore = 5.0 }.ToCss(), Is.Empty);
        }

        [Test]
        public void SaveAndLoad_WritesNoThemeVariantKeys()
        {
            StyleCatalogStore.Save(StyleCatalog.CreateDefault(), _path);
            string json = File.ReadAllText(_path);

            Assert.That(json, Does.Not.Contain("\"theme\":"));
            Assert.That(json, Does.Not.Contain("highlightThemeDark"));
            Assert.That(json, Does.Not.Contain("ColorDark"));
            Assert.That(json, Does.Not.Contain("ColorLight"));
        }

        [Test]
        public void Load_IgnoresTheThemeSettingLeftOverFromAnOlderSettingsFile()
        {
            File.WriteAllText(_path, "{ \"theme\": \"Dark\", \"highlightThemeDark\": { \"Keyword\": \"#79C0FF\" }, \"styles\": { \"Heading1\": { \"fontSize\": 40.0 } } }");

            StyleCatalog catalog = StyleCatalogStore.Load(_path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalog.Get(StyleNames.Heading1)!.FontSize, Is.EqualTo(40.0));
                Assert.That(catalog.HighlightTheme["Keyword"], Is.EqualTo("#0033B3"));
            }
        }

        [Test]
        public void Load_MovesTheLegacyLightColorIntoTheSingleColor()
        {
            File.WriteAllText(_path, "{ \"styles\": { \"Heading1\": { \"fontColorLight\": \"#2E75B5\", \"fontColorDark\": \"#69A4D8\", \"highlightColorLight\": \"#FFF3BF\" } } }");

            StyleDefinition heading = StyleCatalogStore.Load(_path).Get(StyleNames.Heading1)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(heading.FontColor, Is.EqualTo("#2E75B5"));
                Assert.That(heading.HighlightColor, Is.EqualTo("#FFF3BF"));
            }

            StyleCatalogStore.Save(StyleCatalogStore.Load(_path), _path);
            Assert.That(File.ReadAllText(_path), Does.Not.Contain("fontColorLight"));
        }

        [Test]
        public void Load_KeepsAnExplicitColorInsteadOfTheLegacyOne()
        {
            File.WriteAllText(_path, "{ \"styles\": { \"Heading1\": { \"fontColor\": \"#C7254E\", \"fontColorLight\": \"#2E75B5\" } } }");

            Assert.That(StyleCatalogStore.Load(_path).Get(StyleNames.Heading1)!.FontColor, Is.EqualTo("#C7254E"));
        }

        [Test]
        public void Load_IgnoresTheInlineFormattingSwitchLeftOverFromAnOlderSettingsFile()
        {
            File.WriteAllText(_path, "{ \"styles\": { \"Heading1\": { \"resetInlineFormatting\": false, \"fontSize\": 40.0 } } }");

            StyleCatalog catalog = StyleCatalogStore.Load(_path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalog.Get(StyleNames.Heading1)!.FontSize, Is.EqualTo(40.0));
                Assert.That(catalog.Get(StyleNames.Heading2), Is.Not.Null);
            }

            StyleCatalogStore.Save(catalog, _path);
            Assert.That(File.ReadAllText(_path), Does.Not.Contain("resetInlineFormatting"));
        }

        [Test]
        public void Load_DropsTheNormalTextStyleLeftOverFromAnOlderSettingsFile()
        {
            File.WriteAllText(_path, "{ \"styles\": { \"Normal\": { \"fontSize\": 22.0, \"fontColor\": \"#FF0000\" }, \"Heading1\": { \"fontSize\": 40.0 } } }");

            StyleCatalog catalog = StyleCatalogStore.Load(_path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalog.Get(StyleNames.Normal), Is.Null);
                Assert.That(catalog.Get(StyleNames.Heading1)!.FontSize, Is.EqualTo(40.0));
            }

            StyleCatalogStore.Save(catalog, _path);
            Assert.That(File.ReadAllText(_path), Does.Not.Contain("\"Normal\":"));
        }

        [Test]
        public void CreateDefault_GivesHeadingsAColourAndNoEntryForNormalText()
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalog.Get(StyleNames.Heading1)!.FontColor, Is.EqualTo("#2E75B5"));
                Assert.That(catalog.Get(StyleNames.Normal), Is.Null);
            }
        }

        [Test]
        public void Serialize_WritesTheReformatSections()
        {
            string json = StyleCatalogStore.Serialize(StyleCatalog.CreateDefault());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(json, Does.Contain("\"bodyText\""));
                Assert.That(json, Does.Contain("\"textCleanup\""));
                Assert.That(json, Does.Contain("\"codeDetection\""));
            }
        }

        [Test]
        public void FillMissingWithDefaults_AddsTheReformatSectionsToAnOlderSettingsFile()
        {
            StyleCatalog catalog = StyleCatalogStore.Deserialize("{ \"version\": 1 }");
            catalog.FillMissingWithDefaults();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalog.BodyText, Is.Not.Null);
                Assert.That(catalog.TextCleanup.CollapseSpaces, Is.True);
                Assert.That(catalog.CodeDetection.ConvertForeignBlocks, Is.True);
            }
        }

        [Test]
        public void FillMissingWithDefaults_LeavesTheBodyFontUnsetSoOneNotesOwnDefaultCanWin()
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();
            catalog.FillMissingWithDefaults();

            Assert.That(catalog.BodyText.HasAnyValue, Is.False);
        }
    }
}
