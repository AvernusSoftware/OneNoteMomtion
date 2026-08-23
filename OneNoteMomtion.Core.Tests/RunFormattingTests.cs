using System.Collections.Generic;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class RunFormattingTests
    {
        private static readonly string[] CompetingProperties = ["font-family", "font-size", "color"];

        [Test]
        public void RewriteSegments_KeepsTheMarkupWhenTheTextGetsShorter()
        {
            string html = "a  <b>b  c</b>  d";

            string rewritten = HtmlRunRewriter.RewriteSegments(html, context => context.Text.Replace("  ", " "));

            Assert.That(rewritten, Is.EqualTo("a <b>b c</b> d"));
        }

        [Test]
        public void RewriteSegments_ReportsWhereEachStretchSitsInTheRun()
        {
            List<string> seen = [];

            HtmlRunRewriter.RewriteSegments("one<b>two</b>three", context =>
            {
                seen.Add(context.Text + ":" + (context.IsFirst ? "F" : "-") + (context.IsLast ? "L" : "-") + (context.PreviousCharacter?.ToString() ?? "_"));
                return context.Text;
            });

            Assert.That(seen, Is.EqualTo(new[] { "one:F-_", "two:--e", "three:-Lo" }));
        }

        [Test]
        public void ReadTextSegments_KeepsNonBreakingSpacesApartFromOrdinaryOnes()
        {
            IReadOnlyList<string> segments = HtmlRunRewriter.ReadTextSegments("a&nbsp;b c");

            Assert.That(segments, Is.EqualTo(new[] { "a b c" }));
        }

        [Test]
        public void WriteTextSegments_PutsNonBreakingSpacesBackAsEntities()
        {
            string written = HtmlRunRewriter.WriteTextSegments("<b>x</b>", ["a b"]);

            Assert.That(written, Is.EqualTo("<b>a&nbsp;b</b>"));
        }

        [Test]
        public void RemoveStyleProperties_DropsOnlyTheNamedDeclarations()
        {
            string html = "<span style='font-family:Arial;font-weight:bold;color:#FF0000'>text</span>";

            string rewritten = HtmlRunRewriter.RemoveStyleProperties(html, CompetingProperties);

            Assert.That(rewritten, Is.EqualTo("<span style='font-weight:bold'>text</span>"));
        }

        [Test]
        public void RemoveStyleProperties_RemovesTheWholeAttributeWhenNothingIsLeft()
        {
            string html = "<span style='font-family:Arial;font-size:9.0pt'>text</span>";

            string rewritten = HtmlRunRewriter.RemoveStyleProperties(html, CompetingProperties);

            Assert.That(rewritten, Is.EqualTo("<span>text</span>"));
        }

        [Test]
        public void RemoveStyleProperties_LeavesAnAttributeThatMerelyEndsInStyleAlone()
        {
            string html = "<span data-style='font-family:Arial'>text</span>";

            Assert.That(HtmlRunRewriter.RemoveStyleProperties(html, CompetingProperties), Is.EqualTo(html));
        }

        [Test]
        public void RemoveEmptySpans_DropsBareSpansAndKeepsTheRest()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(HtmlRunRewriter.RemoveEmptySpans("<span>a<b>c</b></span>"), Is.EqualTo("a<b>c</b>"));
                Assert.That(HtmlRunRewriter.RemoveEmptySpans("<span style='color:red'>a</span>"), Is.EqualTo("<span style='color:red'>a</span>"));
            }
        }

        [Test]
        public void ApplyParagraphStyle_PreserveKeepsEmphasisAndLinksButDropsCompetingFonts()
        {
            XElement outlineElement = ParseParagraph("<span style='font-family:Arial;font-size:9.0pt'>plain</span> <b>bold</b> <a href=\"https://example.com\">link</a>");

            OeStyleWriter.ApplyParagraphStyle(outlineElement, StyleCatalog.CreateDefault().Get(StyleNames.Heading1)!, PageXml.Namespace, RunFormattingMode.Preserve);

            string html = PageDocument.GetRunHtml(outlineElement.Element(XNamespace.Get(PageXml.Namespace) + "T"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(html, Does.Contain("<b>bold</b>"));
                Assert.That(html, Does.Contain("<a href=\"https://example.com\">link</a>"));
                Assert.That(html, Does.Not.Contain("font-family"));
            }
        }

        [Test]
        public void ApplyParagraphStyle_StripIsStillTheOldDestructiveBehaviour()
        {
            XElement outlineElement = ParseParagraph("<b>bold</b>");

            OeStyleWriter.ApplyParagraphStyle(outlineElement, StyleCatalog.CreateDefault().Get(StyleNames.Heading1)!, PageXml.Namespace, RunFormattingMode.Strip);

            Assert.That(PageDocument.GetRunHtml(outlineElement.Element(XNamespace.Get(PageXml.Namespace) + "T")), Is.EqualTo("bold"));
        }

        [Test]
        public void NeedsParagraphStyle_IsFalseOnceTheStyleIsAlreadyThere()
        {
            StyleDefinition style = StyleCatalog.CreateDefault().Get(StyleNames.Heading1)!;
            XElement outlineElement = ParseParagraph("<b>bold</b>");

            OeStyleWriter.ApplyParagraphStyle(outlineElement, style, PageXml.Namespace, RunFormattingMode.Preserve);

            Assert.That(OeStyleWriter.NeedsParagraphStyle(outlineElement, style, PageXml.Namespace, RunFormattingMode.Preserve), Is.False);
        }

        private static XElement ParseParagraph(string html)
        {
            PageDocument page = PageDocument.Parse(PageXml.Page(PageXml.StyledOe(html)));

            return page.AllOutlineElements().GetEnumerator() is IEnumerator<XElement> enumerator && enumerator.MoveNext() ? enumerator.Current : throw new AssertionException("The test page has no paragraph.");
        }
    }
}
