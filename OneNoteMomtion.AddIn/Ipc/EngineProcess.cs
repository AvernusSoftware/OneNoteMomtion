using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace OneNoteMomtion.AddIn.Ipc
{
    internal sealed class EngineProcess
    {
        internal const string ExecutableName = "OneNoteMomtion.Server.exe";

        private readonly int _hostProcessId;
        private readonly EngineClient _client;
        private Process? _process;

        public EngineProcess(int hostProcessId)
        {
            _hostProcessId = hostProcessId;
            PipeName = PipeNameFor(hostProcessId);
            _client = new EngineClient(PipeName);
        }

        public string PipeName { get; }

        public EngineClient Client => _client;

        public static string PipeNameFor(int hostProcessId)
        {
            return "OneNoteMomtion." + hostProcessId.ToString(CultureInfo.InvariantCulture);
        }

        public static string ExecutablePath
        {
            get
            {
                string directory = Path.GetDirectoryName(typeof(EngineProcess).Assembly.Location) ?? string.Empty;
                return Path.Combine(directory, ExecutableName);
            }
        }

        public bool EnsureRunning()
        {
            if (_process != null && !_process.HasExited)
            {
                return true;
            }

            if (_client.IsRunning())
            {
                return true;
            }

            string executable = ExecutablePath;
            if (!File.Exists(executable))
            {
                ShimLog.Error("Engine not found at path " + executable + ". The ribbon will load, but no command will work.");
                return false;
            }

            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = executable,
                    Arguments = string.Format(CultureInfo.InvariantCulture, "--pipe \"{0}\" --host-pid {1}", PipeName, _hostProcessId),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(executable) ?? string.Empty,
                };

                _process = Process.Start(startInfo);
                ShimLog.Info("Engine started, pipe " + PipeName + ".");
                return _process != null;
            }
            catch (Exception ex)
            {
                ShimLog.Error("Failed to start the engine.", ex);
                return false;
            }
        }

        public void Shutdown()
        {
            try
            {
                _client.Send("engine.shutdown", null, 500);
            }
            catch (Exception ex)
            {
                ShimLog.Warn("The engine shutdown request did not go through: " + ex.Message);
            }

            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.WaitForExit(2000);
                }
            }
            catch (Exception)
            {
                // The process may already be gone - there is nothing to fix here.
            }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }
    }
}
