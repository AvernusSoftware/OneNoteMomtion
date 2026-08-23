using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Interop.UIAutomationClient;

namespace OneNoteMomtion.Server.Ui
{
    internal static class CaretLocator
    {
        private const int UIA_TextPatternId = 10014;
        private const int UIA_TextPattern2Id = 10024;

        public static Point? TryGetCaretScreenPosition()
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                EngineLog.Info("UIA-DEBUG: GetForegroundWindow returned zero.");
                return null;
            }

            uint threadId = GetWindowThreadProcessId(foreground, out uint processId);

            EngineLog.Info(
                "UIA-DEBUG: foreground=" + foreground.ToInt64().ToString("X") +
                " class=" + DescribeWindow(foreground) +
                " pid=" + processId + " tid=" + threadId);

            IntPtr focusedHwnd = foreground;

            if (threadId != 0)
            {
                GuiThreadInfo info = default;
                info.cbSize = Marshal.SizeOf<GuiThreadInfo>();

                if (GetGUIThreadInfo(threadId, ref info) && info.hwndFocus != IntPtr.Zero)
                {
                    EngineLog.Info($"UIA-DEBUG: hwndFocus={Hex(info.hwndFocus)} ({DescribeWindow(info.hwndFocus)})");
                    focusedHwnd = info.hwndFocus;
                }
            }

            IUIAutomation automation;
            try
            {
                automation = new CUIAutomationClass();
            }
            catch (Exception ex)
            {
                EngineLog.Warn($"UIA-DEBUG: creating CUIAutomation failed: {ex.Message}");
                return null;
            }

            IUIAutomationElement? focused = null;
            try
            {
                focused = automation.GetFocusedElement();
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-DEBUG: GetFocusedElement failed: {ex.Message}");
            }

            Point? result = TryGetCaretFromElement(automation, focused, "focused");
            if (result != null)
            {
                return result;
            }

            IUIAutomationElement? byHandle = null;
            try
            {
                byHandle = automation.ElementFromHandle(focusedHwnd);
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-DEBUG: ElementFromHandle failed: {ex.Message}");
            }

            return TryGetCaretFromElement(automation, byHandle, "hwnd");
        }

        private static Point? TryGetCaretFromElement(IUIAutomation automation, IUIAutomationElement? element, string source)
        {
            if (element == null)
            {
                EngineLog.Info($"UIA-DEBUG[{source}]: no element.");
                return null;
            }

            EngineLog.Info(
                "UIA-DEBUG[" + source + "]: element name='" + SafeGet(() => element.CurrentName) +
                "' class='" + SafeGet(() => element.CurrentClassName) +
                "' controlType=" + SafeGet(() => element.CurrentControlType.ToString()) +
                " automationId='" + SafeGet(() => element.CurrentAutomationId) + "'");

            IUIAutomationTextRange? range = TryGetCaretRange(element, source) ?? TryGetSelectionRange(element, source);

            range ??= TryGetCaretFromDescendants(automation, element, source);

            if (range == null)
            {
                EngineLog.Info("UIA-DEBUG[" + source + "]: no text range (neither caret nor selection) anywhere in the subtree.");
                return null;
            }

            double[] rects;
            try
            {
                rects = (double[])range.GetBoundingRectangles();
            }
            catch (Exception ex)
            {
                EngineLog.Warn("UIA-DEBUG[" + source + "]: GetBoundingRectangles failed: " + ex.Message);
                return null;
            }

            if (rects == null || rects.Length < 4)
            {
                EngineLog.Info(
                    "UIA-DEBUG[" + source + "]: GetBoundingRectangles returned an empty result (length=" +
                    (rects?.Length ?? 0) + ").");
                return null;
            }

            double x = rects[0];
            double y = rects[1];
            double width = rects[2];
            double height = rects[3];

            Point result = new((int)Math.Round(x), (int)Math.Round(y + height));

            EngineLog.Info(
                "UIA-DEBUG[" + source + "]: rect=(" + x + "," + y + "," + width + "," + height + ")" +
                " screenPoint=" + PointToString(result) +
                " mousePoint=" + PointToString(System.Windows.Forms.Cursor.Position));

            return result;
        }

        private static IUIAutomationTextRange? TryGetCaretFromDescendants(IUIAutomation automation, IUIAutomationElement root, string source)
        {
            const int maxNodes = 300;
            Stopwatch budget = Stopwatch.StartNew();
            TimeSpan maxDuration = TimeSpan.FromMilliseconds(80);

            IUIAutomationTreeWalker walker;
            try
            {
                walker = automation.RawViewWalker;
            }
            catch (Exception ex)
            {
                EngineLog.Info("UIA-DEBUG[" + source + "]: RawViewWalker unavailable: " + ex.Message);
                return null;
            }

            Queue<IUIAutomationElement> queue = new();
            EnqueueChildren(walker, root, queue, source);

            int visited = 0;
            while (queue.Count > 0)
            {
                if (visited >= maxNodes || budget.Elapsed > maxDuration)
                {
                    EngineLog.Info(
                        "UIA-DEBUG[" + source + "]: subtree search aborted (nodes=" + visited +
                        ", time=" + budget.ElapsedMilliseconds + "ms).");
                    return null;
                }

                IUIAutomationElement current = queue.Dequeue();
                visited++;

                IUIAutomationTextRange? range = TryGetCaretRange(current, source, requireActive: true) ?? TryGetSelectionRange(current, source);
                if (range != null)
                {
                    EngineLog.Info(
                        "UIA-DEBUG[" + source + "]: caret found in the subtree after " + visited +
                        " nodes (" + budget.ElapsedMilliseconds + "ms).");
                    return range;
                }

                EnqueueChildren(walker, current, queue, source);
            }

            return null;
        }

        private static void EnqueueChildren(IUIAutomationTreeWalker walker, IUIAutomationElement parent, Queue<IUIAutomationElement> queue, string source)
        {
            IUIAutomationElement? child;
            try
            {
                child = walker.GetFirstChildElement(parent);
            }
            catch (Exception ex)
            {
                EngineLog.Info("UIA-DEBUG[" + source + "]: GetFirstChildElement failed: " + ex.Message);
                return;
            }

            while (child != null)
            {
                queue.Enqueue(child);

                try
                {
                    child = walker.GetNextSiblingElement(child);
                }
                catch (Exception ex)
                {
                    EngineLog.Info("UIA-DEBUG[" + source + "]: GetNextSiblingElement failed: " + ex.Message);
                    child = null;
                }
            }
        }

        private static string SafeGet(Func<string> getter)
        {
            try
            {
                return getter() ?? "";
            }
            catch (Exception ex)
            {
                return "<err:" + ex.Message + ">";
            }
        }

        private static IUIAutomationTextRange? TryGetCaretRange(IUIAutomationElement element, string source, bool requireActive = false)
        {
            object pattern;
            try
            {
                pattern = element.GetCurrentPattern(UIA_TextPattern2Id);
            }
            catch (Exception ex)
            {
                EngineLog.Info("UIA-DEBUG[" + source + "]: GetCurrentPattern(TextPattern2) failed: " + ex.Message);
                return null;
            }

            if (pattern is not IUIAutomationTextPattern2 textPattern2)
            {
                EngineLog.Info("UIA-DEBUG[" + source + "]: the element exposes no TextPattern2.");
                return null;
            }

            try
            {
                IUIAutomationTextRange range = textPattern2.GetCaretRange(out int isActive);
                EngineLog.Info($"UIA-DEBUG[{source}]: GetCaretRange isActive={isActive} range={(range == null ? "null" : "ok")}");

                if (requireActive && isActive == 0)
                {
                    return null;
                }

                return range;
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-DEBUG[{source}]: GetCaretRange failed: {ex.Message}");
                return null;
            }
        }

        private static IUIAutomationTextRange? TryGetSelectionRange(IUIAutomationElement element, string source)
        {
            object pattern;
            try
            {
                pattern = element.GetCurrentPattern(UIA_TextPatternId);
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-DEBUG[{source}]: GetCurrentPattern(TextPattern) failed: {ex.Message}");
                return null;
            }

            if (pattern is not IUIAutomationTextPattern textPattern)
            {
                EngineLog.Info($"UIA-DEBUG[{source}]: the element exposes no TextPattern.");
                return null;
            }

            try
            {
                IUIAutomationTextRangeArray selection = textPattern.GetSelection();
                int length = selection?.Length ?? 0;
                EngineLog.Info($"UIA-DEBUG[{source}]: GetSelection length={length}");
                return length > 0 ? selection!.GetElement(0) : null;
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-DEBUG[{source}]: GetSelection failed: {ex.Message}");
                return null;
            }
        }

        private static string PointToString(Point p) => "(" + p.X + "," + p.Y + ")";

        private static string Hex(IntPtr value) => value == IntPtr.Zero ? "0" : value.ToInt64().ToString("X");

        private static string DescribeWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return "-";
            }

            StringBuilder className = new(256);
            GetClassName(hwnd, className, className.Capacity);

            StringBuilder title = new(256);
            GetWindowText(hwnd, title, title.Capacity);

            return className + "/'" + title + "'";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GuiThreadInfo
        {
            public int cbSize;
            public uint flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public Rect rcCaret;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo lpgui);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    }
}
