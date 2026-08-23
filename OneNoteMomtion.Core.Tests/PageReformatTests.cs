using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Reformat;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class PageReformatTests
    {
        private static readonly XNamespace One = PageXml.Namespace;

        private static PageEditor CreateEditor()
        {
            return new PageEditor(StyleCatalog.CreateDefault(), SyntaxHighlighterRegistry.CreateDefault());
        }

        private static string HeadingQuickStyles()
        {
            return PageXml.QuickStyle(0, "h1", "Calibri Light", 16.0)
                + PageXml.QuickStyle(1, "h2", "Calibri Light", 14.0)
                + PageXml.QuickStyle(2, "h3", "Calibri Light", 12.0)
                + PageXml.QuickStyle(3, "h4", "Calibri Light", 11.5)
                + PageXml.QuickStyle(4, "p", "Calibri", 11.0);
        }

        private static string[] RunTexts(string xml)
        {
            return [.. XDocument.Parse(xml).Descendants(One + "T").Select(run => run.FirstNode is XCData data ? data.Value : run.Value)];
        }

        private static XElement[] Paragraphs(string xml)
        {
            return [.. XDocument.Parse(xml).Descendants(One + "OE")];
        }

        [Test]
        public void ReformatPage_WorksOnEveryParagraphAndNotOnlyTheSelectedOne()
        {
            string xml = PageXml.Page(PageXml.Oe("selected  text", selected: true), PageXml.Oe("unselected  text", selected: false));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(RunTexts(result.Xml!), Is.EqualTo(new[] { "selected text", "unselected text" }));
            }
        }

        [Test]
        public void ReformatPage_TurnsOneNotesOwnHeadingsIntoTheConfiguredOnes()
        {
            string xml = PageXml.PageWithQuickStyles(
                HeadingQuickStyles(),
                PageXml.StyledOe("First", quickStyleIndex: 0),
                PageXml.StyledOe("Second", quickStyleIndex: 1),
                PageXml.StyledOe("Third", quickStyleIndex: 2));

            PageEditResult result = CreateEditor().ReformatPage(xml);
            XElement[] paragraphs = Paragraphs(result.Xml!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)paragraphs[0].Attribute("style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading1)));
                Assert.That((string?)paragraphs[1].Attribute("style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading2)));
                Assert.That((string?)paragraphs[2].Attribute("style"), Does.Contain(PageXml.FontSizeOf(StyleNames.Heading3)));
            }
        }

        [Test]
        public void ReformatPage_LeavesHeadingLevelsWeDoNotCarryExactlyAsTheyWere()
        {
            string xml = PageXml.PageWithQuickStyles(HeadingQuickStyles(), PageXml.StyledOe("Fourth level", quickStyleIndex: 3));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.False);
                Assert.That(result.Message, Does.Contain("Nothing"));
            }
        }

        [Test]
        public void ReformatPage_KeepsBoldAndLinksWhileTidyingTheWhitespaceAroundThem()
        {
            string html = "start  <b>bold</b>  and <a href=\"https://example.com\">link</a>  ";
            string xml = PageXml.Page(PageXml.StyledOe(html));

            PageEditResult result = CreateEditor().ReformatPage(xml);
            string rewritten = RunTexts(result.Xml!)[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rewritten, Does.Contain("<b>bold</b>"));
                Assert.That(rewritten, Does.Contain("<a href=\"https://example.com\">link</a>"));
                Assert.That(rewritten, Does.Not.Contain("  "));
                Assert.That(rewritten, Does.EndWith("</a>"));
            }
        }

        [Test]
        public void ReformatPage_NormalisesTheGapAfterATypedBullet()
        {
            string xml = PageXml.Page(
                PageXml.StyledOe("*&nbsp;&nbsp;&nbsp;&nbsp;first"),
                PageXml.StyledOe("1.   second"));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            Assert.That(RunTexts(result.Xml!), Is.EqualTo(new[] { "* first", "1. second" }));
        }

        [Test]
        public void ReformatPage_PutsSpaceAroundBracketsPressedAgainstWords()
        {
            string xml = PageXml.Page(PageXml.StyledOe("tekst(uwaga)dalej"));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            Assert.That(RunTexts(result.Xml!), Is.EqualTo(new[] { "tekst (uwaga) dalej" }));
        }

        [Test]
        public void ReformatPage_LeavesBracketsInsideACodeBlockAlone()
        {
            PageEditor editor = CreateEditor();
            string source = PageXml.Page(PageXml.Oe("void Run(int x)"), PageXml.Oe("    Foo(x);"));

            PageEditResult formatted = editor.FormatCode(source, "csharp");
            PageEditResult reformatted = editor.ReformatPage(formatted.Xml!);

            Assert.That(reformatted.Changed, Is.False);
        }

        [Test]
        public void ReformatPage_CollapsesARunOfBlankParagraphsIntoOne()
        {
            string xml = PageXml.Page(
                PageXml.StyledOe("before"),
                PageXml.StyledOe(string.Empty),
                PageXml.StyledOe(string.Empty),
                PageXml.StyledOe(string.Empty),
                PageXml.StyledOe("after"));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            Assert.That(RunTexts(result.Xml!), Is.EqualTo(new[] { "before", string.Empty, "after" }));
        }

        [Test]
        public void ReformatPage_RemovesEveryBlankParagraphBeforeAHeading()
        {
            string xml = PageXml.PageWithQuickStyles(
                HeadingQuickStyles(),
                PageXml.StyledOe("before", quickStyleIndex: 4),
                PageXml.StyledOe(string.Empty, quickStyleIndex: 4),
                PageXml.StyledOe(string.Empty, quickStyleIndex: 4),
                PageXml.StyledOe("Heading", quickStyleIndex: 0),
                PageXml.StyledOe("after", quickStyleIndex: 4));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            Assert.That(RunTexts(result.Xml!), Is.EqualTo(new[] { "before", "Heading", "after" }));
        }

        [Test]
        public void ReformatPage_RemovesEveryBlankParagraphAfterAHeadingToo()
        {
            string xml = PageXml.PageWithQuickStyles(
                HeadingQuickStyles(),
                PageXml.StyledOe("Heading", quickStyleIndex: 0),
                PageXml.StyledOe(string.Empty, quickStyleIndex: 4),
                PageXml.StyledOe(string.Empty, quickStyleIndex: 4),
                PageXml.StyledOe("after", quickStyleIndex: 4));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            Assert.That(RunTexts(result.Xml!), Is.EqualTo(new[] { "Heading", "after" }));
        }

        [Test]
        public void ReformatPage_HangsTheTextUnderABlankLineBeforeAHeadingOffTheParagraphAboveIt()
        {
            // The empty line right above a heading is the one the reader complains about. Its
            // indented block moves onto the paragraph above, which leaves the block where it was.
            string carrier = "<one:OE quickStyleIndex=\"4\"><one:T><![CDATA[]]></one:T><one:OEChildren>"
                + PageXml.StyledOe("indented", quickStyleIndex: 4)
                + "</one:OEChildren></one:OE>";

            string xml = PageXml.PageWithQuickStyles(
                HeadingQuickStyles(),
                PageXml.StyledOe("before", quickStyleIndex: 4),
                carrier,
                PageXml.StyledOe("Heading", quickStyleIndex: 0));

            PageEditResult result = CreateEditor().ReformatPage(xml);
            XElement[] paragraphs = Paragraphs(result.Xml!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RunTexts(result.Xml!), Is.EqualTo(new[] { "before", "indented", "Heading" }));
                Assert.That(paragraphs[0].Element(One + "OEChildren")!.Elements(One + "OE").Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void ReformatPage_HangsTheTextUnderABlankLineOffTheHeadingAboveIt()
        {
            // Enter after a heading and then Tab leaves an empty paragraph holding the list. The
            // list keeps its indentation - it just hangs off the heading instead of off a blank.
            string carrier = "<one:OE quickStyleIndex=\"4\"><one:T><![CDATA[]]></one:T><one:OEChildren>"
                + PageXml.StyledOe("indented", quickStyleIndex: 4)
                + "</one:OEChildren></one:OE>";

            string xml = PageXml.PageWithQuickStyles(HeadingQuickStyles(), PageXml.StyledOe("Heading", quickStyleIndex: 0), carrier);

            PageEditResult result = CreateEditor().ReformatPage(xml);
            XElement heading = Paragraphs(result.Xml!)[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RunTexts(result.Xml!), Is.EqualTo(new[] { "Heading", "indented" }));
                Assert.That(heading.Element(One + "OEChildren")!.Elements(One + "OE").Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void ReformatPage_LeavesTheIndentationInsideACodeBlockAlone()
        {
            PageEditor editor = CreateEditor();
            string source = PageXml.Page(PageXml.Oe("public void Run()"), PageXml.Oe("    Console.WriteLine();"));

            PageEditResult formatted = editor.FormatCode(source, "csharp");
            PageEditResult reformatted = editor.ReformatPage(formatted.Xml!);

            string xml = reformatted.Changed ? reformatted.Xml! : formatted.Xml!;

            Assert.That(RunTexts(xml).Any(text => text.Contains("&nbsp;&nbsp;&nbsp;&nbsp;")), Is.True);
        }

        [Test]
        public void ReformatPage_LeavesAnUpToDateCodeBlockUntouched()
        {
            PageEditor editor = CreateEditor();
            string source = PageXml.Page(PageXml.Oe("public void Run()"), PageXml.Oe("    return;"));

            PageEditResult formatted = editor.FormatCode(source, "csharp");
            PageEditResult reformatted = editor.ReformatPage(formatted.Xml!);

            // Nothing about the block changed, so its copy button keeps working.
            Assert.That(reformatted.Changed, Is.False);
        }

        [Test]
        public void ReformatPage_NeverTouchesThePageTitle()
        {
            string xml = PageXml.PageWithTitle("My  „messy”   title  ", PageXml.StyledOe("body  text"));

            PageEditResult result = CreateEditor().ReformatPage(xml);
            XDocument document = XDocument.Parse(result.Xml!);
            XElement title = document.Descendants(One + "Title").Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(title.Descendants(One + "T").Single().Value, Is.EqualTo("My  „messy”   title  "));
                Assert.That(title.Descendants(One + "OE").Single().Attribute("style"), Is.Null);
                Assert.That(RunTexts(result.Xml!), Does.Contain("body text"));
            }
        }

        [Test]
        public void ReformatPage_LeavesATitleThatIsTheOnlyUntidyThingAlone()
        {
            string xml = PageXml.PageWithTitle("Title  with  spaces", PageXml.StyledOe("clean text"));

            Assert.That(CreateEditor().ReformatPage(xml).Changed, Is.False);
        }

        [Test]
        public void ReformatPage_RecoversTheLanguageFromTheCopyLinkWhenTheHeaderShowsNoName()
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();
            catalog.CodeBlock.ShowLanguageHeader = false;

            PageEditor editor = new(catalog, SyntaxHighlighterRegistry.CreateDefault());
            string source = PageXml.Page(PageXml.Oe("select id"), PageXml.Oe("from customers"));

            PageEditResult formatted = editor.FormatCode(source, "sql");
            PageEditResult reformatted = editor.ReformatPage(formatted.Xml!);

            Assert.That(reformatted.Changed, Is.False);
        }

        [Test]
        public void ReformatPage_RefreshesAnExistingBlockAgainstTheCurrentTheme()
        {
            PageEditor editor = CreateEditor();
            string source = PageXml.Page(PageXml.Oe("select id"), PageXml.Oe("from customers"));
            PageEditResult formatted = editor.FormatCode(source, "sql");

            StyleCatalog recoloured = StyleCatalog.CreateDefault();
            recoloured.HighlightTheme["Keyword"] = "#FF00FF";

            PageEditResult reformatted = new PageEditor(recoloured, SyntaxHighlighterRegistry.CreateDefault()).ReformatPage(formatted.Xml!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reformatted.Changed, Is.True);
                Assert.That(RunTexts(reformatted.Xml!).Any(text => text.Contains("#FF00FF")), Is.True);
            }
        }

        [Test]
        public void ReformatPage_KeepsTheCopyButtonIdWhenItRefreshesABlock()
        {
            PageEditor editor = CreateEditor();
            string source = PageXml.Page(PageXml.Oe("select id"), PageXml.Oe("from customers"));
            PageEditResult formatted = editor.FormatCode(source, "sql");

            string blockId = BlockIdOf(formatted.Xml!);

            StyleCatalog recoloured = StyleCatalog.CreateDefault();
            recoloured.HighlightTheme["Keyword"] = "#FF00FF";

            PageEditResult reformatted = new PageEditor(recoloured, SyntaxHighlighterRegistry.CreateDefault()).ReformatPage(formatted.Xml!);

            Assert.That(BlockIdOf(reformatted.Xml!), Is.EqualTo(blockId));
        }

        private static string BlockIdOf(string xml)
        {
            string header = RunTexts(xml).First(text => CodeBlockLink.RunContainsCopyLink(text));

            Assert.That(CodeBlockLink.TryGetRunLink(header, out string blockId, out _), Is.True);

            return blockId;
        }

        [Test]
        public void ReformatPage_AdoptsAMonospacedRunOfParagraphsAsACodeBlock()
        {
            string css = "font-family:Courier New;font-size:11.0pt";
            string xml = PageXml.Page(
                PageXml.StyledOe("select id, name", css),
                PageXml.StyledOe("from customers", css),
                PageXml.StyledOe("where active = 1", css));

            PageEditResult result = CreateEditor().ReformatPage(xml);
            XDocument document = XDocument.Parse(result.Xml!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That(document.Descendants(One + "Table").Count(), Is.EqualTo(1));
                Assert.That(RunTexts(result.Xml!).Any(text => text.Contains("SQL")), Is.True);
            }
        }

        [Test]
        public void ReformatPage_LeavesASingleMonospacedLineAsProse()
        {
            string xml = PageXml.Page(
                PageXml.StyledOe("Call this:"),
                PageXml.StyledOe("dotnet build", "font-family:Consolas;font-size:11.0pt"),
                PageXml.StyledOe("and wait."));

            PageEditResult result = CreateEditor().ReformatPage(xml);
            string xmlAfter = result.Changed ? result.Xml! : xml;

            Assert.That(XDocument.Parse(xmlAfter).Descendants(One + "Table"), Is.Empty);
        }

        [Test]
        public void ReformatPage_PutsTheConfiguredBodyFontOnOrdinaryParagraphs()
        {
            string xml = PageXml.PageWithQuickStyles(HeadingQuickStyles(), PageXml.StyledOe("body text", quickStyleIndex: 4));

            ReformatOptions options = new() { BodyText = new BodyTextOptions { Font = "Segoe UI", FontSize = 11.0 } };
            PageEditResult result = CreateEditor().ReformatPage(xml, options);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.True);
                Assert.That((string?)Paragraphs(result.Xml!)[0].Attribute("style"), Is.EqualTo("font-family:Segoe UI;font-size:11.0pt"));
            }
        }

        [Test]
        public void ReformatPage_LeavesAParagraphThatAlreadyReadsThatWayAlone()
        {
            string xml = PageXml.PageWithQuickStyles(HeadingQuickStyles(), PageXml.StyledOe("body text", quickStyleIndex: 4));

            ReformatOptions options = new() { BodyText = new BodyTextOptions { Font = "Calibri", FontSize = 11.0 } };
            PageEditResult result = CreateEditor().ReformatPage(xml, options);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Changed, Is.False);
                Assert.That(PageXml.AttributeOfFirst(xml, "OE", "style"), Is.Null);
            }
        }

        [Test]
        public void ReformatPage_StripsThePageBackgroundAPasteBroughtAlong()
        {
            string xml = PageXml.Page(PageXml.StyledOe("<span style='background-color:#FFFFFF'>pasted</span>"));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            Assert.That(RunTexts(result.Xml!)[0], Is.EqualTo("pasted"));
        }

        [Test]
        public void ReformatPage_ReportsWhatItDid()
        {
            string xml = PageXml.Page(PageXml.StyledOe("double  space"));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            Assert.That(result.Message, Is.EqualTo("Reformatted 1 text fix."));
        }

        [Test]
        public void ReformatPage_NeverAsksForACaretMoveSoItStaysOneUndoStep()
        {
            string xml = PageXml.Page(PageXml.StyledOe("double  space"));

            PageEditResult result = CreateEditor().ReformatPage(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FocusObjectId, Is.Null);
                Assert.That(result.NudgeCaret, Is.False);
            }
        }

        [Test]
        public void ReformatPage_ChangesNothingTheSecondTimeRound()
        {
            string css = "font-family:Courier New;font-size:11.0pt";
            string xml = PageXml.PageWithQuickStyles(
                HeadingQuickStyles(),
                PageXml.StyledOe("Heading", quickStyleIndex: 0),
                PageXml.StyledOe("body  text with   spaces  ", quickStyleIndex: 4),
                PageXml.StyledOe(string.Empty),
                PageXml.StyledOe(string.Empty),
                PageXml.StyledOe("*    bullet", quickStyleIndex: 4),
                PageXml.StyledOe("select 1", css),
                PageXml.StyledOe("from dual", css));

            PageEditor editor = CreateEditor();
            PageEditResult first = editor.ReformatPage(xml);
            PageEditResult second = editor.ReformatPage(first.Xml!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.Changed, Is.True);
                Assert.That(second.Changed, Is.False);
            }
        }
    }
}
