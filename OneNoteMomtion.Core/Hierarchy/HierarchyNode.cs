using System;
using System.Collections.Generic;

namespace OneNoteMomtion.Core.Hierarchy
{
    public sealed class HierarchyNode
    {
        public HierarchyNode(string id, string name, HierarchyNodeKind kind, IReadOnlyList<HierarchyNode> children)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Kind = kind;
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }

        public string Id { get; }

        public string Name { get; }

        public HierarchyNodeKind Kind { get; }

        public IReadOnlyList<HierarchyNode> Children { get; }
    }
}
