using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Markdown;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class MarkdownShortcutTests
    {
        [TestCase("# Title", MarkdownBlockKind.Heading1, 2)]
        [TestCase("## Title", MarkdownBlockKind.Heading2, 3)]
        [TestCase("### Title", MarkdownBlockKind.Heading3, 4)]
        public void TryParse_RecognizesSupportedPrefixes(string input, MarkdownBlockKind expectedKind, int expectedPrefixLength)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(MarkdownShortcut.TryParse(input, out MarkdownBlockKind kind, out int prefixLength), Is.True);
                Assert.That(kind, Is.EqualTo(expectedKind));
                Assert.That(prefixLength, Is.EqualTo(expectedPrefixLength));
            }
        }

        [TestCase("")]
        [TestCase("#")]
        [TestCase("#hashtag")]
        [TestCase("#1 issue")]
        [TestCase("#### Too deep")]
        [TestCase(" # Indented")]
        [TestCase("Text # middle")]
        public void TryParse_RejectsEverythingElse(string input)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(MarkdownShortcut.TryParse(input, out MarkdownBlockKind kind, out int prefixLength), Is.False);
                Assert.That(kind, Is.EqualTo(MarkdownBlockKind.None));
                Assert.That(prefixLength, Is.Zero);
            }
        }

        [Test]
        public void Apply_ConvertsParagraphAndAppliesHeadingStyle()
        {
            PageDocument page = PageDocument.Parse(PageXml.Page(PageXml.Oe("## Chapter two")));
            MarkdownShortcutApplier applier = new(StyleCatalog.CreateDefault());

            int converted = applier.Apply(page);

            Assert.That(converted, Is.EqualTo(1));

            XElement outlineElement = page.AllOutlineElements().Single();
            XElement run = outlineElement.Elements(page.One + "T").Single();

            Assert.That(HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(run)), Is.EqualTo("Chapter two"));

            string? style = (string?)outlineElement.Attribute("style");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(style, Does.Contain(PageXml.FontSizeOf(StyleNames.Heading2)).And.Not.Contain("font-weight:bold"));
                Assert.That((string?)outlineElement.Attribute("spaceBefore"), Is.EqualTo("10.0"));
            }
        }

        [Test]
        public void Apply_LeavesOrdinaryParagraphsAlone()
        {
            PageDocument page = PageDocument.Parse(PageXml.Page(PageXml.Oe("Just a sentence with # inside")));

            int converted = new MarkdownShortcutApplier(StyleCatalog.CreateDefault()).Apply(page);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(converted, Is.Zero);
                Assert.That(PageXml.AttributeOfFirst(page.ToXmlString(), "OE", "style"), Is.Null);
            }
        }

        [Test]
        public void Apply_ClearsInlineFormattingOfTheRemainingText()
        {
            PageDocument page = PageDocument.Parse(PageXml.Page(PageXml.Oe("# <span style='color:#FF0000'>Red</span> title")));

            new MarkdownShortcutApplier(StyleCatalog.CreateDefault()).Apply(page);

            Assert.That(PageDocument.GetRunHtml(page.AllTextRuns().Single()), Is.EqualTo("Red title"));
        }

        [Test]
        public void Apply_SkipsConversionWhenStyleWasRemovedFromConfiguration()
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();
            catalog.Styles.Remove(StyleNames.Heading1);

            PageDocument page = PageDocument.Parse(PageXml.Page(PageXml.Oe("# Title")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(new MarkdownShortcutApplier(catalog).Apply(page), Is.Zero);
                Assert.That(HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(page.AllTextRuns().Single())), Is.EqualTo("# Title"));
            }
        }
    }
}