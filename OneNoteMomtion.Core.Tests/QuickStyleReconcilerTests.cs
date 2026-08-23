using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class QuickStyleReconcilerTests
    {
        private static StyleCatalog Catalog => StyleCatalog.CreateDefault();

        private static PageDocument PageWith(string quickStyleDefs, string outlineElements)
        {
            string xml = "<?xml version=\"1.0\"?>"
                + "<one:Page xmlns:one=\"" + PageXml.Namespace + "\" "
                + "ID=\"{11111111-1111-1111-1111-111111111111}{1}{B0}\" "
                + "lastModifiedTime=\"2026-08-14T10:00:00.000Z\">"
                + quickStyleDefs
                + "<one:Outline><one:OEChildren>"
                + outlineElements
                + "</one:OEChildren></one:Outline></one:Page>";

            return PageDocument.Parse(xml);
        }

        private static XElement OutlineElementAt(PageDocument page, int index)
        {
            return page.AllOutlineElements().ElementAt(index);
        }

        private static XElement DefinitionOf(PageDocument page, XElement outlineElement)
        {
            return page.FindQuickStyleByIndex((string?)outlineElement.Attribute("quickStyleIndex"))!;
        }

        [Test]
        public void Reconcile_GivesAParagraphADefinitionThatAgreesWithTheFontItAlreadyShows()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\" style=\"font-family:'Segoe UI';font-size:11.0pt\"><one:T><![CDATA[body text]]></one:T></one:OE>");

            int reconciled = QuickStyleReconciler.Reconcile(page, Catalog);
            XElement outlineElement = OutlineElementAt(page, 0);
            XElement definition = DefinitionOf(page, outlineElement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reconciled, Is.EqualTo(1));
                Assert.That((string?)definition.Attribute("font"), Is.EqualTo("Segoe UI"));
                Assert.That((string?)definition.Attribute("fontSize"), Is.EqualTo("11.0"));
                Assert.That((string?)outlineElement.Attribute("style"), Is.EqualTo("font-family:'Segoe UI';font-size:11.0pt"), "The paragraph keeps saying what it always said.");
            }
        }

        [Test]
        public void Reconcile_LeavesTheDefinitionOtherParagraphsShareUntouched()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\" style=\"font-family:'Segoe UI';font-size:11.0pt\"><one:T><![CDATA[restyled]]></one:T></one:OE>"
                + "<one:OE quickStyleIndex=\"2\"><one:T><![CDATA[bystander]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);

            XElement restyled = OutlineElementAt(page, 0);
            XElement bystander = OutlineElementAt(page, 1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)bystander.Attribute("quickStyleIndex"), Is.EqualTo("2"));
                Assert.That((string?)DefinitionOf(page, bystander).Attribute("font"), Is.EqualTo("Calibri"));
                Assert.That((string?)restyled.Attribute("quickStyleIndex"), Is.Not.EqualTo("2"));
                Assert.That((string?)DefinitionOf(page, restyled).Attribute("font"), Is.EqualTo("Segoe UI"));
            }
        }

        [Test]
        public void Reconcile_ReusesADefinitionThatAlreadySaysTheSameThing()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>"
                + "<one:QuickStyleDef index=\"4\" name=\"p\" fontColor=\"automatic\" font=\"Segoe UI\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\" style=\"font-family:'Segoe UI';font-size:11.0pt\"><one:T><![CDATA[body text]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)OutlineElementAt(page, 0).Attribute("quickStyleIndex"), Is.EqualTo("4"));
                Assert.That(page.QuickStyleDefinitions(), Has.Count.EqualTo(2), "No second definition for a font the page already describes.");
            }
        }

        [Test]
        public void Reconcile_CarriesOverWhatTheOldDefinitionSaidAndTheParagraphDidNot()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"h2\" fontColor=\"#1E4E79\" font=\"Calibri\" fontSize=\"14.0\" bold=\"true\"/>",
                "<one:OE quickStyleIndex=\"2\" style=\"font-family:'Segoe UI'\"><one:T><![CDATA[heading]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);
            XElement definition = DefinitionOf(page, OutlineElementAt(page, 0));

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)definition.Attribute("font"), Is.EqualTo("Segoe UI"), "The paragraph's own font wins.");
                Assert.That((string?)definition.Attribute("fontSize"), Is.EqualTo("14.0"));
                Assert.That((string?)definition.Attribute("fontColor"), Is.EqualTo("#1E4E79"));
                Assert.That((string?)definition.Attribute("bold"), Is.EqualTo("true"));
                Assert.That((string?)definition.Attribute("name"), Is.EqualTo("h2"), "A heading stays a heading to OneNote.");
            }
        }

        [Test]
        public void Reconcile_BuildsADefinitionForAParagraphThatHasNoQuickStyleAtAll()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"1\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE spaceBefore=\"10.0\" style=\"font-family:Segoe UI;font-size:13.5pt;color:#2E75B5\"><one:T><![CDATA[our heading]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);
            XElement outlineElement = OutlineElementAt(page, 0);
            XElement definition = DefinitionOf(page, outlineElement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string?)definition.Attribute("font"), Is.EqualTo("Segoe UI"));
                Assert.That((string?)definition.Attribute("fontSize"), Is.EqualTo("13.5"));
                Assert.That((string?)definition.Attribute("fontColor"), Is.EqualTo("#2E75B5"));
                Assert.That((string?)definition.Attribute("spaceBefore"), Is.EqualTo("0.0"), "Spacing stays the paragraph's own business.");
                Assert.That((string?)outlineElement.Attribute("spaceBefore"), Is.EqualTo("10.0"));
                Assert.That((string?)definition.Attribute("name"), Is.EqualTo("h2"), "Naming it after the body text would put a heading-sized definition in the way of the normal text command.");
            }
        }

        [Test]
        public void Reconcile_NamesADefinitionAfterTheBodyTextWhenTheFontIsNoHeadingOfOurs()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"1\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE style=\"font-family:Consolas;font-size:9.0pt\"><one:T><![CDATA[something of the user's own]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);

            Assert.That((string?)DefinitionOf(page, OutlineElementAt(page, 0)).Attribute("name"), Is.EqualTo("p"));
        }

        [Test]
        public void Reconcile_LeavesPropertiesADefinitionCannotExpressWhereTheyAre()
        {
            string css = "font-family:'Segoe UI';font-size:11.0pt;text-decoration:underline";
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\" style=\"" + css + "\"><one:T><![CDATA[body text]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);

            Assert.That((string?)OutlineElementAt(page, 0).Attribute("style"), Is.EqualTo(css));
        }

        [Test]
        public void Reconcile_LeavesAParagraphWithoutAnyFontOfItsOwnAlone()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\"><one:T><![CDATA[body text]]></one:T></one:OE>");

            int reconciled = QuickStyleReconciler.Reconcile(page, Catalog);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reconciled, Is.Zero);
                Assert.That(page.QuickStyleDefinitions(), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void Reconcile_GivesAParagraphADefinitionMatchingTheFontItsRunsCarry()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\"><one:T style=\"font-family:'Segoe UI';font-size:11.0pt\"><![CDATA[pasted text]]></one:T></one:OE>");

            int reconciled = QuickStyleReconciler.Reconcile(page, Catalog);
            XElement outlineElement = OutlineElementAt(page, 0);
            XElement definition = DefinitionOf(page, outlineElement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reconciled, Is.EqualTo(1));
                Assert.That((string?)definition.Attribute("font"), Is.EqualTo("Segoe UI"));
                Assert.That((string?)outlineElement.Element(page.One + "T")!.Attribute("style"), Is.EqualTo("font-family:'Segoe UI';font-size:11.0pt"), "The run keeps saying what it always said.");
            }
        }

        [Test]
        public void Reconcile_LeavesAParagraphAloneWhenOnlySomeOfItsRunsNameAFont()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\"><one:T style=\"font-family:'Segoe UI'\"><![CDATA[pasted]]></one:T><one:T><![CDATA[ and typed]]></one:T></one:OE>");

            int reconciled = QuickStyleReconciler.Reconcile(page, Catalog);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reconciled, Is.Zero);
                Assert.That((string?)OutlineElementAt(page, 0).Attribute("quickStyleIndex"), Is.EqualTo("2"));
            }
        }

        [Test]
        public void Reconcile_LeavesAParagraphAloneWhenItsRunsNameDifferentFonts()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\"><one:T style=\"font-family:'Segoe UI'\"><![CDATA[prose]]></one:T><one:T style=\"font-family:Consolas\"><![CDATA[code]]></one:T></one:OE>");

            Assert.That(QuickStyleReconciler.Reconcile(page, Catalog), Is.Zero);
        }

        [Test]
        public void Reconcile_LetsARunsFontWinOverTheParagraphsOwn()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\" style=\"font-family:Cambria\"><one:T style=\"font-family:'Segoe UI'\"><![CDATA[text]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);

            Assert.That((string?)DefinitionOf(page, OutlineElementAt(page, 0)).Attribute("font"), Is.EqualTo("Segoe UI"), "The run is the closest thing to the reader.");
        }

        [Test]
        public void Reconcile_IgnoresAColourThatOnlyTheRunsCarry()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"2\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"2\"><one:T style=\"color:#FF0000\"><![CDATA[warning]]></one:T></one:OE>");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(QuickStyleReconciler.Reconcile(page, Catalog), Is.Zero);
                Assert.That(page.QuickStyleDefinitions(), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void Reconcile_TurnsABareCssColourNameIntoAHexValueOneNoteAccepts()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"1\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE style=\"font-family:'Segoe UI';font-size:11.0pt;color:black\"><one:T><![CDATA[typed text]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);

            Assert.That((string?)DefinitionOf(page, OutlineElementAt(page, 0)).Attribute("fontColor"), Is.EqualTo("#000000"),
                "OneNote's QuickStyleDef schema only accepts #RRGGBB, \"none\" or \"automatic\"; a bare CSS colour name fails UpdatePageContent for the whole page.");
        }

        [Test]
        public void Reconcile_DoesNotKeepCreatingNewDefinitionsForTheSameBareCssColourName()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"1\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE style=\"font-family:'Segoe UI';font-size:11.0pt;color:black\"><one:T><![CDATA[typed text]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);
            int countAfterFirstPass = page.QuickStyleDefinitions().Count();

            QuickStyleReconciler.Reconcile(page, Catalog);

            Assert.That(page.QuickStyleDefinitions(), Has.Count.EqualTo(countAfterFirstPass),
                "The definition just created (fontColor=\"#000000\") must match its own paragraph's colour (\"black\") on the very next pass, or every save keeps growing the page's QuickStyleDef list.");
        }

        [Test]
        public void Reconcile_NumbersANewDefinitionPastEveryExistingOne()
        {
            PageDocument page = PageWith(
                "<one:QuickStyleDef index=\"0\" name=\"PageTitle\" fontColor=\"automatic\" font=\"Segoe UI\" fontSize=\"20.0\"/>"
                + "<one:QuickStyleDef index=\"6\" name=\"p\" fontColor=\"automatic\" font=\"Calibri\" fontSize=\"11.0\"/>",
                "<one:OE quickStyleIndex=\"6\" style=\"font-family:'Segoe UI';font-size:11.0pt\"><one:T><![CDATA[body text]]></one:T></one:OE>");

            QuickStyleReconciler.Reconcile(page, Catalog);

            Assert.That((string?)OutlineElementAt(page, 0).Attribute("quickStyleIndex"), Is.EqualTo("7"));
        }
    }
}
