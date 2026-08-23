using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class CodeBlockLinkTests
    {
        [Test]
        public void CopyHref_BuildsAnAddressTheHandlerCanReadBack()
        {
            string href = CodeBlockLink.CopyHref("3f2b9c");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(href, Is.EqualTo("momtion:copy?id=3f2b9c"));
                Assert.That(CodeBlockLink.TryGetCopyBlockId(href, out string blockId), Is.True);
                Assert.That(blockId, Is.EqualTo("3f2b9c"));
            }
        }

        [Test]
        public void TryGetCopyBlockId_AcceptsTheTrailingSlashWindowsSometimesAppends()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CodeBlockLink.TryGetCopyBlockId("momtion:copy?id=abc123/", out string blockId), Is.True);
                Assert.That(blockId, Is.EqualTo("abc123"));
            }
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("momtion:copy?id=")]
        [TestCase("momtion:open?id=abc")]
        [TestCase("https://example.com/copy?id=abc")]
        [TestCase("momtion:copy?id=../../etc")]
        [TestCase("momtion:copy?id=a<b")]
        public void TryGetCopyBlockId_RejectsAnythingWeCouldNotHaveGenerated(string? uri)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CodeBlockLink.TryGetCopyBlockId(uri, out string blockId), Is.False);
                Assert.That(blockId, Is.Empty);
            }
        }

        [Test]
        public void RunLinksToBlock_MatchesOnlyTheBlockItBelongsTo()
        {
            string html = "C#&nbsp;&nbsp;<a href=\"momtion:copy?id=aaa\">Copy</a>";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CodeBlockLink.RunLinksToBlock(html, "aaa"), Is.True);
                Assert.That(CodeBlockLink.RunLinksToBlock(html, "bbb"), Is.False);
                Assert.That(CodeBlockLink.RunLinksToBlock("plain text", "aaa"), Is.False);
            }
        }
    }
}
