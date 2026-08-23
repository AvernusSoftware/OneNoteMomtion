using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace OneNoteMomtion.Core.Reformat
{
    public sealed class CodeBlockCandidate
    {
        public CodeBlockCandidate(XElement anchor, IReadOnlyList<XElement> remove, IReadOnlyList<XElement> sourceLines, string? knownLanguageId, bool isOwn)
        {
            Anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
            Remove = remove ?? throw new ArgumentNullException(nameof(remove));
            SourceLines = sourceLines ?? throw new ArgumentNullException(nameof(sourceLines));
            KnownLanguageId = knownLanguageId;
            IsOwn = isOwn;
        }

        public XElement Anchor { get; }

        public IReadOnlyList<XElement> Remove { get; }

        public IReadOnlyList<XElement> SourceLines { get; }

        public string? KnownLanguageId { get; }

        public bool IsOwn { get; }

        public string? ExistingBlockId { get; set; }
    }
}
