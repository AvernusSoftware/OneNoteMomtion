using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class SyntaxHighlightingTests
    {
        private static readonly SyntaxHighlighterRegistry Registry = SyntaxHighlighterRegistry.CreateDefault();

        public static IEnumerable<TestCaseData> LanguageSamples()
        {
            yield return new TestCaseData(
                    "csharp",
                    "// comment with the word class\npublic class Foo {\n\tint x = 0x1F; // 31\n\tstring s = @\"a\"\"b\";\n}")
                .SetArgDisplayNames("csharp");

            yield return new TestCaseData(
                    "java",
                    "@Override\npublic void run() {\n    List<String> xs = new ArrayList<>();\n    /* block */\n}")
                .SetArgDisplayNames("java");

            yield return new TestCaseData(
                    "groovy",
                    "def greet = { name -> \"\"\"Hello $name\"\"\" }\nassert greet('x') == 'Hello x'")
                .SetArgDisplayNames("groovy");

            yield return new TestCaseData(
                    "sql",
                    "-- comment\nSELECT COUNT(*) FROM [dbo].[Users] u WHERE u.Name = 'O''Brien' AND id > 10;")
                .SetArgDisplayNames("sql");

            yield return new TestCaseData(
                    "json",
                    "{\n  \"name\": \"value\",\n  \"count\": -12.5e3,\n  \"ok\": true,\n  \"nested\": [null]\n}")
                .SetArgDisplayNames("json");

            yield return new TestCaseData(
                    "xml",
                    "<?xml version=\"1.0\"?>\n<!-- comment -->\n<root attr=\"v\">text &amp; more<child/></root>")
                .SetArgDisplayNames("xml");

            yield return new TestCaseData(
                    "powershell",
                    "<# block #>\n# comment\nfunction Get-Sum {\n    param([int]$a, [int]$b)\n    if ($a -eq 0) { return $b }\n    Write-Host \"sum=$($a + $b)\" -ForegroundColor Green\n}")
                .SetArgDisplayNames("powershell");
        }

        [TestCaseSource(nameof(LanguageSamples))]
        public void Tokenize_CoversEveryCharacterOfTheSource(string languageId, string source)
        {
            Assert.That(Registry.TryGet(languageId, out ISyntaxHighlightProvider? provider), Is.True);

            StringBuilder rebuilt = new();
            int expectedStart = 0;

            foreach (Token token in provider!.Tokenize(source))
            {
                Assert.That(token.Start, Is.EqualTo(expectedStart), "Tokens have to form a continuous range.");
                rebuilt.Append(token.Text);
                expectedStart = token.End;
            }

            Assert.That(rebuilt.ToString(), Is.EqualTo(source));
        }

        [Test]
        public void CSharp_DoesNotColorKeywordsInsideComments()
        {
            IReadOnlyList<Token> tokens = Tokenize("csharp", "// class Foo");

            Assert.That(tokens, Has.Count.EqualTo(1));
            Assert.That(tokens[0].Kind, Is.EqualTo(TokenKind.Comment));
        }

        [Test]
        public void CSharp_DoesNotColorKeywordsInsideStrings()
        {
            IReadOnlyList<Token> tokens = Tokenize("csharp", "\"public static\"");

            Assert.That(tokens, Has.Count.EqualTo(1));
            Assert.That(tokens[0].Kind, Is.EqualTo(TokenKind.String));
        }

        [Test]
        public void CSharp_SeparatesControlKeywordsFromDeclarationKeywords()
        {
            IReadOnlyList<Token> tokens = Tokenize("csharp", "public if");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KindOf(tokens, "public"), Is.EqualTo(TokenKind.Keyword));
                Assert.That(KindOf(tokens, "if"), Is.EqualTo(TokenKind.ControlKeyword));
            }
        }

        [Test]
        public void CSharp_RecognizesPreprocessorDirectives()
        {
            Assert.That(Tokenize("csharp", "#region Setup")[0].Kind, Is.EqualTo(TokenKind.Preprocessor));
        }

        [Test]
        public void Json_DistinguishesPropertyNamesFromStringValues()
        {
            IReadOnlyList<Token> tokens = Tokenize("json", "{\"key\": \"value\"}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KindOf(tokens, "\"key\""), Is.EqualTo(TokenKind.PropertyName));
                Assert.That(KindOf(tokens, "\"value\""), Is.EqualTo(TokenKind.String));
            }
        }

        [Test]
        public void Sql_MatchesKeywordsRegardlessOfLetterCase()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Tokenize("sql", "select")[0].Kind, Is.EqualTo(TokenKind.Keyword));
                Assert.That(Tokenize("sql", "SELECT")[0].Kind, Is.EqualTo(TokenKind.Keyword));
            }
        }

        [Test]
        public void Sql_TreatsDoubledApostropheAsEscapeInsideLiteral()
        {
            IReadOnlyList<Token> tokens = Tokenize("sql", "'O''Brien'");

            Assert.That(tokens, Has.Count.EqualTo(1));
            Assert.That(tokens[0].Kind, Is.EqualTo(TokenKind.String));
        }

        [Test]
        public void Xml_SeparatesTagNamesFromAttributeNames()
        {
            IReadOnlyList<Token> tokens = Tokenize("xml", "<root attr=\"v\">");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KindOf(tokens, "<root"), Is.EqualTo(TokenKind.TagName));
                Assert.That(KindOf(tokens, "attr"), Is.EqualTo(TokenKind.AttributeName));
                Assert.That(KindOf(tokens, "\"v\""), Is.EqualTo(TokenKind.String));
            }
        }

        [Test]
        public void PowerShell_DistinguishesComparisonOperatorFromParameterSwitch()
        {
            IReadOnlyList<Token> tokens = Tokenize("powershell", "$a -eq 1; Get-Item -Path $a");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KindOf(tokens, "-eq"), Is.EqualTo(TokenKind.Operator));
                Assert.That(KindOf(tokens, "-Path"), Is.EqualTo(TokenKind.AttributeName));
            }
        }

        [Test]
        public void PowerShell_RecognizesVariablesAndTypeAccelerators()
        {
            IReadOnlyList<Token> tokens = Tokenize("powershell", "[int]$count = 0");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KindOf(tokens, "[int]"), Is.EqualTo(TokenKind.Type));
                Assert.That(KindOf(tokens, "$count"), Is.EqualTo(TokenKind.Annotation));
            }
        }

        [Test]
        public void PowerShell_MatchesKeywordsRegardlessOfLetterCase()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Tokenize("powershell", "foreach")[0].Kind, Is.EqualTo(TokenKind.ControlKeyword));
                Assert.That(Tokenize("powershell", "FOREACH")[0].Kind, Is.EqualTo(TokenKind.ControlKeyword));
            }
        }

        [Test]
        public void PowerShell_TreatsHereStringAsSingleToken()
        {
            IReadOnlyList<Token> tokens = Tokenize("powershell", "@\"\nline1\nline2\n\"@");

            Assert.That(tokens, Has.Count.EqualTo(1));
            Assert.That(tokens[0].Kind, Is.EqualTo(TokenKind.String));
        }

        [Test]
        public void Registry_ExposesAllMvpLanguagesInRegistrationOrder()
        {
            string[] ids = [.. Registry.All().Select(provider => provider.LanguageId)];

            Assert.That(ids, Is.EqualTo(["csharp", "java", "groovy", "sql", "json", "xml", "powershell", "plain"]));
        }

        [Test]
        public void Registry_LeavesPlainTextOutOfTheSelectableLanguages()
        {
            string[] ids = [.. Registry.Selectable().Select(provider => provider.LanguageId)];

            Assert.That(ids, Is.EqualTo(["csharp", "java", "groovy", "sql", "json", "xml", "powershell"]));
        }

        [Test]
        public void Registry_LookupIgnoresLetterCaseAndSurroundingWhitespace()
        {

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Registry.TryGet("  CSharp ", out ISyntaxHighlightProvider? provider), Is.True);
                Assert.That(provider!.LanguageId, Is.EqualTo("csharp"));
            }
        }

        [Test]
        public void Registry_ReplacingLanguageKeepsItsPositionInTheList()
        {
            SyntaxHighlighterRegistry registry = SyntaxHighlighterRegistry.CreateDefault();
            registry.Register(new StubProvider("java", "Java 21"));

            string[] ids = [.. registry.All().Select(provider => provider.LanguageId)];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ids, Is.EqualTo(["csharp", "java", "groovy", "sql", "json", "xml", "powershell", "plain"]));
                Assert.That(registry.All()[1].DisplayName, Is.EqualTo("Java 21"));
            }
        }

        private static IReadOnlyList<Token> Tokenize(string languageId, string source)
        {
            Assert.That(Registry.TryGet(languageId, out ISyntaxHighlightProvider? provider), Is.True);
            return [.. provider!.Tokenize(source)];
        }

        private static TokenKind KindOf(IEnumerable<Token> tokens, string text)
        {
            return tokens.First(token => token.Text == text).Kind;
        }

        private sealed class StubProvider : ISyntaxHighlightProvider
        {
            public StubProvider(string languageId, string displayName)
            {
                LanguageId = languageId;
                DisplayName = displayName;
            }

            public string LanguageId { get; }

            public string DisplayName { get; }

            public IEnumerable<Token> Tokenize(string source)
            {
                yield return new Token(TokenKind.Plain, source, 0);
            }
        }
    }
}
