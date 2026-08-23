using System;
using System.Globalization;

namespace OneNoteMomtion.Server
{
    internal sealed class EngineOptions
    {
        public string PipeName { get; private set; } = "OneNoteMomtion.default";

        public int? HostProcessId { get; private set; }

        public string? Uri { get; private set; }

        public static EngineOptions Parse(string[] args)
        {
            EngineOptions options = new();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--pipe" when i + 1 < args.Length:
                        options.PipeName = args[++i];
                        break;

                    case "--uri" when i + 1 < args.Length:
                        options.Uri = args[++i];
                        break;

                    case "--host-pid" when i + 1 < args.Length:
                        if (int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pid) && pid > 0)
                        {
                            options.HostProcessId = pid;
                        }

                        break;
                }
            }

            return options;
        }
    }
}
