using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class CodeBlockLinkLanguageTests
    {
        [Test]
        public void CopyHref_CarriesTheLanguageAlongsideTheId()
        {
            Assert.That(CodeBlockLink.CopyHref("abc123", "csharp"), Is.EqualTo("momtion:copy?id=abc123&lang=csharp"));
        }

        [Test]
        public void TryGetCopyBlockId_StillReadsALinkWrittenBeforeLanguagesWereStored()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CodeBlockLink.TryGetCopyBlockId("momtion:copy?id=abc123", out string blockId), Is.True);
                Assert.That(blockId, Is.EqualTo("abc123"));
            }
        }

        [Test]
        public void TryGetCopyBlockId_IgnoresTheLanguageWhenReadingTheId()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CodeBlockLink.TryGetCopyBlockId("momtion:copy?id=abc123&lang=sql", out string blockId), Is.True);
                Assert.That(blockId, Is.EqualTo("abc123"));
            }
        }

        [Test]
        public void RunLinksToBlock_DoesNotMatchAnIdThatIsMerelyAPrefix()
        {
            string html = "<a href=\"momtion:copy?id=abcdef&lang=sql\">Copy</a>";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CodeBlockLink.RunLinksToBlock(html, "abcdef"), Is.True);
                Assert.That(CodeBlockLink.RunLinksToBlock(html, "abc"), Is.False);
            }
        }

        [Test]
        public void TryGetRunLink_ReadsBothHalvesOutOfTheHeaderMarkup()
        {
            string html = "C#&nbsp;&nbsp;&nbsp;<a href=\"momtion:copy?id=deadbeef&lang=csharp\">Copy</a>";

            Assert.That(CodeBlockLink.TryGetRunLink(html, out string blockId, out string languageId), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(blockId, Is.EqualTo("deadbeef"));
                Assert.That(languageId, Is.EqualTo("csharp"));
            }
        }

        [Test]
        public void RenderedBlock_RecordsTheLanguageInItsCopyLink()
        {
            PageEditor editor = new(StyleCatalog.CreateDefault(), SyntaxHighlighterRegistry.CreateDefault());
            string source = PageXml.Page(PageXml.Oe("select 1"), PageXml.Oe("from dual"));

            PageEditResult result = editor.FormatCode(source, "sql");
            XNamespace one = PageXml.Namespace;
            string header = XDocument.Parse(result.Xml!).Descendants(one + "T").Select(run => run.FirstNode is XCData data ? data.Value : run.Value).First(text => text.Contains("momtion:copy"));

            Assert.That(header, Does.Contain("&amp;lang=sql"));
        }
    }
}
