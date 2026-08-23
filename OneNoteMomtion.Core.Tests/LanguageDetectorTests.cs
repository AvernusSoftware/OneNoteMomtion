using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting.Detection;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class LanguageDetectorTests
    {
        private static readonly LanguageDetector Detector = new();

        private static string? Detect(string code)
        {
            return Detector.Detect(code).LanguageId;
        }

        [Test]
        public void Detect_RecognisesJsonByParsingIt()
        {
            Assert.That(Detect("{\n  \"name\": \"value\",\n  \"count\": 3\n}"), Is.EqualTo("json"));
        }

        [Test]
        public void Detect_RecognisesXmlByParsingIt()
        {
            Assert.That(Detect("<?xml version=\"1.0\"?>\n<root attr=\"1\">\n  <child>text</child>\n</root>"), Is.EqualTo("xml"));
        }

        [Test]
        public void Detect_RecognisesSqlWhateverTheLetterCase()
        {
            Assert.That(Detect("select id, name\nfrom customers\nwhere active = 1\norder by name"), Is.EqualTo("sql"));
        }

        [Test]
        public void Detect_RecognisesPowerShell()
        {
            Assert.That(Detect("$items = Get-ChildItem -Path C:\\temp\nWrite-Host $items.Count"), Is.EqualTo("powershell"));
        }

        [Test]
        public void Detect_RecognisesCSharp()
        {
            Assert.That(Detect("using System;\n\nnamespace Demo\n{\n    public class Thing\n    {\n        public void Run() { Console.WriteLine(\"hi\"); }\n    }\n}"), Is.EqualTo("csharp"));
        }

        [Test]
        public void Detect_RecognisesJava()
        {
            Assert.That(Detect("package com.example;\n\nimport java.util.List;\n\npublic class Thing {\n    public static void main(String[] args) {\n        System.out.println(\"hi\");\n    }\n}"), Is.EqualTo("java"));
        }

        [Test]
        public void Detect_RecognisesGroovy()
        {
            Assert.That(Detect("def names = ['a', 'b']\nnames.each { println it }"), Is.EqualTo("groovy"));
        }

        [Test]
        public void Detect_SaysNothingWhenTheSnippetCouldBeAnything()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Detect("x = 1\ny = 2\nz = x + y"), Is.Null);
                Assert.That(Detect("if (a) {\n    b();\n}"), Is.Null);
            }
        }

        [Test]
        public void Detect_SaysNothingForEmptyInput()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Detect(string.Empty), Is.Null);
                Assert.That(Detect("   \n  "), Is.Null);
            }
        }

        [Test]
        public void Detect_ReportsTheNumbersBehindARejection()
        {
            LanguageDetectionResult result = Detector.Detect("if (a) {\n    b();\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Recognised, Is.False);
                Assert.That(result.Score, Is.LessThan(new CodeDetectionOptions().MinScore).Or.Property(nameof(result.Margin)).LessThan(new CodeDetectionOptions().MinMargin));
            }
        }

        [Test]
        public void Detect_CanBeMadeStricterThroughTheOptions()
        {
            CodeDetectionOptions strict = new() { MinScore = 1.5 };

            Assert.That(Detector.Detect("using System;\nnamespace Demo { }", strict).LanguageId, Is.Null);
        }
    }
}
