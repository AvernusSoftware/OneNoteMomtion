using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Extensibility;
using Microsoft.Office.Core;
using OneNoteMomtion.AddIn.Ipc;

namespace OneNoteMomtion.AddIn
{
    [ComVisible(true)]
    [Guid(ClassId)]
    [ProgId(ProgIdValue)]
    /// <remarks>
    /// <see cref="ClassInterfaceType.AutoDispatch"/> is a requirement here, not a matter of style. Office
    /// finds the ribbon callbacks by name through <c>IDispatch::GetIDsOfNames</c> on the add-in object, so
    /// the class's public methods have to be visible through <c>IDispatch</c> - which rules out
    /// <see cref="ClassInterfaceType.None"/>. <see cref="ClassInterfaceType.AutoDual"/> would work but would
    /// freeze the layout of the class method table against every future change, and nothing needs that.
    /// </remarks>
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public partial class AddIn : IDTExtensibility2, IRibbonExtensibility
    {
        internal const string ClassId = "7F6CF95B-A3C4-4EFC-A774-9F0A4B741C46";

        internal const string ProgIdValue = "OneNoteMomtion.AddIn";

        private const string RibbonResourceName = "OneNoteMomtion.AddIn.Ribbon.Ribbon.xml";

        private const int DebugBuildNumber = 58;

        private EngineProcess? _engine;

        public AddIn()
        {
            ShimLog.Info("Add-in object created in process " + Process.GetCurrentProcess().ProcessName + " (PID " + Process.GetCurrentProcess().Id + ", " + (IntPtr.Size == 8 ? "64-bit" : "32-bit") + ").");
        }

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInstance, ref Array custom)
        {
            try
            {
                ShimLog.Info("OnConnection (" + connectMode + "), version " + typeof(AddIn).Assembly.GetName().Version + ", debug build #" + DebugBuildNumber + ".");

                int hostProcessId = HostProcess.ResolveId();
                ShimLog.Info("Host is process " + hostProcessId + ".");

                _engine = new EngineProcess(hostProcessId);
                _engine.EnsureRunning();
            }
            catch (Exception ex)
            {
                ShimLog.Error("OnConnection failed.", ex);
            }
        }

        public void OnDisconnection(ext_DisconnectMode removeMode, ref Array custom)
        {
            try
            {
                ShimLog.Info("OnDisconnection (" + removeMode + ").");
                _engine?.Shutdown();
                _engine = null;
                ReleaseRibbon();
                ForceComCleanup();
            }
            catch (Exception ex)
            {
                ShimLog.Error("OnDisconnection failed.", ex);
            }
        }

        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnStartupComplete(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
            try
            {
                _engine?.Shutdown();
                _engine = null;
                ReleaseRibbon();
                ForceComCleanup();
            }
            catch (Exception ex)
            {
                ShimLog.Error("OnBeginShutdown failed.", ex);
            }
        }

        private static void ForceComCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public string GetCustomUI(string ribbonId)
        {
            try
            {
                ShimLog.Info("GetCustomUI for ribbon \"" + ribbonId + "\".");

                using Stream? stream = typeof(AddIn).Assembly.GetManifestResourceStream(RibbonResourceName);
                if (stream == null)
                {
                    ShimLog.Error("Missing resource " + RibbonResourceName + " in the assembly.");
                    return string.Empty;
                }

                using StreamReader reader = new(stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                ShimLog.Error("Failed to return the ribbon definition.", ex);
                return string.Empty;
            }
        }
    }
}
