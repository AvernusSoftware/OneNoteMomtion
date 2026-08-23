using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Spelling;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Text;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class PageEditorTests
    {
        private static PageEditor CreateEditor()
        {
            return new PageEditor(StyleCatalog.CreateDefault(), SyntaxHighlighterRegistry.CreateDefault());
        }

        [Test]
        public void ApplyStyle_SetsParagraphFormattingAndSpacing()
        {
            string xml = PageXml.Page(PageXml.Oe("Some heading"));

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Heading1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "spaceBefore"), Is.EqualTo("10.0"));
            }
        }

        [Test]
        public void ApplyStyle_NormalReplacesTheHeadingQuickStyleWithTheBuiltInBodyOne()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"1\" name=\"p\" fontColor=\"automatic\" fontSize=\"11\"/>"
                + "<one:QuickStyleDef index=\"2\" name=\"h1\" fontColor=\"#1E4E79\" fontSize=\"16\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE quickStyleIndex=\"2\" spaceBefore=\"10.0\" selected=\"all\">"
                + "<one:T selected=\"all\"><![CDATA[plain text]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Normal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "quickStyleIndex"), Is.EqualTo("1"));
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Is.Null);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "spaceBefore"), Is.Null);
            }
        }

        [Test]
        public void ApplyStyle_DoesNotLetTheSaveRestyleTheParagraphsItNeverTouched()
        {
            string bodyCss = "font-family:'Segoe UI';font-size:11.0pt";
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>"
                + "<one:QuickStyleDef index=\"5\" name=\"h2\" fontColor=\"#2E75B5\" font=\"Calibri\" fontSize=\"14.0\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE quickStyleIndex=\"5\" selected=\"all\"><one:T selected=\"all\"><![CDATA[The heading being restyled]]></one:T></one:OE>"
                + "<one:OE quickStyleIndex=\"2\" style=\"" + bodyCss + "\"><one:T><![CDATA[An untouched body paragraph]]></one:T></one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Heading2);

            PageDocument saved = PageDocument.Parse(result.Xml!);
            XElement bystander = saved.AllOutlineElements().ElementAt(1);
            XElement bystanderStyle = saved.FindQuickStyleByIndex((string?)bystander.Attribute("quickStyleIndex"))!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That((string?)bystander.Attribute("style"), Does.Contain("font-family:'Segoe UI'"));
                Assert.That((string?)bystanderStyle.Attribute("font"), Is.EqualTo("Segoe UI"), "A definition still naming Calibri is what makes OneNote restyle the paragraph.");
                Assert.That((string?)bystanderStyle.Attribute("fontSize"), Is.EqualTo("11.0"));
            }
        }

        [Test]
        public void ApplyStyle_NormalDropsTheQuickStyleIndexWhenThePageHasNoBodyDefinition()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"2\" name=\"h1\" fontColor=\"#1E4E79\" fontSize=\"16\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE quickStyleIndex=\"2\" selected=\"all\">"
                + "<one:T selected=\"all\"><![CDATA[plain text]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Normal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "quickStyleIndex"), Is.Null);
            }
        }

        [Test]
        public void ApplyStyle_NormalLeavesTheParagraphWithoutAnyExplicitFormatting()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE style=\"font-family:Segoe UI;font-size:11.0pt;color:#FFFFFF\" selected=\"all\">"
                + "<one:T selected=\"all\"><![CDATA[<span style='color:#FFFFFF'>white text</span>]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Normal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Is.Null);
                Assert.That(result.Xml, Does.Not.Contain("#FFFFFF"));
                Assert.That(PlainTexts(result.Xml!), Does.Contain("white text"));
            }
        }

        [Test]
        public void ApplyStyle_NormalClearsTheFontOneNoteMovedOntoTheRun()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Segoe UI\" fontSize=\"11.0\"/>"
                + "<one:QuickStyleDef index=\"3\" name=\"p\" fontColor=\"#2E75B5\" font=\"Segoe UI\" fontSize=\"14.0\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE quickStyleIndex=\"3\" spaceBefore=\"8.0\" spaceAfter=\"2.0\" selected=\"all\">"
                + "<one:T style=\"font-family:'Segoe UI';font-size:14.0pt;color:#276296\" selected=\"all\"><![CDATA[heading text]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Normal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "T", "style"), Is.Null);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Is.Null);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "spaceBefore"), Is.Null);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "quickStyleIndex"), Is.EqualTo("2"));
                Assert.That(PlainTexts(result.Xml!), Does.Contain("heading text"));
            }
        }

        [Test]
        public void ApplyStyle_NormalPassesOverTheBodyNamedDefinitionsMintedForOurHeadings()
        {
            string headingCss = StyleCatalog.CreateDefault().Get(StyleNames.Heading1)!.ToCss();
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Segoe UI\" fontSize=\"11.0\"/>"
                + "<one:QuickStyleDef index=\"4\" name=\"p\" fontColor=\"#2E75B5\" font=\"Segoe UI\" fontSize=\"14.5\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE quickStyleIndex=\"4\" style=\"" + headingCss + "\"><one:T><![CDATA[heading above]]></one:T></one:OE>"
                + "<one:OE quickStyleIndex=\"4\" style=\"" + headingCss + "\" selected=\"all\">"
                + "<one:T selected=\"all\"><![CDATA[was a heading too]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Normal);

            List<XElement> outlineElements = [.. PageDocument.Parse(result.Xml!).AllOutlineElements()];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That((string?)outlineElements[1].Attribute("quickStyleIndex"), Is.EqualTo("2"));
                Assert.That((string?)outlineElements[1].Attribute("style"), Is.Null);
                Assert.That((string?)outlineElements[0].Attribute("style"), Is.EqualTo(headingCss));
            }
        }

        [Test]
        public void ApplyStyle_StylesAParagraphThatHasIndentedTextUnderIt()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE selected=\"all\"><one:T selected=\"all\"><![CDATA[the paragraph above]]></one:T>"
                + "<one:OEChildren>"
                + "<one:OE selected=\"all\"><one:T selected=\"all\"><![CDATA[indented under it]]></one:T></one:OE>"
                + "</one:OEChildren>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Heading1);

            List<XElement> outlineElements = [.. PageDocument.Parse(result.Xml!).AllOutlineElements()];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.AffectedNodes, Is.EqualTo(2));
                Assert.That((string?)outlineElements[0].Attribute("style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
                Assert.That((string?)outlineElements[1].Attribute("style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
            }
        }

        [Test]
        public void ApplyStyle_LeavesTheParagraphsTheCaretOnlyPassesThrough()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE selected=\"partial\"><one:T><![CDATA[the paragraph above]]></one:T>"
                + "<one:OEChildren>"
                + "<one:OE selected=\"partial\"><one:T selected=\"all\"><![CDATA[where the caret is]]></one:T></one:OE>"
                + "</one:OEChildren>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Heading1);

            List<XElement> outlineElements = [.. PageDocument.Parse(result.Xml!).AllOutlineElements()];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.AffectedNodes, Is.EqualTo(1));
                Assert.That((string?)outlineElements[0].Attribute("style"), Is.Null);
                Assert.That((string?)outlineElements[1].Attribute("style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
            }
        }

        [Test]
        public void ApplyStyle_HeadingClearsTheFontOneNoteMovedOntoTheRun()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE selected=\"all\">"
                + "<one:T style=\"font-family:'Segoe UI';font-size:14.0pt\" selected=\"all\"><![CDATA[smaller heading]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Heading1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "T", "style"), Is.Null);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
            }
        }

        [Test]
        public void ApplyStyle_UnwrapsACodeBlockTableSoTheShadedBackgroundDoesNotStick()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"));
            string codeXml = CreateEditor().FormatCode(xml, "csharp").Xml!;

            PageDocument codePage = PageDocument.Parse(codeXml);
            foreach (XElement selectedElement in codePage.Document.Descendants().Where(e => e.Attribute("selected") != null))
            {
                selectedElement.Attributes("selected").Remove();
            }

            XElement lineInsideTable = codePage.Document.Descendants(codePage.One + "Cell").Last().Descendants(codePage.One + "OE").Single();
            lineInsideTable.SetAttributeValue("selected", "all");
            lineInsideTable.Element(codePage.One + "T")!.SetAttributeValue("selected", "all");

            PageEditResult result = CreateEditor().ApplyStyle(codePage.ToXmlString(), StyleNames.Heading1);

            Assert.That(result.Changed, Is.True);

            PageDocument page = PageDocument.Parse(result.Xml!);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(page.Document.Descendants(page.One + "Table").Any(), Is.False);
                Assert.That(page.Document.Descendants(page.One + "Cell").Any(), Is.False);
            }

            XElement styledLine = page.Document.Descendants(page.One + "OE").Single(oe => ((string?)oe.Attribute("style"))?.Contains(PageXml.FontSizeOf(StyleNames.Heading1)) == true);
            Assert.That(HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(styledLine.Element(page.One + "T"))), Is.EqualTo("public int x;"));
        }

        [Test]
        public void ApplyStyle_ReportsMissingStyleInsteadOfSilentlyDoingNothing()
        {
            PageEditResult result = CreateEditor().ApplyStyle(PageXml.Page(PageXml.Oe("x")), "NoSuchStyle");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.False);
                Assert.That(result.Message, Does.Contain("NoSuchStyle"));
            }
        }

        [Test]
        public void ApplyTextCase_OnlyTouchesSelectedRuns()
        {
            string xml = PageXml.Page(PageXml.Oe("selected text", selected: true), PageXml.Oe("untouched text", selected: false));

            PageEditResult result = CreateEditor().ApplyTextCase(xml, TextCaseMode.Upper);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PlainTexts(result.Xml!), Is.EqualTo(["SELECTED TEXT", "untouched text"]));
            }
        }

        [Test]
        public void ApplyTextCase_KeepsInlineFormatting()
        {
            string xml = PageXml.Page(PageXml.Oe("a<span style='font-weight:bold'>bc</span>"));

            PageEditResult result = CreateEditor().ApplyTextCase(xml, TextCaseMode.Upper);

            PageDocument page = PageDocument.Parse(result.Xml!);
            Assert.That(PageDocument.GetRunHtml(page.AllTextRuns().Single()), Is.EqualTo("A<span style='font-weight:bold'>BC</span>"));
        }

        [Test]
        public void ApplyMarkdownShortcuts_ConvertsOnlyTheSelectedParagraph()
        {
            string xml = PageXml.Page(PageXml.Oe("# Selected heading", selected: true), PageXml.Oe("# Not selected", selected: false));

            PageEditResult result = CreateEditor().ApplyMarkdownShortcuts(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PlainTexts(result.Xml!), Is.EqualTo(["Selected heading", "# Not selected"]));
            }
        }

        [Test]
        public void ApplyMarkdownAtCaret_ConvertsTheParagraphWithTheCaret()
        {
            string xml = PageXml.Page(PageXml.Oe("# Heading", selected: true), PageXml.Oe("# Another paragraph", selected: false));

            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
                Assert.That(PlainTexts(result.Xml!), Is.EqualTo([string.Empty, "Heading", "# Another paragraph"]));
            }
        }

        [Test]
        public void ApplyMarkdownAtCaret_ChangesAnExistingHeading1ToHeading3()
        {
            string heading1Css = StyleCatalog.CreateDefault().Get(StyleNames.Heading1)!.ToCss();
            string xml = PageXml.Page(PageXml.Oe("### Some text", selected: true, style: heading1Css));

            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading3)));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Does.Not.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
                Assert.That(PlainTexts(result.Xml!), Does.Contain("Some text"));
            }
        }

        [Test]
        public void ApplyMarkdownAtCaret_TakesTheLevelFromTheParagraphNotFromTyping()
        {
            string xml = PageXml.Page(PageXml.Oe("#", selected: false), PageXml.Oe("#", selected: false), PageXml.Oe("# ", selected: true));

            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(xml);

            Assert.That(result.Changed, Is.True);

            List<XElement> outlineElements = [.. PageDocument.Parse(result.Xml!).AllOutlineElements()];
            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)outlineElements[2].Attribute("style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
                Assert.That((string?)outlineElements[0].Attribute("style"), Is.Null);
                Assert.That((string?)outlineElements[1].Attribute("style"), Is.Null);
                Assert.That(PlainTexts(result.Xml!), Is.EqualTo(["#", "#", string.Empty]));
            }
        }

        [Test]
        public void ApplyMarkdownAtCaret_IgnoresAHashInTheMiddleOfTheText()
        {
            string xml = PageXml.Page(PageXml.Oe("The C# language is nice"));

            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(xml);

            Assert.That(result.Changed, Is.False);
        }

        [Test]
        public void ApplyMarkdownAtCaret_LeavesFourHashesAlone()
        {
            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(PageXml.Page(PageXml.Oe("#### Cztery")));

            Assert.That(result.Changed, Is.False);
        }

        [Test]
        public void ApplyMarkdownAtCaret_PutsTheCaretBackAtTheStartOfTheParagraph()
        {
            string xml = PageXml.Page(PageXml.Oe("### Some text"));

            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(xml);

            PageDocument page = PageDocument.Parse(result.Xml!);
            List<XElement> runs = [.. page.AllOutlineElements().Single().Elements(page.One + "T")];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PageDocument.GetRunHtml(runs[0]), Is.Empty);
                Assert.That((string?)runs[0].Attribute("selected"), Is.EqualTo("all"));
                Assert.That(HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(runs[1])), Is.EqualTo("Some text"));
                Assert.That(page.Document.Descendants().Attributes("selected").Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void ApplyMarkdownAtCaret_LeavesASingleEmptyRunWhenTheHeadingHasNoTitleYet()
        {
            string xml = PageXml.Page(PageXml.Oe("## "));

            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(xml);

            PageDocument page = PageDocument.Parse(result.Xml!);
            List<XElement> runs = [.. page.AllOutlineElements().Single().Elements(page.One + "T")];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runs, Has.Count.EqualTo(1));
                Assert.That(PageDocument.GetRunHtml(runs[0]), Is.Empty);
                Assert.That((string?)runs[0].Attribute("selected"), Is.EqualTo("all"));
            }
        }

        [TestCase("# ", TestName = "ApplyMarkdownAtCaret_AsksToFocusTheParagraph_WhenTheHeadingHasNoTitleYet")]
        [TestCase("# Title", TestName = "ApplyMarkdownAtCaret_AsksToFocusTheParagraph_WhenTheHeadingKeepsItsText")]
        public void ApplyMarkdownAtCaret_AsksToFocusTheConvertedParagraph(string text)
        {
            string xml = PageXml.Page(PageXml.Oe(text, objectId: "{OE-1}{12}{B0}"));

            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(result.FocusObjectId, Is.EqualTo("{OE-1}{12}{B0}"));
            }
        }

        [Test]
        public void ApplyMarkdownAtCaret_DoesNothingWhenTheSelectionSpansManyParagraphs()
        {
            string xml = PageXml.Page(PageXml.Oe("# First", selected: true), PageXml.Oe("# Second", selected: true));

            PageEditResult result = CreateEditor().ApplyMarkdownAtCaret(xml);

            Assert.That(result.Changed, Is.False);
        }

        [Test]
        public void ApplyNormalAfterHeading_ConvertsTheEmptyParagraphStartedByEnterInAHeading()
        {
            string headingCss = StyleCatalog.CreateDefault().Get(StyleNames.Heading2)!.ToCss();
            string xml = PageXml.Page(PageXml.Oe("Heading", selected: false, style: headingCss), PageXml.Oe(string.Empty, selected: true, style: headingCss));

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            Assert.That(result.Changed, Is.True);

            List<XElement> outlineElements = [.. PageDocument.Parse(result.Xml!).AllOutlineElements()];
            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)outlineElements[1].Attribute("style"), Is.Null);
                Assert.That((string?)outlineElements[0].Attribute("style"), Is.EqualTo(headingCss));
            }
        }

        [Test]
        public void ApplyNormalAfterHeading_LeavesTheHeadingAloneWhileItStillHasItsText()
        {
            string headingCss = StyleCatalog.CreateDefault().Get(StyleNames.Heading1)!.ToCss();
            string xml = PageXml.Page(PageXml.Oe("Heading", selected: true, style: headingCss, objectId: "{OE-1}{12}{B0}"));

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.False);
                Assert.That(result.CaretObjectId, Is.EqualTo("{OE-1}{12}{B0}"));
            }
        }

        [Test]
        public void ApplyNormalAfterHeading_ConvertsAParagraphTheUserAlreadyStartedTypingIntoAfterEnter()
        {
            string headingCss = StyleCatalog.CreateDefault().Get(StyleNames.Heading1)!.ToCss();
            string xml = PageXml.Page(PageXml.Oe("Heading", selected: false, style: headingCss, objectId: "{OE-1}{12}{B0}"), PageXml.Oe("tex", selected: true, style: headingCss, objectId: "{OE-2}{13}{B0}"));

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml, "{OE-1}{12}{B0}");

            Assert.That(result.Changed, Is.True);

            List<XElement> outlineElements = [.. PageDocument.Parse(result.Xml!).AllOutlineElements()];
            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)outlineElements[1].Attribute("style"), Is.Null);
                Assert.That(PlainTexts(result.Xml!), Does.Contain("tex"));
            }
        }

        [Test]
        public void ApplyNormalAfterHeading_LeavesTheHeadingAloneWhenTheCaretHasNotMovedBetweenLooks()
        {
            string headingCss = StyleCatalog.CreateDefault().Get(StyleNames.Heading1)!.ToCss();
            string xml = PageXml.Page(PageXml.Oe(string.Empty, selected: false, style: headingCss, objectId: "{OE-2}{13}{B0}"), PageXml.Oe("Heading", selected: true, style: headingCss, objectId: "{OE-1}{12}{B0}"));

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml, "{OE-1}{12}{B0}");

            Assert.That(result.Changed, Is.False);
        }

        [Test]
        public void ApplyNormalAfterHeading_LeavesAnEmptyOrdinaryParagraphAlone()
        {
            string xml = PageXml.Page(PageXml.Oe(string.Empty, selected: true));

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            Assert.That(result.Changed, Is.False);
        }

        [Test]
        public void ApplyNormalAfterHeading_RecognisesTheHeadingWhenOneNoteRewritesItsCss()
        {
            StyleDefinition heading = StyleCatalog.CreateDefault().Get(StyleNames.Heading2)!;
            string rewrittenCss = "font-family:'" + heading.Font + "'"
                + ";font-size:\n" + heading.FontSize!.Value.ToString(CultureInfo.InvariantCulture) + "pt"
                + ";color:#276296";

            string xml = PageXml.Page(PageXml.Oe(string.Empty, selected: true, style: rewrittenCss));

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Is.Null);
            }
        }

        [Test]
        public void ApplyNormalAfterHeading_LeavesAnEmptyParagraphWithOrdinaryTextSizeAlone()
        {
            string css = "font-family:Segoe UI;font-size:11.0pt;color:#276296";
            string xml = PageXml.Page(PageXml.Oe(string.Empty, selected: true, style: css));

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            Assert.That(result.Changed, Is.False);
        }

        [Test]
        public void ApplyNormalAfterHeading_IgnoresAHeadingStyleSizedLikeTheBodyTextOfThePage()
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();
            catalog.Styles[StyleNames.Heading3].FontSize = 11.0;

            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"1\" name=\"p\" fontColor=\"automatic\" fontSize=\"11\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE style=\"font-family:Segoe UI;font-size:11.0pt\" selected=\"all\">"
                + "<one:T selected=\"all\"><![CDATA[]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditor editor = new(catalog, SyntaxHighlighterRegistry.CreateDefault());

            Assert.That(editor.ApplyNormalAfterHeading(xml).Changed, Is.False);
        }

        [Test]
        public void ApplyNormalAfterHeading_RecognisesAHeadingMadeWithOneNotesOwnQuickStyle()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"1\" name=\"p\" fontColor=\"automatic\" fontSize=\"11\"/>"
                + "<one:QuickStyleDef index=\"3\" name=\"h1\" fontColor=\"#1E4E79\" fontSize=\"16\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE quickStyleIndex=\"3\" selected=\"all\">"
                + "<one:T selected=\"all\"><![CDATA[]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "quickStyleIndex"), Is.EqualTo("1"));
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "style"), Is.Null);
            }
        }

        [Test]
        public void ApplyNormalAfterHeading_PrefersASiblingsBodyFontOverAnUnrelatedOneDeclaredEarlierInThePage()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"1\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11\"/>"
                + "<one:QuickStyleDef index=\"2\" name=\"h1\" fontColor=\"#1E4E79\" fontSize=\"16\"/>"
                + "<one:QuickStyleDef index=\"3\" name=\"p\" fontColor=\"automatic\" font=\"Segoe UI\" fontSize=\"11\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE quickStyleIndex=\"3\"><one:T><![CDATA[Body text above]]></one:T></one:OE>"
                + "<one:OE quickStyleIndex=\"2\" selected=\"all\">"
                + "<one:T selected=\"all\"><![CDATA[]]></one:T>"
                + "</one:OE>"
                + "<one:OE quickStyleIndex=\"3\"><one:T><![CDATA[Body text below]]></one:T></one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            XElement convertedOe = XDocument.Parse(result.Xml!).Descendants().Where(element => element.Name.LocalName == "OE").ElementAt(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That((string?)convertedOe.Attribute("quickStyleIndex"), Is.EqualTo("3"));
                Assert.That((string?)convertedOe.Attribute("style"), Is.Null);
            }
        }

        [Test]
        public void ApplyNormalAfterHeading_IgnoresAQuickStyleThatIsNotAHeading()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:QuickStyleDef index=\"3\" name=\"p\" fontColor=\"automatic\" fontSize=\"11\"/>"
                + "<one:Outline><one:OEChildren>"
                + "<one:OE quickStyleIndex=\"3\" selected=\"all\">"
                + "<one:T selected=\"all\"><![CDATA[]]></one:T>"
                + "</one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            Assert.That(result.Changed, Is.False);
        }

        [Test]
        public void ApplyNormalAfterHeading_LeavesTheCaretInTheParagraphWithoutAskingForNavigateTo()
        {
            string headingCss = StyleCatalog.CreateDefault().Get(StyleNames.Heading2)!.ToCss();
            string xml = PageXml.Page(PageXml.Oe("Heading", selected: false, style: headingCss), PageXml.Oe(string.Empty, selected: true, style: headingCss, objectId: "{OE-2}{14}{B0}"));

            PageEditResult result = CreateEditor().ApplyNormalAfterHeading(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FocusObjectId, Is.Null);
                Assert.That(result.NudgeCaret, Is.True);
            }

            PageDocument page = PageDocument.Parse(result.Xml!);
            XElement caret = page.AllOutlineElements().Last().Elements(page.One + "T").Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PageDocument.GetRunHtml(caret), Is.Empty);
                Assert.That((string?)caret.Attribute("selected"), Is.EqualTo("all"));
                Assert.That(page.Document.Descendants().Attributes("selected").Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void SetSpellCheck_MarksSelectionWithALanguageThatHasNoDictionary()
        {
            string xml = PageXml.Page(PageXml.Oe("Surname Kowalski"));

            PageEditResult result = CreateEditor().SetSpellCheck(xml, enabled: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "T", "lang"), Is.EqualTo(SpellCheckDisabler.NoSpellCheckLanguage));
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "lang"), Is.EqualTo(SpellCheckDisabler.NoSpellCheckLanguage));
            }
        }

        [Test]
        public void SetSpellCheck_RemovesTheForcedLanguageWhenReEnabled()
        {
            string disabled = CreateEditor().SetSpellCheck(PageXml.Page(PageXml.Oe("x")), enabled: false).Xml!;

            PageEditResult result = CreateEditor().SetSpellCheck(disabled, enabled: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "T", "lang"), Is.Null);
            }
        }

        [Test]
        public void SetSpellCheck_ReportsNoChangeWhenAlreadyDisabled()
        {
            string disabled = CreateEditor().SetSpellCheck(PageXml.Page(PageXml.Oe("x")), enabled: false).Xml!;

            Assert.That(CreateEditor().SetSpellCheck(disabled, enabled: false).Changed, Is.False);
        }

        [Test]
        public void SetSpellCheck_ClearsForcedLanguageWhenOneNoteConsolidatedItOntoTheParagraph()
        {
            string xml = PageXml.Page("<one:OE lang=\"yo\" selected=\"all\"><one:T selected=\"all\"><![CDATA[Surname Kowalski]]></one:T></one:OE>");

            PageEditResult result = CreateEditor().SetSpellCheck(xml, enabled: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(PageXml.AttributeOfFirst(result.Xml!, "OE", "lang"), Is.Null);
            }
        }

        [Test]
        public void SetSpellCheck_ClearsForcedLanguageWhenOneNoteConsolidatedItOntoTheOutline()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:Outline lang=\"yo\"><one:OEChildren>"
                + "<one:OE><one:T><![CDATA[still disabled]]></one:T></one:OE>"
                + "<one:OE selected=\"all\"><one:T selected=\"all\"><![CDATA[Surname Kowalski]]></one:T></one:OE>"
                + "</one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().SetSpellCheck(xml, enabled: true);
            XElement page = XElement.Parse(result.Xml!);
            XNamespace one = PageXml.Namespace;
            List<XElement> outlineElements = [.. page.Descendants(one + "OE")];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True, "The outline-level default was cleared and a sibling was pinned, even though the target paragraph itself carried no lang of its own.");
                Assert.That((string?)page.Descendants(one + "Outline").Single().Attribute("lang"), Is.Null, "The stale outline-level default must not keep overriding every future edit.");
                Assert.That((string?)outlineElements[0].Attribute("lang"), Is.EqualTo(SpellCheckDisabler.NoSpellCheckLanguage), "The untouched sibling must keep its old effective state now that the shared default is gone.");
                Assert.That((string?)outlineElements[1].Attribute("lang"), Is.Null, "The re-enabled paragraph must not still be governed by the outline's old default.");
            }
        }

        [Test]
        public void SetSpellCheck_DoesNotClaimTheSelectionIsMissingWhenEnablingAlreadyEnabledText()
        {
            string xml = PageXml.Page(PageXml.Oe("Surname Kowalski"));

            PageEditResult result = CreateEditor().SetSpellCheck(xml, enabled: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.False);
                Assert.That(result.Message, Does.Not.Contain("Select the text"));
            }
        }

        [Test]
        public void FormatCode_ReplacesSelectionWithAShadedSingleCellTable()
        {
            string xml = PageXml.Page(PageXml.Oe("public class Foo"), PageXml.Oe("{"), PageXml.Oe("}"));

            PageEditResult result = CreateEditor().FormatCode(xml, "csharp");

            Assert.That(result.Changed, Is.True);

            PageDocument page = PageDocument.Parse(result.Xml!);
            XElement table = page.Document.Descendants(page.One + "Table").Single();
            XElement cell = page.Document.Descendants(page.One + "Cell").Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)table.Attribute("bordersVisible"), Is.EqualTo("false"));
                Assert.That((string?)cell.Attribute("shadingColor"), Is.EqualTo("#F7F6F3"));
            }

            XElement cellChildren = cell.Element(page.One + "OEChildren")!;
            Assert.That(cellChildren.Elements(page.One + "OE").Count(), Is.EqualTo(3));
        }

        [Test]
        public void FormatCode_AddsALanguageHeaderRowAboveTheCode()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"));

            PageEditResult result = CreateEditor().FormatCode(xml, "csharp");

            PageDocument page = PageDocument.Parse(result.Xml!);
            XElement table = page.Document.Descendants(page.One + "Table").Single();
            List<XElement> rows = [.. table.Elements(page.One + "Row")];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(2));
                Assert.That((string?)table.Attribute("hasHeaderRow"), Is.EqualTo("true"));
            }

            XElement headerCell = rows[0].Element(page.One + "Cell")!;
            XElement headerLine = headerCell.Element(page.One + "OEChildren")!.Elements(page.One + "OE").Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)headerCell.Attribute("shadingColor"), Is.EqualTo("#EDEBE5"));
                Assert.That(PageDocument.GetRunHtml(headerLine.Element(page.One + "T")), Does.Contain("C#"));
                Assert.That((string?)headerLine.Attribute("style"), Does.Contain("font-weight:bold"));
                Assert.That((string?)headerLine.Attribute("selected"), Is.Null);
            }
        }

        [Test]
        public void FormatCode_ReformattingAnExistingCodeBlockUpdatesTheHeaderInsteadOfDuplicatingIt()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"));
            string firstPass = CreateEditor().FormatCode(xml, "csharp").Xml!;

            PageDocument firstPage = PageDocument.Parse(firstPass);
            foreach (XElement selectedElement in firstPage.Document.Descendants().Where(e => e.Attribute("selected") != null))
            {
                selectedElement.Attributes("selected").Remove();
            }

            XElement lineInsideTable = firstPage.Document.Descendants(firstPage.One + "Cell").Last().Descendants(firstPage.One + "OE").Single();
            lineInsideTable.SetAttributeValue("selected", "all");
            lineInsideTable.Element(firstPage.One + "T")!.SetAttributeValue("selected", "all");

            PageEditResult second = CreateEditor().FormatCode(firstPage.ToXmlString(), "powershell");

            Assert.That(second.Changed, Is.True);

            PageDocument page = PageDocument.Parse(second.Xml!);
            Assert.That(page.Document.Descendants(page.One + "Table").Count(), Is.EqualTo(1));

            List<XElement> rows = [.. page.Document.Descendants(page.One + "Table").Single().Elements(page.One + "Row")];
            Assert.That(rows, Has.Count.EqualTo(2), "Reformatting has to update the header, not add another one.");

            XElement headerLine = rows[0].Element(page.One + "Cell")!.Element(page.One + "OEChildren")!.Elements(page.One + "OE").Single();
            Assert.That(PageDocument.GetRunHtml(headerLine.Element(page.One + "T")), Does.Contain("PowerShell"));

            XElement codeLine = rows[1].Element(page.One + "Cell")!.Element(page.One + "OEChildren")!.Elements(page.One + "OE").Single();
            Assert.That(HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(codeLine.Element(page.One + "T"))), Is.EqualTo("public int x;"));
        }

        [Test]
        public void FormatCode_SelectsEveryLineOfTheNewCodeBlock()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"), PageXml.Oe("public int y;"));

            PageEditResult result = CreateEditor().FormatCode(xml, "csharp");

            PageDocument page = PageDocument.Parse(result.Xml!);
            XElement outerOe = page.Document.Descendants(page.One + "Table").Single().Parent!;
            Assert.That((string?)outerOe.Attribute("selected"), Is.Null);

            List<XElement> lines = [.. page.Document.Descendants(page.One + "Cell").Last().Element(page.One + "OEChildren")!.Elements(page.One + "OE")];

            Assert.That(lines, Has.Count.EqualTo(2));
            foreach (XElement line in lines)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That((string?)line.Attribute("selected"), Is.EqualTo("all"));
                    Assert.That((string?)line.Element(page.One + "T")!.Attribute("selected"), Is.EqualTo("all"));
                }
            }
        }

        [Test]
        public void FormatCode_AppliesMonospaceFontAndTokenColors()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"));

            PageEditResult result = CreateEditor().FormatCode(xml, "csharp");

            PageDocument page = PageDocument.Parse(result.Xml!);
            XElement line = page.Document.Descendants(page.One + "Cell").Last().Element(page.One + "OEChildren")!.Elements(page.One + "OE").Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)line.Attribute("style"), Does.Contain("font-family:Consolas"));
                Assert.That(PageDocument.GetRunHtml(line.Element(page.One + "T")), Does.Contain("color:#0033B3"));
            }
        }

        [Test]
        public void FormatCode_AlwaysUsesTheLightPaletteSoOneNoteCanAdaptIt()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"));

            PageEditResult result = CreateEditor().FormatCode(xml, "csharp");

            PageDocument page = PageDocument.Parse(result.Xml!);
            XElement cell = page.Document.Descendants(page.One + "Cell").Last();

            Assert.That((string?)cell.Attribute("shadingColor"), Is.EqualTo("#F7F6F3"));
        }

        [Test]
        public void FormatCode_DisablesSpellCheckOnEveryLine()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"), PageXml.Oe("public int y;"));

            PageEditResult result = CreateEditor().FormatCode(xml, "csharp");

            PageDocument page = PageDocument.Parse(result.Xml!);
            List<XElement> lines = [.. page.Document.Descendants(page.One + "Cell").Last().Element(page.One + "OEChildren")!.Elements(page.One + "OE")];

            Assert.That(lines, Has.Count.EqualTo(2));
            foreach (XElement line in lines)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That((string?)line.Attribute("lang"), Is.EqualTo("yo"));
                    Assert.That((string?)line.Element(page.One + "T")!.Attribute("lang"), Is.EqualTo("yo"));
                }
            }
        }

        [Test]
        public void FormatCode_DisablesSpellCheckOnTheHeaderRow()
        {
            string xml = PageXml.Page(PageXml.Oe("def x = 1"));

            PageEditResult result = CreateEditor().FormatCode(xml, "groovy");

            XElement headerLine = HeaderLine(result.Xml!);
            PageDocument page = PageDocument.Parse(result.Xml!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)headerLine.Attribute("lang"), Is.EqualTo("yo"));
                Assert.That((string?)headerLine.Element(page.One + "T")!.Attribute("lang"), Is.EqualTo("yo"));
            }
        }

        [Test]
        public void FormatCode_PreservesIndentation()
        {
            string xml = PageXml.Page(PageXml.Oe("    indented();"));

            PageEditResult result = CreateEditor().FormatCode(xml, "csharp");

            PageDocument page = PageDocument.Parse(result.Xml!);
            Assert.That(PageDocument.GetRunHtml(page.AllTextRuns().Last()), Does.StartWith("&nbsp;&nbsp;&nbsp;&nbsp;"));
        }

        [Test]
        public void FormatCode_ReformattingAnExistingCodeBlockReplacesItInsteadOfNesting()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"));
            string firstPass = CreateEditor().FormatCode(xml, "csharp").Xml!;

            PageDocument firstPage = PageDocument.Parse(firstPass);
            foreach (XElement selectedElement in firstPage.Document.Descendants().Where(e => e.Attribute("selected") != null))
            {
                selectedElement.Attributes("selected").Remove();
            }

            XElement lineInsideTable = firstPage.Document.Descendants(firstPage.One + "Cell").Last().Descendants(firstPage.One + "OE").Single();
            lineInsideTable.SetAttributeValue("selected", "all");
            lineInsideTable.Element(firstPage.One + "T")!.SetAttributeValue("selected", "all");

            PageEditResult second = CreateEditor().FormatCode(firstPage.ToXmlString(), "csharp");

            Assert.That(second.Changed, Is.True);

            PageDocument page = PageDocument.Parse(second.Xml!);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(page.Document.Descendants(page.One + "Table").Count(), Is.EqualTo(1));
                Assert.That(page.Document.Descendants(page.One + "Cell").Count(), Is.EqualTo(2));
            }
        }

        [Test]
        public void FormatCode_RejectsUnknownLanguages()
        {
            PageEditResult result = CreateEditor().FormatCode(PageXml.Page(PageXml.Oe("x")), "brainfuck");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.False);
                Assert.That(result.Message, Does.Contain("brainfuck"));
            }
        }

        [Test]
        public void FormatCode_PutsACopyButtonOnTheRightOfTheHeaderRow()
        {
            string xml = PageXml.Page(PageXml.Oe("public int x;"));

            PageEditResult result = CreateEditor().FormatCode(xml, "csharp");

            string header = HeaderHtml(result.Xml!);

            Assert.That(header, Does.StartWith("C#"));
            Assert.That(header, Does.Contain("<a href=\"momtion:copy?id="));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(header, Does.EndWith("Copy</a>"));
                Assert.That(HeaderLineAttribute(result.Xml!, "alignment"), Is.Null);
                Assert.That(CountOccurrences(header, "&nbsp;"), Is.LessThan(6));
            }
        }

        [Test]
        public void FormatCode_GivesEveryBlockItsOwnCopyButtonId()
        {
            string first = CreateEditor().FormatCode(PageXml.Page(PageXml.Oe("int a;")), "csharp").Xml!;
            string second = CreateEditor().FormatCode(PageXml.Page(PageXml.Oe("int b;")), "csharp").Xml!;

            Assert.That(CopyBlockIdOf(first), Is.Not.EqualTo(CopyBlockIdOf(second)));
        }

        [Test]
        public void FormatCode_OmitsTheCopyButtonWhenItIsTurnedOff()
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();
            catalog.CodeBlock.ShowCopyButton = false;
            PageEditor editor = new(catalog, SyntaxHighlighterRegistry.CreateDefault());

            PageEditResult result = editor.FormatCode(PageXml.Page(PageXml.Oe("int a;")), "csharp");

            Assert.That(HeaderHtml(result.Xml!), Is.EqualTo("C#"));
        }

        [Test]
        public void ApplyStyle_DropsTheCodeBlockHeaderInsteadOfTurningItIntoAHeading()
        {
            PageEditor editor = CreateEditor();
            string formatted = editor.FormatCode(PageXml.Page(PageXml.Oe("int x;"), PageXml.Oe("int y;")), "csharp").Xml!;
            string wholeBlockSelected = SelectEveryParagraph(formatted);

            PageEditResult result = editor.ApplyStyle(wholeBlockSelected, StyleNames.Heading1);

            Assert.That(result.Changed, Is.True);

            PageDocument page = PageDocument.Parse(result.Xml!);
            Assert.That(page.Document.Descendants(page.One + "Table"), Is.Empty, "The code block should have stopped being a table.");

            string[] texts = PlainTexts(result.Xml!);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(texts, Is.EqualTo(["int x;", "int y;"]));
                Assert.That(result.Xml, Does.Not.Contain("momtion:copy"));
            }

            Assert.That(result.Xml, Does.Not.Contain("lang=\"" + SpellCheckDisabler.NoSpellCheckLanguage + "\""));
        }

        [Test]
        public void ApplyStyle_KeepsEveryRowOfAnOrdinaryUserTable()
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-03T10:00:00.000Z\">"
                + "<one:Outline><one:OEChildren><one:OE><one:Table><one:Columns>"
                + "<one:Column index=\"0\" width=\"200.0\"/></one:Columns>"
                + "<one:Row><one:Cell><one:OEChildren>" + PageXml.Oe("first") + "</one:OEChildren></one:Cell></one:Row>"
                + "<one:Row><one:Cell><one:OEChildren>" + PageXml.Oe("second") + "</one:OEChildren></one:Cell></one:Row>"
                + "</one:Table></one:OE></one:OEChildren></one:Outline></one:Page>";

            PageEditResult result = CreateEditor().ApplyStyle(xml, StyleNames.Heading1);

            Assert.That(PlainTexts(result.Xml!), Is.EqualTo(["first", "second"]));
        }

        [Test]
        public void ApplyTextCase_LeavesTheCodeBlockHeaderAlone()
        {
            PageEditor editor = CreateEditor();
            string formatted = editor.FormatCode(PageXml.Page(PageXml.Oe("int x;")), "csharp").Xml!;
            string wholeBlockSelected = SelectEveryParagraph(formatted);

            PageEditResult result = editor.ApplyTextCase(wholeBlockSelected, TextCaseMode.Upper);

            Assert.That(HeaderHtml(result.Xml!), Does.StartWith("C#"));
            Assert.That(HeaderHtml(result.Xml!), Does.EndWith("Copy</a>"));

            XElement codeLine = PageDocument.Parse(result.Xml!).AllTextRuns().Last();
            Assert.That(HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(codeLine)), Is.EqualTo("INT X;"));
        }

        [Test]
        public void ExtractCode_ReturnsThePlainSourceOfTheBlockBehindTheButton()
        {
            string xml = PageXml.Page(PageXml.Oe("public class Foo"), PageXml.Oe("{"), PageXml.Oe("    if (a &lt; b &amp;&amp; c) return;"), PageXml.Oe("}"));

            PageEditor editor = CreateEditor();
            string formatted = editor.FormatCode(xml, "csharp").Xml!;

            CodeExtractResult copied = editor.ExtractCodeByBlockId(formatted, CopyBlockIdOf(formatted));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(copied.Success, Is.True);
                Assert.That(copied.LineCount, Is.EqualTo(4));
                Assert.That(copied.Text, Is.EqualTo("public class Foo\n{\n    if (a < b && c) return;\n}"));
            }
        }

        [Test]
        public void ExtractCode_LeavesTheLanguageHeaderOutOfTheCopiedText()
        {
            PageEditor editor = CreateEditor();
            string formatted = editor.FormatCode(PageXml.Page(PageXml.Oe("int x;")), "csharp").Xml!;

            CodeExtractResult copied = editor.ExtractCodeByBlockId(formatted, CopyBlockIdOf(formatted));

            Assert.That(copied.Text, Is.EqualTo("int x;"));
            Assert.That(copied.Text, Does.Not.Contain("Copy"));
            Assert.That(copied.Text, Does.Not.Contain("C#"));
        }

        [Test]
        public void ExtractCode_JoinsEveryRunOfALineEditedInPlace()
        {
            PageEditor editor = CreateEditor();
            string formatted = editor.FormatCode(PageXml.Page(PageXml.Oe("int x;")), "csharp").Xml!;

            PageDocument page = PageDocument.Parse(formatted);
            XElement codeLine = page.Document.Descendants(page.One + "Cell").Last().Descendants(page.One + "OE").Single();
            codeLine.Add(new XElement(page.One + "T", new XCData("&nbsp;// appended")));

            CodeExtractResult copied = editor.ExtractCodeByBlockId(page.ToXmlString(), CopyBlockIdOf(formatted));

            Assert.That(copied.Text, Is.EqualTo("int x; // appended"));
        }

        [Test]
        public void ExtractCode_CopiesTheBlockThatOwnsTheButtonWhenThePageHasSeveral()
        {
            PageEditor editor = CreateEditor();
            string firstBlock = editor.FormatCode(PageXml.Page(PageXml.Oe("int first;")), "csharp").Xml!;
            string secondBlock = editor.FormatCode(PageXml.Page(PageXml.Oe("int second;")), "csharp").Xml!;

            PageDocument page = PageDocument.Parse(firstBlock);
            XElement children = page.Document.Descendants(page.One + "OEChildren").First();
            children.Add(PageDocument.Parse(secondBlock).Document.Descendants(page.One + "Table").Single().Parent);

            CodeExtractResult copied = editor.ExtractCodeByBlockId(page.ToXmlString(), CopyBlockIdOf(secondBlock));

            Assert.That(copied.Text, Is.EqualTo("int second;"));
        }

        [Test]
        public void ExtractCode_ReportsAButtonWhoseBlockIsGone()
        {
            PageEditor editor = CreateEditor();
            string formatted = editor.FormatCode(PageXml.Page(PageXml.Oe("int x;")), "csharp").Xml!;

            CodeExtractResult copied = editor.ExtractCodeByBlockId(formatted, "deadbeef");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(copied.Success, Is.False);
                Assert.That(copied.Text, Is.Null);
                Assert.That(copied.Message, Does.Contain("No code block"));
            }
        }

        [Test]
        public void ExtractCode_RejectsEmptyInput()
        {
            PageEditor editor = CreateEditor();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(editor.ExtractCodeByBlockId(PageXml.Page(PageXml.Oe("x")), string.Empty).Success, Is.False);
                Assert.That(editor.ExtractCodeByBlockId("<not-onenote/>", "abc").Success, Is.False);
            }
        }

        private static string SelectEveryParagraph(string pageXml)
        {
            PageDocument page = PageDocument.Parse(pageXml);

            foreach (XElement outlineElement in page.AllOutlineElements())
            {
                if (outlineElement.Element(page.One + "T") == null)
                {
                    continue;
                }

                outlineElement.SetAttributeValue("selected", "all");
                foreach (XElement run in outlineElement.Elements(page.One + "T"))
                {
                    run.SetAttributeValue("selected", "all");
                }
            }

            return page.ToXmlString();
        }

        private static XElement HeaderLine(string pageXml)
        {
            PageDocument page = PageDocument.Parse(pageXml);
            XElement headerCell = page.Document.Descendants(page.One + "Cell").First();
            return headerCell.Element(page.One + "OEChildren")!.Elements(page.One + "OE").Single();
        }

        private static string HeaderHtml(string pageXml)
        {
            XElement headerLine = HeaderLine(pageXml);
            return PageDocument.GetRunHtml(headerLine.Element(headerLine.Name.Namespace + "T"));
        }

        private static string? HeaderLineAttribute(string pageXml, string attributeName)
        {
            return (string?)HeaderLine(pageXml).Attribute(attributeName);
        }

        private static string CopyBlockIdOf(string pageXml)
        {
            const string marker = "momtion:copy?id=";
            string header = HeaderHtml(pageXml);

            int start = header.IndexOf(marker, System.StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "The block header holds no copy link.");

            start += marker.Length;
            int end = header.IndexOf('"', start);
            return header[start..end];
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        [Test]
        public void Operations_ReportNoChangeForUnreadableInput()
        {
            PageEditor editor = CreateEditor();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(editor.ApplyStyle("<not-onenote/>", StyleNames.Heading1).Changed, Is.False);
                Assert.That(editor.ApplyTextCase(string.Empty, TextCaseMode.Upper).Changed, Is.False);
                Assert.That(editor.ApplyMarkdownShortcuts("garbage").Changed, Is.False);
            }
        }

        [Test]
        public void Operations_DoNothingWhenThePageCarriesNoSelectionMarkers()
        {
            string xml = PageXml.Page(PageXml.Oe("first", selected: false), PageXml.Oe("# second", selected: false));

            PageEditor editor = CreateEditor();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(editor.ApplyStyle(xml, StyleNames.Heading1).Changed, Is.False);
                Assert.That(editor.ApplyTextCase(xml, TextCaseMode.Upper).Changed, Is.False);
                Assert.That(editor.ApplyMarkdownShortcuts(xml).Changed, Is.False);
                Assert.That(editor.FormatCode(xml, "csharp").Changed, Is.False);
                Assert.That(editor.SetSpellCheck(xml, enabled: false).Changed, Is.False);
            }
        }

        [Test]
        public void AvailableLanguages_ExposesIdentifiersWithDisplayNames()
        {
            IReadOnlyList<KeyValuePair<string, string>> languages = CreateEditor().AvailableLanguages();

            Assert.That(languages, Has.Count.EqualTo(7));
            Assert.That(languages, Has.Some.Matches<System.Collections.Generic.KeyValuePair<string, string>>(language => language.Key == "csharp" && language.Value == "C#"));
        }

        private static string[] PlainTexts(string pageXml)
        {
            PageDocument page = PageDocument.Parse(pageXml);
            return [.. page.AllTextRuns().Select(run => HtmlRunRewriter.ExtractPlainText(PageDocument.GetRunHtml(run)))];
        }
    }
}