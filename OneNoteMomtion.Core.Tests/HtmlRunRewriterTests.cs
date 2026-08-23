using NUnit.Framework;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class HtmlRunRewriterTests
    {
        [Test]
        public void ExtractPlainText_DropsTagsAndDecodesEntities()
        {
            string html = "<span style='color:#FF0000'>a &amp; b</span> &lt;end&gt;";

            Assert.That(HtmlRunRewriter.ExtractPlainText(html), Is.EqualTo("a & b <end>"));
        }

        [Test]
        public void ExtractPlainText_TreatsNonBreakingSpaceAsRegularSpace()
        {
            Assert.That(HtmlRunRewriter.ExtractPlainText("two&nbsp;words"), Is.EqualTo("two words"));
        }

        [Test]
        public void RewriteText_KeepsFormattingWhenTransformPreservesLength()
        {
            string html = "<span style='font-weight:bold'>abc</span>def";

            string result = HtmlRunRewriter.RewriteText(html, text => text.ToUpperInvariant());

            Assert.That(result, Is.EqualTo("<span style='font-weight:bold'>ABC</span>DEF"));
        }

        [Test]
        public void RewriteText_PassesWholeRunToTransformNotIndividualSegments()
        {
            string html = "a<span style='font-style:italic'>BC</span>";
            string? seenByTransform = null;

            string result = HtmlRunRewriter.RewriteText(html, text =>
            {
                seenByTransform = text;
                return text.ToLowerInvariant();
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seenByTransform, Is.EqualTo("aBC"));
                Assert.That(result, Is.EqualTo("a<span style='font-style:italic'>bc</span>"));
            }
        }

        [Test]
        public void RewriteText_ReturnsInputUnchangedWhenTransformIsIdentity()
        {
            string html = "<span style='color:#000000'>abc</span>";

            Assert.That(HtmlRunRewriter.RewriteText(html, text => text), Is.SameAs(html));
        }

        [Test]
        public void RemoveLeadingCharacters_KeepsTrailingFormatting()
        {
            string html = "# <span style='font-weight:bold'>Title</span>";

            int count = 2;
            string result = HtmlRunRewriter.RemoveLeadingCharacters(html, ref count);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(count, Is.Zero);
                Assert.That(result, Is.EqualTo("<span style='font-weight:bold'>Title</span>"));
            }
        }

        [Test]
        public void RemoveLeadingCharacters_ReportsRemainderWhenRunIsTooShort()
        {
            int count = 5;
            string result = HtmlRunRewriter.RemoveLeadingCharacters("ab", ref count);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(count, Is.EqualTo(3));
                Assert.That(result, Is.Empty);
            }
        }

        [Test]
        public void RemoveLeadingCharacters_SpansMultipleSegments()
        {
            string html = "<b>ab</b>cd";

            int count = 3;
            string result = HtmlRunRewriter.RemoveLeadingCharacters(html, ref count);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(count, Is.Zero);
                Assert.That(result, Is.EqualTo("<b></b>d"));
            }
        }

        [Test]
        public void Encode_EscapesMarkupButLeavesSpacesAlone()
        {
            Assert.That(HtmlRunRewriter.Encode("a & b <c>"), Is.EqualTo("a &amp; b &lt;c&gt;"));
        }

        [Test]
        public void EncodePreservingIndent_ProtectsLeadingWhitespace()
        {
            Assert.That(HtmlRunRewriter.EncodePreservingIndent("\tcode"), Is.EqualTo("&nbsp;&nbsp;&nbsp;&nbsp;code"));
        }

        [Test]
        public void StripTags_ProducesPlainCdataContent()
        {
            Assert.That(HtmlRunRewriter.StripTags("<span style='color:#111'>a &amp; b</span>"), Is.EqualTo("a &amp; b"));
        }

        [Test]
        public void Decode_LeavesUnknownEntitiesUntouched()
        {
            Assert.That(HtmlRunRewriter.Decode("100 &euro? x"), Is.EqualTo("100 &euro? x"));
        }
    }
}
