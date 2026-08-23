namespace OneNoteMomtion.Core
{
    public sealed class CodeExtractResult
    {
        private CodeExtractResult(bool success, string? text, int lineCount, string? message)
        {
            Success = success;
            Text = text;
            LineCount = lineCount;
            Message = message;
        }

        public bool Success { get; }

        public string? Text { get; }

        public int LineCount { get; }

        public string? Message { get; }

        public static CodeExtractResult Extracted(string text, int lineCount, string? message = null)
        {
            return new CodeExtractResult(true, text, lineCount, message);
        }

        public static CodeExtractResult Failed(string message)
        {
            return new CodeExtractResult(false, null, 0, message);
        }
    }
}
