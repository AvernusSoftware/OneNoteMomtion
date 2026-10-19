using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Highlighting.Providers;
using OneNoteMomtion.Core.Reformat;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Text;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class LocalizedPageTests
    {
        private static PageEditor CreateEditor(string language, bool showCopyButton = true, string? copyButtonText = null)
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();
            catalog.Language = language;
            catalog.CodeBlock.ShowCopyButton = showCopyButton;
            catalog.CodeBlock.CopyButtonText = copyButtonText;

            return new PageEditor(catalog, SyntaxHighlighterRegistry.CreateDefault());
        }

        private static string HeaderText(string pageXml)
        {
            PageDocument page = PageDocument.Parse(pageXml);
            XElement header = page.Document.Descendants(page.One + "Row").First().Descendants(page.One + "T").First();

            return HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(header)).Replace(' ', ' ');
        }

        [Test]
        public void Messages_FollowTheLanguageOfTheCatalog()
        {
            string xml = PageXml.Page(PageXml.Oe("text", selected: false));

            PageEditResult result = CreateEditor("pl").ApplyTextCase(xml, TextCaseMode.Upper);

            Assert.That(result.Message, Is.EqualTo("Zaznacz tekst, w którym chcesz zmienić wielkość liter."));
        }

        [Test]
        public void FormatCode_TranslatesTheDefaultCopyButtonAndPlainText()
        {
            PageEditResult result = CreateEditor("pl").FormatCode(PageXml.Page(PageXml.Oe("hello")), PlainTextHighlightProvider.Id);

            Assert.That(HeaderText(result.Xml!), Does.StartWith("Zwykły tekst").And.EndWith("Kopiuj"));
        }

        [Test]
        public void FormatCode_KeepsACopyButtonTextTheUserChose()
        {
            PageEditResult result = CreateEditor("pl", copyButtonText: "Do schowka").FormatCode(PageXml.Page(PageXml.Oe("int a;")), "csharp");

            Assert.That(HeaderText(result.Xml!), Does.EndWith("Do schowka"));
        }

        [Test]
        public void FormatCode_ShowsTheEnglishButtonTextInEnglish()
        {
            PageEditResult result = CreateEditor("en").FormatCode(PageXml.Page(PageXml.Oe("int a;")), "csharp");

            Assert.That(HeaderText(result.Xml!), Does.EndWith("Copy"));
        }

        [Test]
        public void FormatCode_KeepsCopyWhenTheUserChoseItInAnotherLanguage()
        {
            PageEditResult result = CreateEditor("pl", copyButtonText: "Copy").FormatCode(PageXml.Page(PageXml.Oe("int a;")), "csharp");

            Assert.That(HeaderText(result.Xml!), Does.EndWith("Copy"));
        }

        [Test]
        public void ReformatPage_RecognisesABlockWithTheDefaultButtonAfterTheUserChoseTheirOwn()
        {
            // An older copy link carries no language, so the block is recognised by its header text - which still
            // ends with the default "Kopiuj" after the user switched to a button text of their own.
            string polish = CreateEditor("pl").FormatCode(PageXml.Page(PageXml.Oe("using System;"), PageXml.Oe("public class Program"), PageXml.Oe("{"), PageXml.Oe("    private static void Main() { }"), PageXml.Oe("}")), PlainTextHighlightProvider.Id).Xml!;
            string withoutLanguageInLink = polish.Replace("&amp;lang=" + PlainTextHighlightProvider.Id, string.Empty).Replace("&lang=" + PlainTextHighlightProvider.Id, string.Empty);
            Assume.That(withoutLanguageInLink, Is.Not.EqualTo(polish));

            PageEditResult reformatted = CreateEditor("pl", copyButtonText: "Do schowka").ReformatPage(withoutLanguageInLink);

            Assert.That(HeaderText(reformatted.Xml!), Does.StartWith("Zwykły tekst"));
        }

        [Test]
        public void ReformatPage_RecognisesAPlainTextBlockMadeInAnotherLanguage()
        {
            // Without a copy button the header shows only the language name, so this block is recognised by
            // "Zwykły tekst" alone - code that looks like C# would otherwise be detected and highlighted as C#.
            string polish = CreateEditor("pl", showCopyButton: false).FormatCode(PageXml.Page(PageXml.Oe("using System;"), PageXml.Oe("public class Program"), PageXml.Oe("{"), PageXml.Oe("    private static void Main() { }"), PageXml.Oe("}")), PlainTextHighlightProvider.Id).Xml!;

            PageEditResult reformatted = CreateEditor("en", showCopyButton: false).ReformatPage(polish);

            Assert.That(HeaderText(reformatted.Xml!), Is.EqualTo("Plain text"));
        }

        [TestCase("Plain text")]
        [TestCase("Zwykły tekst")]
        [TestCase("zwykły TEKST")]
        public void ResolveLanguageId_KnowsPlainTextInEveryLanguage(string displayName)
        {
            Assert.That(CodeBlockFinder.ResolveLanguageId(SyntaxHighlighterRegistry.CreateDefault(), displayName), Is.EqualTo(PlainTextHighlightProvider.Id));
        }
    }
}
