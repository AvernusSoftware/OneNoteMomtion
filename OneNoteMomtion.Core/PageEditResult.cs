namespace OneNoteMomtion.Core
{
    public sealed class PageEditResult
    {
        private PageEditResult(bool changed, string? xml, int affectedNodes, string? message, string? focusObjectId, string? caretObjectId, bool nudgeCaret)
        {
            Changed = changed;
            Xml = xml;
            AffectedNodes = affectedNodes;
            Message = message;
            FocusObjectId = focusObjectId;
            CaretObjectId = caretObjectId;
            NudgeCaret = nudgeCaret;
        }

        public bool Changed { get; }

        public string? Xml { get; }

        public int AffectedNodes { get; }

        public string? Message { get; }

        public string? FocusObjectId { get; }

        public string? CaretObjectId { get; }

        public bool NudgeCaret { get; }

        public static PageEditResult NoChange(string? message = null, string? caretObjectId = null)
        {
            return new PageEditResult(false, null, 0, message, null, caretObjectId, false);
        }

        public static PageEditResult Updated(string xml, int affectedNodes, string? message = null, string? focusObjectId = null, bool nudgeCaret = false)
        {
            return new PageEditResult(true, xml, affectedNodes, message, focusObjectId, null, nudgeCaret);
        }
    }
}
