using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OneNoteMomtion.Core.Highlighting;
using OneNoteMomtion.Server.Commands;
using OneNoteMomtion.Server.Ipc;
using OneNoteMomtion.Server.OneNote;
using OneNoteMomtion.Server.Ui;

namespace OneNoteMomtion.Server
{
    internal static class ProtocolHandler
    {
        private const int ForwardTimeoutMilliseconds = 500;

        private const string OneNoteProcessName = "ONENOTE";

        public static int Run(string uri)
        {
            EngineLog.Info("Handling protocol request " + uri);

            if (!CodeBlockLink.TryGetCopyBlockId(uri, out string blockId))
            {
                EngineLog.Warn("Protocol request \"" + uri + "\" is not a recognised copy link - ignoring it.");
                return 1;
            }

            if (TryForwardToRunningEngine(blockId))
            {
                return 0;
            }

            return CopyWithoutEngine(blockId);
        }

        private static bool TryForwardToRunningEngine(string blockId)
        {
            foreach (int processId in OneNoteProcessIds())
            {
                string pipeName = EngineCommandClient.PipeNameFor(processId);

                if (EngineCommandClient.TrySend(pipeName, CommandIds.CodeCopyById, blockId, ForwardTimeoutMilliseconds))
                {
                    EngineLog.Info("Copy request handed over to the engine on pipe " + pipeName + ".");
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<int> OneNoteProcessIds()
        {
            List<int> ordered = [];

            int foreground = ForegroundProcessId();
            if (foreground != 0)
            {
                ordered.Add(foreground);
            }

            foreach (Process process in Process.GetProcessesByName(OneNoteProcessName))
            {
                using (process)
                {
                    if (!ordered.Contains(process.Id))
                    {
                        ordered.Add(process.Id);
                    }
                }
            }

            return ordered;
        }

        private static int ForegroundProcessId()
        {
            IntPtr window = GetForegroundWindow();
            if (window == IntPtr.Zero)
            {
                return 0;
            }

            GetWindowThreadProcessId(window, out int processId);
            return processId;
        }

        private static int CopyWithoutEngine(string blockId)
        {
            EngineLog.Info("No engine is running - copying the code block in the protocol handler process.");

            CommandResult result;

            using (OneNoteConnection connection = new())
            {
                OneNoteSession? session = connection.TryGetSession();

                result = session is null ? CommandResult.Failed("There is no connection to OneNote.") : CodeCopyOperation.Run(new EngineServices().Editor, session, blockId);
            }

            EngineLog.Info("Copy result: " + (result.Success ? "ok" : "failed") + " - " + result.Message);

            ShowToastAndWait(result.Message ?? "The code was copied.");
            return result.Success ? 0 : 1;
        }

        private static void ShowToastAndWait(string message)
        {
            ToastWindow toast = ToastWindow.Show(message);
            toast.FormClosed += (_, _) => Application.ExitThread();
            Application.Run();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);
    }
}
