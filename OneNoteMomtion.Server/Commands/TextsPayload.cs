using System.Collections.Generic;

namespace OneNoteMomtion.Server.Commands
{
    internal sealed class TextsPayload
    {
        public TextsPayload(string language, IReadOnlyDictionary<string, string> texts)
        {
            Language = language;
            Texts = texts;
        }

        public string Language { get; }

        public IReadOnlyDictionary<string, string> Texts { get; }
    }
}
