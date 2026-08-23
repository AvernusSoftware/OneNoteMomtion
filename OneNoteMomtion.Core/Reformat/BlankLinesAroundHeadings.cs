using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OneNoteMomtion.Core.Styles;
using OneNoteMomtion.Core.Xml;

namespace OneNoteMomtion.Core.Reformat
{
    public static class BlankLinesAroundHeadings
    {
        public static int Apply(PageDocument page, StyleCatalog catalog, ISet<XElement> skip)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentNullException.ThrowIfNull(skip);

            int removed = 0;

            foreach (XElement heading in page.AllOutlineElements().ToList())
            {
                if (skip.Contains(heading) || !IsHeading(page, catalog, heading))
                {
                    continue;
                }

                removed += ClearAbove(page, heading, skip);
                removed += ClearBelow(page, heading, skip);
            }

            return removed;
        }

        private static bool IsHeading(PageDocument page, StyleCatalog catalog, XElement outlineElement)
        {
            return HeadingNormalizer.HeadingLevelOf(page, outlineElement) > 0 || OeStyleReader.IsHeading(page, outlineElement, catalog);
        }

        private static int ClearAbove(PageDocument page, XElement heading, ISet<XElement> skip)
        {
            int removed = 0;

            while (true)
            {
                XElement? previous = PreviousOf(page, heading);

                if (previous == null || skip.Contains(previous))
                {
                    return removed;
                }

                if (IsBlank(page, previous))
                {
                    previous.Remove();
                    removed++;
                    continue;
                }

                if (!IsCarrier(page, previous))
                {
                    return removed;
                }

                XElement? anchor = AnchorAbove(page, previous, skip);

                if (anchor == null)
                {
                    return removed;
                }

                Fold(page, previous, anchor);
                removed++;
            }
        }

        private static int ClearBelow(PageDocument page, XElement heading, ISet<XElement> skip)
        {
            if (heading.Elements(page.One + "OEChildren").Any())
            {
                return 0;
            }

            int removed = 0;

            while (true)
            {
                XElement? next = NextOf(page, heading);

                if (next == null || skip.Contains(next))
                {
                    return removed;
                }

                if (IsBlank(page, next))
                {
                    next.Remove();
                    removed++;
                    continue;
                }

                if (!IsCarrier(page, next))
                {
                    return removed;
                }

                Fold(page, next, heading);

                return removed + 1;
            }
        }

        private static XElement? AnchorAbove(PageDocument page, XElement carrier, ISet<XElement> skip)
        {
            XElement? candidate = PreviousOf(page, carrier);

            while (candidate != null && !skip.Contains(candidate) && IsBlank(page, candidate))
            {
                candidate = PreviousOf(page, candidate);
            }

            return candidate == null || skip.Contains(candidate) ? null : candidate;
        }

        private static void Fold(PageDocument page, XElement carrier, XElement owner)
        {
            XElement children = carrier.Element(page.One + "OEChildren")!;
            XElement? existing = owner.Element(page.One + "OEChildren");

            children.Remove();

            if (existing == null)
            {
                owner.Add(children);
            }
            else
            {
                existing.Add(children.Elements(page.One + "OE").ToList());
            }

            carrier.Remove();
        }

        private static bool IsCarrier(PageDocument page, XElement outlineElement)
        {
            if (outlineElement.Elements(page.One + "OEChildren").Count() != 1)
            {
                return false;
            }

            if (!outlineElement.Elements().All(child => child.Name == page.One + "T" || child.Name == page.One + "OEChildren"))
            {
                return false;
            }

            return OeStyleReader.IsEmpty(page, outlineElement);
        }

        private static bool IsBlank(PageDocument page, XElement outlineElement)
        {
            if (!outlineElement.Elements().All(child => child.Name == page.One + "T"))
            {
                return false;
            }

            return OeStyleReader.IsEmpty(page, outlineElement);
        }

        private static XElement? PreviousOf(PageDocument page, XElement outlineElement)
        {
            return outlineElement.ElementsBeforeSelf(page.One + "OE").LastOrDefault();
        }

        private static XElement? NextOf(PageDocument page, XElement outlineElement)
        {
            return outlineElement.ElementsAfterSelf(page.One + "OE").FirstOrDefault();
        }
    }
}