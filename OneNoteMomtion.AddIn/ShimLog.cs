using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace OneNoteMomtion.AddIn
{
    internal static class ShimLog
    {
        private const long MaxFileBytes = 256 * 1024;
        private static readonly object Gate = new();
        private static string? _path;

        internal static string Path
        {
            get
            {
                _path ??= System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneNoteMomtion", "logs", "addin.log");
                return _path;
            }
        }

        internal static void Info(string message)
        {
            Write("INFO ", message, null);
        }

        internal static void Warn(string message)
        {
            Write("WARN ", message, null);
        }

        internal static void Error(string message, Exception? exception = null)
        {
            Write("ERROR", message, exception);
        }

        private static void Write(string level, string message, Exception? exception)
        {
            try
            {
                lock (Gate)
                {
                    string directory = System.IO.Path.GetDirectoryName(Path);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    RollIfTooLarge();

                    StringBuilder builder = new();
                    builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                    builder.Append(" [addin] ").Append(level).Append(' ').Append(message);

                    if (exception != null)
                    {
                        builder.AppendLine().Append(exception);
                    }

                    File.AppendAllText(Path, builder.ToString() + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
            }
        }

        private static void RollIfTooLarge()
        {
            FileInfo file = new(Path);
            if (!file.Exists || file.Length < MaxFileBytes)
            {
                return;
            }

            string previous = Path + ".1";
            if (File.Exists(previous))
            {
                File.Delete(previous);
            }

            File.Move(Path, previous);
        }
    }
}
