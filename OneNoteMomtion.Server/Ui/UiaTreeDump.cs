using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Interop.UIAutomationClient;

namespace OneNoteMomtion.Server.Ui
{
    internal static class UiaTreeDump
    {
        private const int DefaultMaxDepth = 24;
        private const int DefaultMaxNodes = 6000;

        private static readonly TimeSpan MaxDuration = TimeSpan.FromSeconds(20);

        internal static string Path => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(EngineLog.Path) ?? ".", "uia-tree.txt");

        public static bool TryDump(IntPtr window, out string message)
        {
            message = string.Empty;

            if (window == IntPtr.Zero)
            {
                message = "The OneNote window could not be found.";
                return false;
            }

            IUIAutomation automation;
            IUIAutomationElement root;
            IUIAutomationTreeWalker walker;

            try
            {
                automation = new CUIAutomationClass();
                root = automation.ElementFromHandle(window);
                walker = automation.RawViewWalker;
            }
            catch (Exception ex)
            {
                EngineLog.Error($"UIA-TREE: could not attach to the window.", ex);
                message = $"UI Automation could not attach to the OneNote window: {ex.Message}";
                return false;
            }

            StringBuilder builder = new();
            builder.Append("OneNote UI Automation tree, ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).AppendLine();
            builder.Append("window ").Append(OneNoteWindowFinder.Describe(window)).AppendLine();
            builder.AppendLine();

            Stopwatch budget = Stopwatch.StartNew();
            int visited = 0;

            Walk(walker, root, 0, builder, ref visited, budget);

            builder.AppendLine();
            builder.Append("-- ").Append(visited).Append(" nodes in ").Append(budget.ElapsedMilliseconds).Append(" ms").AppendLine();

            try
            {
                string path = Path;
                string? directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                EngineLog.Error("UIA-TREE: writing the dump failed.", ex);
                message = "The tree could not be written: " + ex.Message;
                return false;
            }

            EngineLog.Info($"UIA-TREE: {visited} nodes written to {Path}.");
            message = $"{visited} nodes written to {Path}";
            return true;
        }

        private static void Walk(IUIAutomationTreeWalker walker, IUIAutomationElement element, int depth, StringBuilder builder, ref int visited, Stopwatch budget)
        {
            if (visited >= DefaultMaxNodes || budget.Elapsed > MaxDuration)
            {
                return;
            }

            visited++;

            builder.Append(' ', depth * 2).Append(UiaHelpers.Describe(element)).AppendLine();

            if (depth >= DefaultMaxDepth)
            {
                builder.Append(' ', (depth + 1) * 2).AppendLine("... depth limit");
                return;
            }

            IUIAutomationElement? child;
            try
            {
                child = walker.GetFirstChildElement(element);
            }
            catch (Exception ex)
            {
                builder.Append(' ', (depth + 1) * 2).Append("<err:").Append(ex.Message).AppendLine(">");
                return;
            }

            while (child is not null)
            {
                Walk(walker, child, depth + 1, builder, ref visited, budget);

                if (visited >= DefaultMaxNodes || budget.Elapsed > MaxDuration)
                {
                    builder.Append(' ', (depth + 1) * 2).AppendLine("... budget exhausted");
                    return;
                }

                try
                {
                    child = walker.GetNextSiblingElement(child);
                }
                catch (Exception ex)
                {
                    builder.Append(' ', (depth + 1) * 2).Append("<err:").Append(ex.Message).AppendLine(">");
                    return;
                }
            }
        }
    }
}
