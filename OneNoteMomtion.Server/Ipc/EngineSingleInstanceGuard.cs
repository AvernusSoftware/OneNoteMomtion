using System;
using System.IO.Pipes;

namespace OneNoteMomtion.Server.Ipc
{
    internal sealed class EngineSingleInstanceGuard(string pipeName)
    {
        private const int ProbeTimeoutMilliseconds = 250;

        public bool IsAlreadyRunning()
        {
            try
            {
                using NamedPipeClientStream probe = new(".", pipeName, PipeDirection.InOut, PipeOptions.None);
                probe.Connect(ProbeTimeoutMilliseconds);

                EngineLog.Info($"An engine on pipe {pipeName} is already running - exiting this process.");
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
