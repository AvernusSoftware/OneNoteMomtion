using System;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace OneNoteMomtion.Server.Ipc
{
    internal static class EngineCommandClient
    {
        public static string PipeNameFor(int hostProcessId)
        {
            return $"OneNoteMomtion.{hostProcessId}";
        }

        public static bool TrySend(string pipeName, string command, string? argument, int timeoutMilliseconds)
        {
            try
            {
                using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.InOut, PipeOptions.None);
                pipe.Connect(timeoutMilliseconds);

                StreamWriter writer = new(pipe, new UTF8Encoding(false)) { AutoFlush = false };
                writer.WriteLine(PipeProtocol.Encode(command, argument));
                writer.Flush();

                StreamReader reader = new(pipe, new UTF8Encoding(false));
                string? line = reader.ReadLine();

                string[] fields = PipeProtocol.Decode(line);
                if (fields.Length == 0)
                {
                    EngineLog.Warn($"The engine on pipe {pipeName} accepted \"{command}\" but sent no reply.");
                    return false;
                }

                if (fields[0] != PipeProtocol.StatusOk)
                {
                    EngineLog.Warn($"The engine on pipe {pipeName} rejected \"{command}\": {(fields.Length > 1 ? fields[1] : "no message")}.");
                }

                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (Exception ex)
            {
                EngineLog.Warn($"Forwarding \"{command}\" to pipe {pipeName} failed: {ex.Message}");
                return false;
            }
        }
    }
}
