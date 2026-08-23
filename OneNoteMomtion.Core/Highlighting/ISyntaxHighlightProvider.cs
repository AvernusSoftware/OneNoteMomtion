using System.Collections.Generic;

namespace OneNoteMomtion.Core.Highlighting
{
    public interface ISyntaxHighlightProvider
    {
        string LanguageId { get; }

        string DisplayName { get; }

        IEnumerable<Token> Tokenize(string source);
    }
}
