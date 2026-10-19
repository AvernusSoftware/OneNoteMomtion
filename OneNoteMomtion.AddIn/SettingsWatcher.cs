using System;
using System.IO;
using System.Threading;

namespace OneNoteMomtion.AddIn
{
    /// <summary>
    /// Tells the add-in that styles.json changed, so the ribbon can pick up a new interface language without
    /// restarting OneNote. A single save raises several file system events, hence the short quiet period
    /// before <see cref="Changed"/> fires - which also lets the engine finish reading the file first.
    /// </summary>
    internal sealed class SettingsWatcher : IDisposable
    {
        private const string FolderName = "OneNoteMomtion";
        private const string FileName = "styles.json";
        private const int QuietPeriodMilliseconds = 500;

        private readonly FileSystemWatcher _watcher;
        private readonly Timer _quietPeriod;

        public SettingsWatcher()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName);
            Directory.CreateDirectory(folder);

            _quietPeriod = new Timer(_ => Changed?.Invoke(this, EventArgs.Empty), null, Timeout.Infinite, Timeout.Infinite);

            _watcher = new FileSystemWatcher(folder, FileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            };

            _watcher.Changed += (_, _) => Restart();
            _watcher.Created += (_, _) => Restart();
            _watcher.Renamed += (_, _) => Restart();
            _watcher.EnableRaisingEvents = true;
        }

        public event EventHandler? Changed;

        public void Dispose()
        {
            _watcher.Dispose();
            _quietPeriod.Dispose();
        }

        private void Restart()
        {
            _quietPeriod.Change(QuietPeriodMilliseconds, Timeout.Infinite);
        }
    }
}
