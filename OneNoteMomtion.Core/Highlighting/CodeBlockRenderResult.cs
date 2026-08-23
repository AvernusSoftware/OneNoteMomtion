using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace OneNoteMomtion.Core.Highlighting
{
    public sealed class CodeBlockRenderResult
    {
        public CodeBlockRenderResult(IReadOnlyList<XElement> rootElements, IReadOnlyList<XElement> codeLineElements)
        {
            RootElements = rootElements ?? throw new ArgumentNullException(nameof(rootElements));
            CodeLineElements = codeLineElements ?? throw new ArgumentNullException(nameof(codeLineElements));
        }

        public IReadOnlyList<XElement> RootElements { get; }

        public IReadOnlyList<XElement> CodeLineElements { get; }
    }
}
