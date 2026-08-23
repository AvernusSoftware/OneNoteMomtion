using System;
using System.Collections.Generic;
using System.Text;

namespace OneNoteMomtion.Core.Hierarchy
{
    public sealed class HierarchyPath
    {
        public const string DefaultSeparator = " › ";

        public HierarchyPath(IReadOnlyList<HierarchyNode> segments)
        {
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        }

        public IReadOnlyList<HierarchyNode> Segments { get; }

        public IEnumerable<HierarchyNode> Containers
        {
            get
            {
                foreach (HierarchyNode segment in Segments)
                {
                    if (segment.Kind != HierarchyNodeKind.Page)
                    {
                        yield return segment;
                    }
                }
            }
        }

        public string ToBreadcrumb(string separator = DefaultSeparator)
        {
            StringBuilder builder = new();

            foreach (HierarchyNode segment in Segments)
            {
                if (builder.Length > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(segment.Name);
            }

            return builder.ToString();
        }
    }
}
