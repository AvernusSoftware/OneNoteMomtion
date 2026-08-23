using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace OneNoteMomtion.AddIn.Ipc
{
    internal sealed class EngineClient
    {
        private readonly string _pipeName;

        public EngineClient(string pipeName)
        {
            _pipeName = pipeName;
        }

        public EngineResponse Send(string command, string? argument = null, int timeoutMilliseconds = 4000)
        {
            try
            {
                using NamedPipeClientStream pipe = new(".", _pipeName, PipeDirection.InOut, PipeOptions.None);
                pipe.Connect(timeoutMilliseconds);

                StreamWriter writer = new(pipe, new UTF8Encoding(false)) { AutoFlush = false };
                writer.WriteLine(PipeProtocol.Encode(command, argument));
                writer.Flush();

                StreamReader reader = new(pipe, new UTF8Encoding(false));
                string? line = reader.ReadLine();

                string[] fields = PipeProtocol.Decode(line);
                if (fields.Length == 0)
                {
                    return EngineResponse.Error("The engine did not respond.");
                }

                string[] payload = new string[Math.Max(0, fields.Length - 1)];
                Array.Copy(fields, 1, payload, 0, payload.Length);

                return fields[0] == PipeProtocol.StatusOk ? EngineResponse.Ok(payload) : EngineResponse.Error(payload.Length > 0 ? payload[0] : "Unknown engine error.");
            }
            catch (TimeoutException)
            {
                return EngineResponse.Error("The OneNoteMomtion engine is not responding.");
            }
            catch (Exception ex)
            {
                ShimLog.Error("Command \"" + command + "\" did not reach the engine.", ex);
                return EngineResponse.Error(ex.Message);
            }
        }

        public bool IsRunning(int timeoutMilliseconds = 200)
        {
            try
            {
                using NamedPipeClientStream pipe = new(".", _pipeName, PipeDirection.InOut, PipeOptions.None);
                pipe.Connect(timeoutMilliseconds);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
