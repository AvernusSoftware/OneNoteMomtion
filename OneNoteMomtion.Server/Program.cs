using System;
using System.Threading;
using System.Windows.Forms;
using OneNoteMomtion.Server.Ipc;

namespace OneNoteMomtion.Server
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            EngineOptions options = EngineOptions.Parse(args);
            ApplicationConfiguration.Initialize();

            if (options.Uri != null)
            {
                return ProtocolHandler.Run(options.Uri);
            }

            if (new EngineSingleInstanceGuard(options.PipeName).IsAlreadyRunning())
            {
                return 0;
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => EngineLog.Error("Unhandled UI thread exception.", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => EngineLog.Error("Unhandled exception.", e.ExceptionObject as Exception);
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            EngineLog.Info("Starting engine, pipe " + options.PipeName + ", host " + (options.HostProcessId?.ToString() ?? "none") + ".");

            try
            {
                using EngineContext context = new(options);
                Application.Run(context);
                return 0;
            }
            catch (Exception ex)
            {
                EngineLog.Error("The engine exited with an error.", ex);
                return 1;
            }
            finally
            {
                EngineLog.Info("Engine stopped.");
            }
        }
    }
}