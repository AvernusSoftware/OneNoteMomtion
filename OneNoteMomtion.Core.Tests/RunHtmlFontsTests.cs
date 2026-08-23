using NUnit.Framework;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class RunHtmlFontsTests
    {
        [Test]
        public void Common_FindsTheFontDeclaredOnTheSpanAroundTheText()
        {
            Assert.That(RunHtmlFonts.Common("<span style='font-family:\"Segoe UI\"'>hello</span>"), Does.ContainKey("font-family").WithValue("\"Segoe UI\""));
        }

        [Test]
        public void Common_PassesOverASpanHoldingNothingButAnEmoji()
        {
            string html = "<a href=\"onenote:#page\"><span style='font-family:\"Segoe UI Emoji\"'>&#128296;</span>"
                + "<span style='font-family:\n\"Segoe UI\"'>BL260037/7</span></a>"
                + "<span style='font-family:\"Segoe UI\"'> -&gt; podsumowanie</span>";

            Assert.That(RunHtmlFonts.Common(html), Does.ContainKey("font-family").WithValue("\"Segoe UI\""));
        }

        [Test]
        public void Common_FindsNothingWhenTheSpansNameDifferentFonts()
        {
            string html = "<span style='font-family:\"Segoe UI\"'>prose</span><span style='font-family:Consolas'>code</span>";

            Assert.That(RunHtmlFonts.Common(html), Does.Not.ContainKey("font-family"));
        }

        [Test]
        public void Common_FindsNothingWhenSomeTextSitsOutsideEverySpan()
        {
            string html = "bare text<span style='font-family:\"Segoe UI\"'>spanned text</span>";

            Assert.That(RunHtmlFonts.Common(html), Does.Not.ContainKey("font-family"));
        }

        [Test]
        public void Common_InheritsTheFontOfAnEnclosingSpan()
        {
            string html = "<span style='font-family:\"Segoe UI\"'>outer<span style='font-weight:bold'>inner</span></span>";

            Assert.That(RunHtmlFonts.Common(html), Does.ContainKey("font-family").WithValue("\"Segoe UI\""));
        }

        [Test]
        public void Common_FindsNothingInPlainText()
        {
            Assert.That(RunHtmlFonts.Common("just some text"), Is.Empty);
        }

        [Test]
        public void Common_FindsNothingInAnEmptyRun()
        {
            Assert.That(RunHtmlFonts.Common(null), Is.Empty);
        }

        [Test]
        public void Common_ReadsAStyleAttributeThatFollowsOtherAttributes()
        {
            string html = "<span lang=\"pl\" style=\"font-family:'Segoe UI';font-size:11.0pt\">text</span>";
            System.Collections.Generic.Dictionary<string, string> common = RunHtmlFonts.Common(html);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(common, Does.ContainKey("font-family").WithValue("'Segoe UI'"));
                Assert.That(common, Does.ContainKey("font-size").WithValue("11.0pt"));
            }
        }
    }
}
