using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Localization;

namespace OneNoteMomtion.Core.Tests
{
    /// <summary>
    /// Guards every language file against the English one, so a language added later cannot ship with a missing
    /// text, a lost placeholder or a plural form its grammar needs.
    /// </summary>
    [TestFixture]
    public class LocalizationFilesTests
    {
        private static readonly Regex Placeholder = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
        private static readonly Regex PluralSuffix = new("_(one|few|many|other)$", RegexOptions.Compiled);

        private static IEnumerable<string> Languages => TextCatalog.Available;

        private static IReadOnlyDictionary<string, string> English => TextCatalog.TextsOf(UiLanguage.English);

        [Test]
        public void TheEnglishAndPolishFilesAreEmbedded()
        {
            Assert.That(TextCatalog.Available, Is.SupersetOf(new[] { "en", "pl" }));
        }

        [TestCaseSource(nameof(Languages))]
        public void EveryLanguageHasTheSameTextsAsEnglish(string language)
        {
            IReadOnlyDictionary<string, string> texts = TextCatalog.TextsOf(language);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(SingularKeys(texts), Is.EquivalentTo(SingularKeys(English)));
                Assert.That(PluralKeys(texts), Is.EquivalentTo(PluralKeys(English)));
            }
        }

        [TestCaseSource(nameof(Languages))]
        public void EveryPluralTextHasExactlyTheFormsItsLanguageNeeds(string language)
        {
            IReadOnlyDictionary<string, string> texts = TextCatalog.TextsOf(language);

            foreach (string key in PluralKeys(English))
            {
                string[] forms = [.. texts.Keys.Where(candidate => PluralSuffix.Replace(candidate, string.Empty) == key && candidate != key).Select(candidate => candidate[(key.Length + 1)..])];

                Assert.That(forms, Is.EquivalentTo(PluralRules.CategoriesOf(language)), key);
            }
        }

        [TestCaseSource(nameof(Languages))]
        public void EveryTextKeepsThePlaceholdersOfTheEnglishOne(string language)
        {
            IReadOnlyDictionary<string, string> texts = TextCatalog.TextsOf(language);

            foreach (KeyValuePair<string, string> pair in texts)
            {
                string englishKey = English.ContainsKey(pair.Key) ? pair.Key : PluralSuffix.Replace(pair.Key, "_other");

                Assert.That(PlaceholdersOf(pair.Value), Is.EquivalentTo(PlaceholdersOf(English[englishKey])), pair.Key);
            }
        }

        [Test]
        public void EveryKeyTheCodeUsesExistsInEnglish()
        {
            string[] keys = [.. typeof(TextKeys).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => !field.Name.EndsWith("Prefix", StringComparison.Ordinal)).Select(field => (string)field.GetValue(null)!)];

            foreach (string key in keys)
            {
                Assert.That(English.ContainsKey(key) || English.ContainsKey(key + "_one"), Is.True, key);
            }
        }

        [Test]
        public void EveryRibbonControlThatAsksForATextHasOne()
        {
            XDocument ribbon = XDocument.Load(Path.Combine(RepositoryRoot(), "OneNoteMomtion.AddIn", "Ribbon", "Ribbon.xml"));
            List<string> missing = [];

            foreach (XElement control in ribbon.Descendants())
            {
                string? id = (string?)control.Attribute("id");

                foreach ((string callback, string kind) in new[] { ("getLabel", "label"), ("getScreentip", "screentip"), ("getSupertip", "supertip") })
                {
                    string key = TextKeys.RibbonPrefix + id + "." + kind;
                    if (control.Attribute(callback) != null && !English.ContainsKey(key))
                    {
                        missing.Add(key);
                    }
                }
            }

            Assert.That(missing, Is.Empty);
        }

        private static string[] SingularKeys(IReadOnlyDictionary<string, string> texts)
        {
            return [.. texts.Keys.Where(key => !PluralSuffix.IsMatch(key))];
        }

        private static string[] PluralKeys(IReadOnlyDictionary<string, string> texts)
        {
            return [.. texts.Keys.Where(key => PluralSuffix.IsMatch(key)).Select(key => PluralSuffix.Replace(key, string.Empty)).Distinct()];
        }

        private static string[] PlaceholdersOf(string text)
        {
            return [.. Placeholder.Matches(text).Select(match => match.Groups[1].Value).Distinct()];
        }

        private static string RepositoryRoot()
        {
            DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "OneNoteMomtion.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("The repository root (OneNoteMomtion.slnx) was not found above the test directory.");
        }
    }
}
