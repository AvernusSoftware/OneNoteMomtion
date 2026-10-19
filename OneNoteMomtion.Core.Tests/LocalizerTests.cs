using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Highlighting.Providers;
using OneNoteMomtion.Core.Localization;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class LocalizerTests
    {
        private static readonly string[] Available = ["en", "pl"];

        [TestCase(null, "pl-PL", ExpectedResult = "pl")]
        [TestCase("auto", "pl-PL", ExpectedResult = "pl")]
        [TestCase("AUTO", "en-US", ExpectedResult = "en")]
        [TestCase("auto", "de-DE", ExpectedResult = "en")]
        [TestCase("pl", "en-US", ExpectedResult = "pl")]
        [TestCase(" PL ", "en-US", ExpectedResult = "pl")]
        [TestCase("en", "pl-PL", ExpectedResult = "en")]
        [TestCase("klingon", "pl-PL", ExpectedResult = "en")]
        [TestCase("pl-PL", "en-US", ExpectedResult = "pl")]
        [TestCase("PL_pl", "en-US", ExpectedResult = "pl")]
        public string Resolve_PicksTheLanguageOfTheSettingOrOfWindows(string? setting, string uiCulture)
        {
            return UiLanguage.Resolve(setting, new CultureInfo(uiCulture), Available);
        }

        [TestCase(0, ExpectedResult = PluralRules.Many)]
        [TestCase(1, ExpectedResult = PluralRules.One)]
        [TestCase(2, ExpectedResult = PluralRules.Few)]
        [TestCase(4, ExpectedResult = PluralRules.Few)]
        [TestCase(5, ExpectedResult = PluralRules.Many)]
        [TestCase(12, ExpectedResult = PluralRules.Many)]
        [TestCase(14, ExpectedResult = PluralRules.Many)]
        [TestCase(21, ExpectedResult = PluralRules.Many)]
        [TestCase(22, ExpectedResult = PluralRules.Few)]
        [TestCase(25, ExpectedResult = PluralRules.Many)]
        [TestCase(112, ExpectedResult = PluralRules.Many)]
        [TestCase(124, ExpectedResult = PluralRules.Few)]
        public string CategoryOf_FollowsPolishGrammar(int count)
        {
            return PluralRules.CategoryOf("pl", count);
        }

        [TestCase(0, ExpectedResult = PluralRules.Other)]
        [TestCase(1, ExpectedResult = PluralRules.One)]
        [TestCase(2, ExpectedResult = PluralRules.Other)]
        public string CategoryOf_UsesTheEnglishRuleForOtherLanguages(int count)
        {
            return PluralRules.CategoryOf("de", count);
        }

        [Test]
        public void Plural_FillsInTheCountWithTheMatchingForm()
        {
            Localizer polish = new("pl");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(polish.Plural(TextKeys.EngineCodeCopiedLines, 1), Is.EqualTo("Skopiowano 1 wiersz."));
                Assert.That(polish.Plural(TextKeys.EngineCodeCopiedLines, 3), Is.EqualTo("Skopiowano 3 wiersze."));
                Assert.That(polish.Plural(TextKeys.EngineCodeCopiedLines, 11), Is.EqualTo("Skopiowano 11 wierszy."));
                Assert.That(Localizer.English.Plural(TextKeys.EngineCodeCopiedLines, 2), Is.EqualTo("Copied 2 lines."));
            }
        }

        [Test]
        public void ALanguageCodeInAnyCaseGetsThePluralRuleOfItsLanguage()
        {
            Localizer polish = new("PL");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(polish.Language, Is.EqualTo("pl"));
                Assert.That(polish.Plural(TextKeys.EngineCodeCopiedLines, 3), Is.EqualTo("Skopiowano 3 wiersze."));
            }
        }

        [Test]
        public void Format_ReplacesEveryNamedValue()
        {
            string text = new Localizer("pl").Format(TextKeys.EngineTreeWritten, ("count", "7"), ("path", @"C:\uia-tree.txt"));

            Assert.That(text, Is.EqualTo(@"Zapisano węzły (7) do pliku C:\uia-tree.txt"));
        }

        [Test]
        public void Get_ReturnsTheKeyItselfWhenNoLanguageHasIt()
        {
            Assert.That(new Localizer("pl").Get("engine.noSuchText"), Is.EqualTo("engine.noSuchText"));
        }

        [Test]
        public void ALanguageWithoutAFileGetsEnglish()
        {
            Localizer unknown = new("xx");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unknown.Language, Is.EqualTo("en"));
                Assert.That(unknown.Get(TextKeys.EngineCodeCopied), Is.EqualTo("The code was copied."));
            }
        }

        [Test]
        public void LanguageName_TranslatesPlainTextOnly()
        {
            Localizer polish = new("pl");
            SyntaxHighlighterRegistry registry = SyntaxHighlighterRegistry.CreateDefault();
            Assert.That(registry.TryGet("csharp", out ISyntaxHighlightProvider? csharp), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(polish.LanguageName(new PlainTextHighlightProvider()), Is.EqualTo("Zwykły tekst"));
                Assert.That(polish.LanguageName(csharp!), Is.EqualTo("C#"));
            }
        }

        [Test]
        public void WithPrefix_GivesOnlyTheTextsOfOnePartOfTheInterface()
        {
            IReadOnlyDictionary<string, string> ribbon = new Localizer("pl").WithPrefix(TextKeys.RibbonPrefix);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ribbon["ribbon.grpTools.label"], Is.EqualTo("Narzędzia"));
                Assert.That(ribbon.Keys, Has.All.StartWith(TextKeys.RibbonPrefix));
            }
        }
    }
}
