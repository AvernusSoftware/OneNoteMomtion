using System;
using System.Diagnostics;

namespace OneNoteMomtion.AddIn
{
    internal static class HostProcess
    {
        private const string OneNoteProcessName = "ONENOTE";

        internal static int ResolveId()
        {
            Process current = Process.GetCurrentProcess();

            try
            {
                if (string.Equals(current.ProcessName, OneNoteProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return current.Id;
                }

                int sessionId = current.SessionId;
                int found = 0;
                int count = 0;

                foreach (Process candidate in Process.GetProcessesByName(OneNoteProcessName))
                {
                    using (candidate)
                    {
                        try
                        {
                            if (candidate.SessionId != sessionId)
                            {
                                continue;
                            }

                            found = candidate.Id;
                            count++;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                if (count == 1)
                {
                    return found;
                }

                ShimLog.Warn($"Could not uniquely identify the OneNote process (matches: {count}). The engine will get the process id of {current.ProcessName}; the hotkey may not work.");
            }
            catch (Exception ex)
            {
                ShimLog.Warn("Looking up the OneNote process failed: " + ex.Message);
            }

            return current.Id;
        }
    }
}
