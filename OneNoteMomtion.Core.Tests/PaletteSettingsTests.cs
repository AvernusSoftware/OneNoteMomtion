using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class PaletteSettingsTests
    {
        [Test]
        public void CreateDefault_KeepsTheShippedOrderInStepsOfTen()
        {
            PaletteSettings settings = PaletteSettings.CreateDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Ordered(settings.Groups), Is.EqualTo(new[] { "styles", "navigation", "code", "letterCase", "tools" }));
                Assert.That(Ordered(settings.Case), Is.EqualTo(new[] { "case.upper", "case.lower", "case.capitalize", "case.sentence", "case.toggle" }));
                Assert.That(settings.Groups.Values.Select(entry => entry.Order), Is.EqualTo(new[] { 10, 20, 30, 40, 50 }));
                Assert.That(settings.Case.Values.Select(entry => entry.Order), Is.EqualTo(new[] { 10, 20, 30, 40, 50 }));
                Assert.That(settings.Groups.Values.All(entry => entry.Enabled), Is.True);
            }
        }

        [Test]
        public void CreateDefault_NumbersEveryGroupOfActionsFromTen()
        {
            PaletteSettings settings = PaletteSettings.CreateDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Actions["style.heading1"].Order, Is.EqualTo(10));
                Assert.That(settings.Actions["style.normal"].Order, Is.EqualTo(40));
                Assert.That(settings.Actions["nav.reveal"].Order, Is.EqualTo(10));
                Assert.That(settings.Actions["nav.dump"].Order, Is.EqualTo(30));
                Assert.That(settings.Actions["code.format"].Order, Is.EqualTo(10));
                Assert.That(settings.Actions["case.menu"].Order, Is.EqualTo(10));
                Assert.That(settings.Actions["page.reformat"].Order, Is.EqualTo(10));
                Assert.That(settings.Actions["spell.disable"].Order, Is.EqualTo(20));
                Assert.That(settings.Actions["spell.enable"].Order, Is.EqualTo(30));
            }
        }

        [Test]
        public void CreateDefault_CoversEveryRegisteredLanguage()
        {
            PaletteSettings settings = PaletteSettings.CreateDefault();
            IReadOnlyList<ISyntaxHighlightProvider> providers = SyntaxHighlighterRegistry.CreateDefault().Selectable();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Languages, Has.Count.EqualTo(providers.Count));
                Assert.That(Ordered(settings.Languages), Is.EqualTo(providers.Select(provider => provider.LanguageId)));
                Assert.That(settings.Languages.Values.Select(entry => entry.Order), Is.EqualTo(Enumerable.Range(1, providers.Count).Select(index => index * 10)));
            }
        }

        [Test]
        public void FillMissingWithDefaults_AddsMissingEntriesWithoutTouchingTheUsersOwn()
        {
            PaletteSettings settings = new()
            {
                Actions = new Dictionary<string, PaletteEntry>
                {
                    { "spell.enable", new PaletteEntry { Order = 5, Enabled = false } },
                },
            };

            settings.FillMissingWithDefaults();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Actions["spell.enable"].Order, Is.EqualTo(5));
                Assert.That(settings.Actions["spell.enable"].Enabled, Is.False);
                Assert.That(settings.Actions["style.heading1"].Order, Is.EqualTo(10));
                Assert.That(settings.Actions["style.heading1"].Enabled, Is.True);
                Assert.That(settings.Groups, Has.Count.EqualTo(5));
                Assert.That(settings.Case, Has.Count.EqualTo(5));
                Assert.That(settings.Languages, Is.Not.Empty);
            }
        }

        [Test]
        public void FillMissingWithDefaults_KeepsKeysTheEngineNoLongerKnows()
        {
            PaletteSettings settings = new()
            {
                Languages = new Dictionary<string, PaletteEntry>
                {
                    { "cobol", new PaletteEntry { Order = 999 } },
                },
            };

            settings.FillMissingWithDefaults();

            Assert.That(settings.Languages.ContainsKey("cobol"), Is.True);
        }

        [Test]
        public void FillMissingWithDefaults_SurvivesNullDictionaries()
        {
            PaletteSettings settings = new()
            {
                Groups = null!,
                Actions = null!,
                Case = null!,
                Languages = null!,
            };

            Assert.DoesNotThrow(() => settings.FillMissingWithDefaults());
            Assert.That(settings.Actions, Has.Count.EqualTo(PaletteSettings.CreateDefault().Actions.Count));
        }

        [Test]
        public void FillMissingWithDefaults_MatchesKeysCaseInsensitively()
        {
            PaletteSettings settings = new()
            {
                Actions = new Dictionary<string, PaletteEntry>
                {
                    { "STYLE.Heading1", new PaletteEntry { Order = 7 } },
                },
            };

            settings.FillMissingWithDefaults();

            Assert.That(settings.Actions["style.heading1"].Order, Is.EqualTo(7));
        }

        [Test]
        public void Serialize_RoundTripsOverridesThroughTheCatalog()
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();
            catalog.Palette.Actions["style.heading1"].Label = "Nagłówek 1";
            catalog.Palette.Actions["style.heading1"].Hint = "Pierwszy poziom";
            catalog.Palette.Actions["style.heading1"].Keywords = ["naglowek", "tytul"];
            catalog.Palette.Groups["tools"].Order = 5;
            catalog.Palette.Languages["groovy"].Enabled = false;

            StyleCatalog reloaded = StyleCatalogStore.Deserialize(StyleCatalogStore.Serialize(catalog));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.Palette.Actions["style.heading1"].Label, Is.EqualTo("Nagłówek 1"));
                Assert.That(reloaded.Palette.Actions["style.heading1"].Hint, Is.EqualTo("Pierwszy poziom"));
                Assert.That(reloaded.Palette.Actions["style.heading1"].Keywords, Is.EqualTo(new[] { "naglowek", "tytul" }));
                Assert.That(reloaded.Palette.Groups["tools"].Order, Is.EqualTo(5));
                Assert.That(reloaded.Palette.Languages["groovy"].Enabled, Is.False);
            }
        }

        [Test]
        public void Serialize_LeavesOutTheOverridesNobodySet()
        {
            string json = StyleCatalogStore.Serialize(StyleCatalog.CreateDefault());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(json, Does.Contain("\"palette\""));
                Assert.That(json, Does.Not.Contain("\"label\": null"));
                Assert.That(json, Does.Not.Contain("\"keywords\""));
            }
        }

        [Test]
        public void Deserialize_FillsInThePaletteMissingFromAHandEditedFile()
        {
            const string Json = "{ \"styles\": {}, \"autoMarkdownTyping\": true }";

            StyleCatalog catalog = StyleCatalogStore.Deserialize(Json);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalog.Palette, Is.Not.Null);
                Assert.That(catalog.Palette.Groups, Has.Count.EqualTo(5));
                Assert.That(catalog.Palette.Actions["nav.dump"].Enabled, Is.True);
            }
        }

        private static IEnumerable<string> Ordered(Dictionary<string, PaletteEntry> entries)
        {
            return entries.OrderBy(pair => pair.Value.Order).Select(pair => pair.Key);
        }
    }
}
