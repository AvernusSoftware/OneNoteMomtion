namespace OneNoteMomtion.Core.Xml
{
    public sealed class SegmentContext
    {
        public SegmentContext(string text, bool isFirst, bool isLast, char? previousCharacter)
        {
            Text = text ?? string.Empty;
            IsFirst = isFirst;
            IsLast = isLast;
            PreviousCharacter = previousCharacter;
        }

        public string Text { get; }

        public bool IsFirst { get; }

        public bool IsLast { get; }

        public char? PreviousCharacter { get; }
    }
}