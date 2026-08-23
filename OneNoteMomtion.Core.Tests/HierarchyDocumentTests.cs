using System.Collections.Generic;
using NUnit.Framework;
using OneNoteMomtion.Core.Hierarchy;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class HierarchyDocumentTests
    {
        private const string PageId = "{page-1}";

        [Test]
        public void FindsAPageSittingInASectionOfANotebook()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Work", "{nb-1}", null,
                    HierarchyXml.Section("Meetings", "{sec-1}", HierarchyXml.Page("Monday", PageId))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            Assert.That(hierarchy.TryFindPagePath(PageId, out HierarchyPath? path), Is.True);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(path!.ToBreadcrumb(), Is.EqualTo("Work › Meetings › Monday"));
                Assert.That(Kinds(path), Is.EqualTo(new[] { HierarchyNodeKind.Notebook, HierarchyNodeKind.Section, HierarchyNodeKind.Page }));
                Assert.That(path.Segments[1].Id, Is.EqualTo("{sec-1}"));
            }
        }

        [Test]
        public void FindsAPageThroughNestedSectionGroups()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Work", "{nb-1}", null,
                    HierarchyXml.SectionGroup("Projects", "{grp-1}",
                        HierarchyXml.SectionGroup("2026", "{grp-2}",
                            HierarchyXml.Section("Alpha", "{sec-1}", HierarchyXml.Page("Kick-off", PageId))))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            Assert.That(hierarchy.TryFindPagePath(PageId, out HierarchyPath? path), Is.True);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(path!.ToBreadcrumb(), Is.EqualTo("Work › Projects › 2026 › Alpha › Kick-off"));
                Assert.That(Names(path.Containers), Is.EqualTo(new[] { "Work", "Projects", "2026", "Alpha" }));
            }
        }

        [Test]
        public void ANotebookIsNamedAfterItsNicknameRatherThanItsFolder()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Work notebook (2019)", "{nb-1}", "Work",
                    HierarchyXml.Section("Meetings", "{sec-1}", HierarchyXml.Page("Monday", PageId))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hierarchy.TryFindPagePath(PageId, out HierarchyPath? path), Is.True);
                Assert.That(path!.Segments[0].Name, Is.EqualTo("Work"));
            }
        }

        [Test]
        public void ANotebookWithoutANicknameKeepsItsName()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Work", "{nb-1}", null,
                    HierarchyXml.Section("Meetings", "{sec-1}", HierarchyXml.Page("Monday", PageId))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hierarchy.TryFindPagePath(PageId, out HierarchyPath? path), Is.True);
                Assert.That(path!.Segments[0].Name, Is.EqualTo("Work"));
            }
        }

        [Test]
        public void APageInTheRecycleBinHasNoPositionToReveal()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Work", "{nb-1}", null,
                    HierarchyXml.RecycleBin("Deleted Pages", "{bin-1}",
                        HierarchyXml.Section("Meetings", "{sec-1}", HierarchyXml.Page("Monday", PageId)))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hierarchy.TryFindPagePath(PageId, out HierarchyPath? path), Is.False);
                Assert.That(path, Is.Null);
                Assert.That(hierarchy.Notebooks[0].Children, Is.Empty);
            }
        }

        [Test]
        public void AnUnknownPageIsNotFound()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Work", "{nb-1}", null,
                    HierarchyXml.Section("Meetings", "{sec-1}", HierarchyXml.Page("Monday", PageId))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hierarchy.TryFindPagePath("{page-missing}", out HierarchyPath? path), Is.False);
                Assert.That(path, Is.Null);
            }
        }

        [TestCase("")]
        [TestCase(null)]
        public void AnEmptyPageIdIsNotFound(string? pageId)
        {
            string xml = HierarchyXml.Notebooks(HierarchyXml.Notebook("Work", "{nb-1}", null));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hierarchy.TryFindPagePath(pageId!, out HierarchyPath? path), Is.False);
                Assert.That(path, Is.Null);
            }
        }

        [Test]
        public void TheSearchLooksThroughEveryNotebookAndSection()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Private", "{nb-1}", null,
                    HierarchyXml.Section("Recipes", "{sec-1}", HierarchyXml.Page("Bread", "{page-9}"))),
                HierarchyXml.Notebook("Work", "{nb-2}", null,
                    HierarchyXml.Section("Meetings", "{sec-2}", HierarchyXml.Page("Monday", "{page-8}")),
                    HierarchyXml.Section("Notes", "{sec-3}", HierarchyXml.Page("Ideas", PageId))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hierarchy.TryFindPagePath(PageId, out HierarchyPath? path), Is.True);
                Assert.That(path!.ToBreadcrumb(), Is.EqualTo("Work › Notes › Ideas"));
            }
        }

        [Test]
        public void ContainersLeaveOutThePageItself()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Work", "{nb-1}", null,
                    HierarchyXml.Section("Meetings", "{sec-1}", HierarchyXml.Page("Monday", PageId))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);
            hierarchy.TryFindPagePath(PageId, out HierarchyPath? path);

            Assert.That(Names(path!.Containers), Is.EqualTo(new[] { "Work", "Meetings" }));
        }

        [Test]
        public void TheBreadcrumbSeparatorCanBeChosen()
        {
            string xml = HierarchyXml.Notebooks(
                HierarchyXml.Notebook("Work", "{nb-1}", null,
                    HierarchyXml.Section("Meetings", "{sec-1}", HierarchyXml.Page("Monday", PageId))));

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);
            hierarchy.TryFindPagePath(PageId, out HierarchyPath? path);

            Assert.That(path!.ToBreadcrumb(" / "), Is.EqualTo("Work / Meetings / Monday"));
        }

        [Test]
        public void TheTreeIsAlsoReadFromASingleNotebookRoot()
        {
            string xml = "<?xml version=\"1.0\"?>" +
                HierarchyXml.Notebook("Work", "{nb-1}", null,
                    HierarchyXml.Section("Meetings", "{sec-1}", HierarchyXml.Page("Monday", PageId)))
                    .Replace("<one:Notebook ", "<one:Notebook xmlns:one=\"" + HierarchyXml.Namespace + "\" ");

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hierarchy.Notebooks, Has.Count.EqualTo(1));
                Assert.That(hierarchy.TryFindPagePath(PageId, out HierarchyPath? path), Is.True);
                Assert.That(path!.ToBreadcrumb(), Is.EqualTo("Work › Meetings › Monday"));
            }
        }

        [Test]
        public void QuickNotesCountsAsASection()
        {
            string xml = HierarchyXml.Notebooks(
                    HierarchyXml.Notebook("Work", "{nb-1}", null))
                .Replace("</one:Notebooks>", "<one:UnfiledNotes name=\"Quick Notes\" ID=\"{sec-1}\">" + HierarchyXml.Page("Idea", PageId) + "</one:UnfiledNotes></one:Notebooks>");

            HierarchyDocument hierarchy = HierarchyDocument.Parse(xml);

            Assert.That(hierarchy.TryFindPagePath(PageId, out HierarchyPath? path), Is.True);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(path!.Segments[0].Kind, Is.EqualTo(HierarchyNodeKind.Section));
                Assert.That(path.ToBreadcrumb(), Is.EqualTo("Quick Notes › Idea"));
            }
        }

        [Test]
        public void UnreadableXmlIsReportedRatherThanThrown()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(HierarchyDocument.TryParse("not xml at all", out HierarchyDocument? hierarchy), Is.False);
                Assert.That(hierarchy, Is.Null);
                Assert.That(HierarchyDocument.TryParse("<root><child /></root>", out HierarchyDocument? foreign), Is.False);
                Assert.That(foreign, Is.Null);
            }
        }

        private static IReadOnlyList<HierarchyNodeKind> Kinds(HierarchyPath path)
        {
            List<HierarchyNodeKind> kinds = [];

            foreach (HierarchyNode segment in path.Segments)
            {
                kinds.Add(segment.Kind);
            }

            return kinds;
        }

        private static IReadOnlyList<string> Names(IEnumerable<HierarchyNode> nodes)
        {
            List<string> names = [];

            foreach (HierarchyNode node in nodes)
            {
                names.Add(node.Name);
            }

            return names;
        }
    }
}