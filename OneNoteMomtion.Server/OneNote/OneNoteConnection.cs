using System;
using System.Runtime.InteropServices;

namespace OneNoteMomtion.Server.OneNote
{
    internal sealed class OneNoteConnection : IDisposable
    {
        private const string ProgId = "OneNote.Application";

        private const int RpcDisconnected = unchecked((int)0x80010108);

        private const int ObjectNotConnected = unchecked((int)0x800401FD);

        private readonly object _gate = new();
        private IApplication? _application;

        public OneNoteSession? TryGetSession()
        {
            lock (_gate)
            {
                _application ??= CreateApplication();
                return _application is null ? null : new OneNoteSession(_application);
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                ReleaseApplication();
            }
        }

        public static bool IsDisconnected(Exception exception)
        {
            return exception is COMException com && (com.HResult == RpcDisconnected || com.HResult == ObjectNotConnected);
        }

        public void Dispose()
        {
            Reset();
        }

        private static IApplication? CreateApplication()
        {
            try
            {
                Type? type = Type.GetTypeFromProgID(ProgId, throwOnError: false);
                if (type is null)
                {
                    EngineLog.Error($"Program id {ProgId} was not found. Is desktop OneNote installed (not the Store version)?");
                    return null;
                }

                object? instance = Activator.CreateInstance(type);
                if (instance is null)
                {
                    EngineLog.Error($"Failed to create an instance of {ProgId}.");
                    return null;
                }

                if (instance is not IApplication application)
                {
                    EngineLog.Error($"The {ProgId} object does not expose the IApplication interface.");
                    Marshal.FinalReleaseComObject(instance);
                    return null;
                }

                EngineLog.Info("Connected to the OneNote object model.");
                return application;
            }
            catch (Exception ex)
            {
                EngineLog.Error($"Failed to connect to {ProgId}.", ex);
                return null;
            }
        }

        private void ReleaseApplication()
        {
            if (_application is null)
            {
                return;
            }

            try
            {
                Marshal.FinalReleaseComObject(_application);
            }
            catch (Exception)
            {
                // The object may already have gone along with the OneNote process.
            }
            finally
            {
                _application = null;
            }
        }
    }
}
