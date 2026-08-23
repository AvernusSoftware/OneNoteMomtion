namespace OneNoteMomtion.AddIn.Ipc
{
    internal sealed class EngineResponse
    {
        private EngineResponse(bool success, string[] fields)
        {
            Success = success;
            Fields = fields;
        }

        public bool Success { get; }

        public string[] Fields { get; }

        public string Message => Fields.Length > 0 ? Fields[0] : string.Empty;

        public static EngineResponse Ok(string[] fields)
        {
            return new EngineResponse(true, fields);
        }

        public static EngineResponse Error(string message)
        {
            return new EngineResponse(false, [message]);
        }
    }
}
