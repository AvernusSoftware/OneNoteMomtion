namespace OneNoteMomtion.Server.Commands
{
    internal sealed class CommandResult
    {
        private CommandResult(bool success, bool changed, string? message, object? data, string? caretObjectId = null)
        {
            Success = success;
            Changed = changed;
            Message = message;
            Data = data;
            CaretObjectId = caretObjectId;
        }

        public bool Success { get; }

        public bool Changed { get; }

        public string? Message { get; }

        public object? Data { get; }

        public static CommandResult Applied(string? message = null)
        {
            return new CommandResult(true, true, message, null);
        }

        public string? CaretObjectId { get; }

        public static CommandResult NothingToDo(string? message = null, string? caretObjectId = null)
        {
            return new CommandResult(true, false, message, null, caretObjectId);
        }

        public static CommandResult Payload(object data)
        {
            return new CommandResult(true, false, null, data);
        }

        public static CommandResult Failed(string message)
        {
            return new CommandResult(false, false, message, null);
        }
    }
}
