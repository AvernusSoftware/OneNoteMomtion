using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Core.Reformat;
using OneNoteMomtion.Core.Styles;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class ReformatPassesTests
    {
        private static readonly XNamespace One = PageXml.Namespace;

        private static PageEditor CreateEditor()
        {
            return new PageEditor(StyleCatalog.CreateDefault(), SyntaxHighlighterRegistry.CreateDefault());
        }

        [Test]
        public void MonospaceFonts_ReadsTheFirstNameOfAFontStack()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(MonospaceFonts.IsMonospace("Consolas"), Is.True);
                Assert.That(MonospaceFonts.IsMonospace("'Courier New', monospace"), Is.True);
                Assert.That(MonospaceFonts.IsMonospace("Segoe UI, monospace"), Is.False);
                Assert.That(MonospaceFonts.IsMonospace(null), Is.False);
            }
        }

        [Test]
        public void EmptyParagraphCollapser_NeverRemovesAParagraphHoldingSomethingOtherThanText()
        {
            string tableParagraph = "<one:OE><one:Table><one:Columns><one:Column index=\"0\" width=\"100.0\" /></one:Columns><one:Row><one:Cell><one:OEChildren><one:OE><one:T><![CDATA[cell]]></one:T></one:OE></one:OEChildren></one:Cell></one:Row></one:Table></one:OE>";

            string xml = PageXml.Page(PageXml.StyledOe(string.Empty), tableParagraph, PageXml.StyledOe(string.Empty));

            PageEditResult result = CreateEditor().ReformatPage(xml);
            string xmlAfter = result.Changed ? result.Xml! : xml;

            Assert.That(XDocument.Parse(xmlAfter).Descendants(One + "Table").Count(), Is.EqualTo(1));
        }

        [Test]
        public void ParagraphTextCleaner_CollapsesADoubledSpaceSplitAcrossTwoRuns()
        {
            string xml = PageXml.Page(PageXml.StyledOe(["first ", " second"]));

            PageEditResult result = CreateEditor().ReformatPage(xml);
            string[] runs = [.. XDocument.Parse(result.Xml!).Descendants(One + "T").Select(run => run.FirstNode is XCData data ? data.Value : run.Value)];

            Assert.That(string.Concat(runs), Is.EqualTo("first second"));
        }

        [Test]
        public void ReformatCounters_NamesOnlyTheThingsItActuallyDid()
        {
            ReformatCounters counters = new() { Headings = 2, CodeBlocks = 1 };

            Assert.That(counters.Describe(), Is.EqualTo("Reformatted 2 headings, 1 code block."));
        }

        [Test]
        public void ReformatCounters_SaysSoWhenThereWasNothingToDo()
        {
            Assert.That(new ReformatCounters().Describe(), Is.EqualTo("Nothing on this page needed reformatting."));
        }

        [Test]
        public void ReformatOptions_LaysOneNotesDefaultFontUnderTheConfiguredOne()
        {
            StyleCatalog catalog = StyleCatalog.CreateDefault();
            catalog.BodyText = new BodyTextOptions { FontSize = 12.0 };

            ReformatOptions options = ReformatOptions.FromCatalog(catalog, new BodyTextOptions { Font = "Segoe UI", FontSize = 11.0, FontColor = "#1F2328" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.BodyText.Font, Is.EqualTo("Segoe UI"));
                Assert.That(options.BodyText.FontSize, Is.EqualTo(12.0));
                Assert.That(options.BodyText.FontColor, Is.EqualTo("#1F2328"));
            }
        }

        [Test]
        public void ReformatPage_CanBeToldToLeaveCodeBlocksAlone()
        {
            string css = "font-family:Courier New;font-size:11.0pt";
            string xml = PageXml.Page(PageXml.StyledOe("select 1", css), PageXml.StyledOe("from dual", css));

            ReformatOptions options = new();
            options.CodeDetection.ConvertForeignBlocks = false;

            PageEditResult result = CreateEditor().ReformatPage(xml, options);
            string xmlAfter = result.Changed ? result.Xml! : xml;

            Assert.That(XDocument.Parse(xmlAfter).Descendants(One + "Table"), Is.Empty);
        }

        [Test]
        public void ReformatPage_CanBeToldToLeaveTheTextAlone()
        {
            ReformatOptions options = new()
            {
                TextCleanup = new Text.TextCleanupOptions
                {
                    TrimTrailing = false,
                    CollapseSpaces = false,
                    NormalizeListMarkers = false,
                    SpaceAroundBrackets = false,
                    CollapseEmptyParagraphs = false,
                    CleanPastedFormatting = false,
                    NormalizeNonBreakingSpaces = false,
                    NormalizeDashes = false,
                    NormalizeQuotes = false,
                },
            };

            PageEditResult result = CreateEditor().ReformatPage(PageXml.Page(PageXml.StyledOe("double  space  and(brackets)")), options);

            Assert.That(result.Changed, Is.False);
        }
    }
}
