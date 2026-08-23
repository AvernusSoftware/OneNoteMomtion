using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Interop.UIAutomationClient;
using OneNoteMomtion.Core.Hierarchy;

namespace OneNoteMomtion.Server.Ui
{
    internal sealed class NavigationPaneAutomation
    {
        private const string NodeClassName = "NetUINavBarTreeViewItem";

        private const int SearchNodeBudget = 800;

        private const int ExpandAttempts = 3;
        private const int ExpandPollMilliseconds = 50;

        private const int LocateAttempts = 3;
        private const int LocateRetryMilliseconds = 150;

        private const int CollapsePasses = 3;

        private const int MaxNodeDepth = 32;

        private const int BestScore = 3;

        private static readonly TimeSpan TotalBudget = TimeSpan.FromSeconds(6);
        private static readonly TimeSpan TotalCollapseBudget = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ExpandWait = TimeSpan.FromMilliseconds(700);
        private static readonly TimeSpan CollapseWait = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan SearchBudget = TimeSpan.FromMilliseconds(400);

        private readonly int? _hostProcessId;

        public NavigationPaneAutomation(int? hostProcessId)
        {
            _hostProcessId = hostProcessId;
        }

        public IntPtr FindOneNoteWindow()
        {
            return OneNoteWindowFinder.TryFindMainWindow(_hostProcessId);
        }

        public bool TryReveal(HierarchyPath path)
        {
            if (path is null)
            {
                return false;
            }

            IntPtr window = FindOneNoteWindow();
            if (window == IntPtr.Zero)
            {
                EngineLog.Warn("UIA-NAV: no OneNote window to reveal the page in.");
                return false;
            }

            if (!ForegroundActivation.Take(window))
            {
                EngineLog.Info("UIA-NAV: OneNote did not come back to the foreground - carrying on anyway.");
            }

            try
            {
                return Reveal(window, path);
            }
            catch (Exception ex)
            {
                EngineLog.Error("UIA-NAV: revealing the page in the navigation pane failed.", ex);
                return false;
            }
        }

        public bool TryCollapseAll(out int collapsed)
        {
            collapsed = 0;

            IntPtr window = FindOneNoteWindow();
            if (window == IntPtr.Zero)
            {
                EngineLog.Warn("UIA-NAV: no OneNote window to collapse the navigation pane in.");
                return false;
            }

            if (!ForegroundActivation.Take(window))
            {
                EngineLog.Info("UIA-NAV: OneNote did not come back to the foreground - carrying on anyway.");
            }

            try
            {
                return CollapseAll(window, out collapsed);
            }
            catch (Exception ex)
            {
                EngineLog.Error("UIA-NAV: collapsing the navigation pane failed.", ex);
                return false;
            }
        }

        private bool Reveal(IntPtr window, HierarchyPath path)
        {
            IUIAutomation automation = new CUIAutomationClass();
            IUIAutomationElement root = automation.ElementFromHandle(window);

            EngineLog.Info("UIA-NAV: revealing \"" + path.ToBreadcrumb() + "\" in " + OneNoteWindowFinder.Describe(window) + ".");

            Stopwatch budget = Stopwatch.StartNew();
            IUIAutomationElement current = root;
            IUIAutomationElement? last = null;

            foreach (HierarchyNode container in path.Containers)
            {
                if (budget.Elapsed > TotalBudget)
                {
                    EngineLog.Warn("UIA-NAV: ran out of time before reaching \"" + container.Name + "\".");
                    return false;
                }

                IUIAutomationElement? node = Locate(automation, current, root, container.Name, budget);
                if (node is null)
                {
                    EngineLog.Warn("UIA-NAV: \"" + container.Name + "\" (" + container.Kind + ") is not in the navigation pane.");
                    return false;
                }

                EngineLog.Info("UIA-NAV: matched " + container.Kind + " -> " + UiaHelpers.Describe(node));

                Expand(node, budget);

                current = node;
                last = node;
            }

            if (last is null)
            {
                EngineLog.Warn("UIA-NAV: the path has no containers to follow.");
                return false;
            }

            TrySelect(last);
            TryScrollIntoView(last);

            bool visible = !UiaHelpers.IsOffscreen(last) && UiaHelpers.HasArea(last);
            EngineLog.Info("UIA-NAV: finished in " + budget.ElapsedMilliseconds + " ms, target visible=" + visible + ".");

            return visible;
        }

        private bool CollapseAll(IntPtr window, out int collapsed)
        {
            IUIAutomation automation = new CUIAutomationClass();
            IUIAutomationElement root = automation.ElementFromHandle(window);

            EngineLog.Info("UIA-NAV: collapsing the navigation pane in " + OneNoteWindowFinder.Describe(window) + ".");

            Stopwatch budget = Stopwatch.StartNew();
            collapsed = 0;

            for (int pass = 1; pass <= CollapsePasses; pass++)
            {
                List<IUIAutomationElement> open = FindOpenNodes(automation, root);
                if (open.Count == 0)
                {
                    EngineLog.Info("UIA-NAV: the pane is closed - " + collapsed + " node(s) collapsed in " + budget.ElapsedMilliseconds + " ms.");
                    return true;
                }

                EngineLog.Info("UIA-NAV: pass " + pass + "/" + CollapsePasses + " found " + open.Count + " open node(s).");

                foreach (IUIAutomationElement node in Deepest(automation, open))
                {
                    if (budget.Elapsed > TotalCollapseBudget)
                    {
                        EngineLog.Warn("UIA-NAV: ran out of time with " + collapsed + " node(s) collapsed.");
                        return collapsed > 0;
                    }

                    if (Collapse(node, budget))
                    {
                        collapsed++;
                    }
                }
            }

            EngineLog.Warn($"UIA-NAV: {collapsed} node(s) collapsed in {budget.ElapsedMilliseconds} ms, {FindOpenNodes(automation, root).Count} still open.");
            return collapsed > 0;
        }

        private static List<IUIAutomationElement> FindOpenNodes(IUIAutomation automation, IUIAutomationElement root)
        {
            List<IUIAutomationElement> open = [];

            IUIAutomationElementArray matches;
            try
            {
                IUIAutomationCondition condition = automation.CreatePropertyCondition(UiaHelpers.ClassNamePropertyId, NodeClassName);
                matches = root.FindAll(TreeScope.TreeScope_Descendants, condition);
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-NAV: FindAll for the pane nodes failed: {ex.Message}");
                return open;
            }

            for (int index = 0; index < SafeLength(matches); index++)
            {
                IUIAutomationElement candidate;
                try
                {
                    candidate = matches.GetElement(index);
                }
                catch (Exception)
                {
                    continue;
                }

                IUIAutomationExpandCollapsePattern? pattern = UiaHelpers.TryGetPattern<IUIAutomationExpandCollapsePattern>(candidate, UiaHelpers.ExpandCollapsePatternId);
                if (pattern is null || ReadState(pattern) != ExpandCollapseState.ExpandCollapseState_Expanded)
                {
                    continue;
                }

                open.Add(candidate);
            }

            return open;
        }

        private static List<IUIAutomationElement> Deepest(IUIAutomation automation, List<IUIAutomationElement> nodes)
        {
            IUIAutomationTreeWalker? walker;
            try
            {
                walker = automation.RawViewWalker;
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-NAV: RawViewWalker unavailable: {ex.Message}");
                return nodes;
            }

            List<(IUIAutomationElement Node, int Depth)> ranked = [];
            foreach (IUIAutomationElement node in nodes)
            {
                ranked.Add((node, DepthOf(walker, node)));
            }

            ranked.Sort((left, right) => right.Depth.CompareTo(left.Depth));

            List<IUIAutomationElement> ordered = [];
            foreach ((IUIAutomationElement Node, int Depth) entry in ranked)
            {
                ordered.Add(entry.Node);
            }

            return ordered;
        }

        private static int DepthOf(IUIAutomationTreeWalker walker, IUIAutomationElement node)
        {
            IUIAutomationElement? current = node;
            int depth = 0;

            while (depth < MaxNodeDepth)
            {
                try
                {
                    current = walker.GetParentElement(current);
                }
                catch (Exception)
                {
                    break;
                }

                if (current is null || !string.Equals(UiaHelpers.ClassNameOf(current), NodeClassName, StringComparison.Ordinal))
                {
                    break;
                }

                depth++;
            }

            return depth;
        }

        private static bool Collapse(IUIAutomationElement node, Stopwatch budget)
        {
            IUIAutomationExpandCollapsePattern? pattern = UiaHelpers.TryGetPattern<IUIAutomationExpandCollapsePattern>(node, UiaHelpers.ExpandCollapsePatternId);
            if (pattern is null)
            {
                return false;
            }

            if (ReadState(pattern) != ExpandCollapseState.ExpandCollapseState_Expanded)
            {
                return false;
            }

            try
            {
                pattern.Collapse();
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-NAV: Collapse failed: {ex.Message}");
                return false;
            }

            return WaitForState(pattern, ExpandCollapseState.ExpandCollapseState_Collapsed, budget, TotalCollapseBudget, CollapseWait);
        }

        private static IUIAutomationElement? Locate(IUIAutomation automation, IUIAutomationElement scope, IUIAutomationElement root, string name, Stopwatch budget)
        {
            for (int attempt = 1; attempt <= LocateAttempts; attempt++)
            {
                IUIAutomationElement? node = FindByName(automation, scope, name, budget);
                if (node is not null)
                {
                    return node;
                }

                if (!ReferenceEquals(scope, root))
                {
                    EngineLog.Info($"UIA-NAV: \"{name}\" is not below the previous match - searching the whole window.");

                    node = FindByName(automation, root, name, budget);
                    if (node is not null)
                    {
                        return node;
                    }
                }

                if (attempt >= LocateAttempts || budget.Elapsed > TotalBudget)
                {
                    break;
                }

                EngineLog.Info($"UIA-NAV: \"{name}\" not there yet, retrying ({attempt}/{LocateAttempts}).");
                Thread.Sleep(LocateRetryMilliseconds);
            }

            return null;
        }

        private static IUIAutomationElement? FindByName(IUIAutomation automation, IUIAutomationElement scope, string name, Stopwatch budget)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            IUIAutomationElement? exact = FindExact(automation, scope, name);
            if (exact is not null)
            {
                return exact;
            }

            return FindByPrefix(automation, scope, name, budget);
        }

        private static IUIAutomationElement? FindExact(IUIAutomation automation, IUIAutomationElement scope, string name)
        {
            IUIAutomationElementArray matches;
            try
            {
                IUIAutomationCondition condition = automation.CreatePropertyCondition(UiaHelpers.NamePropertyId, name);
                matches = scope.FindAll(TreeScope.TreeScope_Descendants, condition);
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-NAV: FindAll for \"{name}\" failed: {ex.Message}");
                return null;
            }

            IUIAutomationElement? best = null;
            int bestScore = -1;

            for (int index = 0; index < SafeLength(matches); index++)
            {
                IUIAutomationElement candidate;
                try
                {
                    candidate = matches.GetElement(index);
                }
                catch (Exception)
                {
                    continue;
                }

                int score = Score(candidate);
                if (score <= bestScore)
                {
                    continue;
                }

                best = candidate;
                bestScore = score;

                if (score == BestScore)
                {
                    break;
                }
            }

            return best;
        }

        private static int Score(IUIAutomationElement element)
        {
            bool painted = !UiaHelpers.IsOffscreen(element) && UiaHelpers.HasArea(element);
            bool treeLike = UiaHelpers.TryGetPattern<IUIAutomationExpandCollapsePattern>(element, UiaHelpers.ExpandCollapsePatternId) is not null
                || UiaHelpers.TryGetPattern<IUIAutomationSelectionItemPattern>(element, UiaHelpers.SelectionItemPatternId) is not null;

            return (painted ? 2 : 0) + (treeLike ? 1 : 0);
        }

        private static IUIAutomationElement? FindByPrefix(IUIAutomation automation, IUIAutomationElement scope, string name, Stopwatch budget)
        {
            IUIAutomationTreeWalker walker;
            try
            {
                walker = automation.RawViewWalker;
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-NAV: RawViewWalker unavailable: {ex.Message}");
                return null;
            }

            Stopwatch search = Stopwatch.StartNew();
            Queue<IUIAutomationElement> queue = new();
            Enqueue(walker, scope, queue);

            IUIAutomationElement? best = null;
            int bestScore = -1;
            int visited = 0;

            while (queue.Count > 0)
            {
                if (visited >= SearchNodeBudget || search.Elapsed > SearchBudget || budget.Elapsed > TotalBudget)
                {
                    EngineLog.Info($"UIA-NAV: prefix search for \"{name}\" stopped after {visited} nodes.");
                    break;
                }

                IUIAutomationElement candidate = queue.Dequeue();
                visited++;

                string candidateName = UiaHelpers.NameOf(candidate);
                if (candidateName.Length > 0 && candidateName.StartsWith(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    int score = Score(candidate);
                    if (score > bestScore)
                    {
                        best = candidate;
                        bestScore = score;
                    }

                    if (score == BestScore)
                    {
                        break;
                    }
                }

                Enqueue(walker, candidate, queue);
            }

            if (best is not null)
            {
                EngineLog.Info($"UIA-NAV: \"{name}\" matched the pane label \"{UiaHelpers.NameOf(best)}\".");
            }

            return best;
        }

        private static void Enqueue(IUIAutomationTreeWalker walker, IUIAutomationElement parent, Queue<IUIAutomationElement> queue)
        {
            IUIAutomationElement? child;
            try
            {
                child = walker.GetFirstChildElement(parent);
            }
            catch (Exception)
            {
                return;
            }

            while (child is not null)
            {
                queue.Enqueue(child);

                try
                {
                    child = walker.GetNextSiblingElement(child);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private static void Expand(IUIAutomationElement element, Stopwatch budget)
        {
            IUIAutomationExpandCollapsePattern? pattern = UiaHelpers.TryGetPattern<IUIAutomationExpandCollapsePattern>(element, UiaHelpers.ExpandCollapsePatternId);
            if (pattern is null)
            {
                return;
            }

            for (int attempt = 1; attempt <= ExpandAttempts; attempt++)
            {
                ExpandCollapseState state = ReadState(pattern);
                if (state is ExpandCollapseState.ExpandCollapseState_Expanded or ExpandCollapseState.ExpandCollapseState_LeafNode)
                {
                    return;
                }

                try
                {
                    pattern.Expand();
                }
                catch (Exception ex)
                {
                    EngineLog.Info($"UIA-NAV: Expand failed: {ex.Message}");
                    return;
                }

                if (WaitForState(pattern, ExpandCollapseState.ExpandCollapseState_Expanded, budget, TotalBudget, ExpandWait))
                {
                    return;
                }

                EngineLog.Warn($"UIA-NAV: the node is still {ReadState(pattern)} after Expand attempt {attempt}/{ExpandAttempts}.");

                if (budget.Elapsed > TotalBudget)
                {
                    return;
                }
            }
        }

        private static bool WaitForState(IUIAutomationExpandCollapsePattern pattern, ExpandCollapseState wanted, Stopwatch budget, TimeSpan totalBudget, TimeSpan patience)
        {
            Stopwatch wait = Stopwatch.StartNew();

            while (wait.Elapsed < patience && budget.Elapsed < totalBudget)
            {
                Thread.Sleep(ExpandPollMilliseconds);

                if (ReadState(pattern) == wanted)
                {
                    return true;
                }
            }

            return false;
        }

        private static ExpandCollapseState ReadState(IUIAutomationExpandCollapsePattern pattern)
        {
            try
            {
                return pattern.CurrentExpandCollapseState;
            }
            catch (Exception)
            {
                return ExpandCollapseState.ExpandCollapseState_Collapsed;
            }
        }

        private static void TrySelect(IUIAutomationElement element)
        {
            IUIAutomationSelectionItemPattern? pattern = UiaHelpers.TryGetPattern<IUIAutomationSelectionItemPattern>(element, UiaHelpers.SelectionItemPatternId);
            if (pattern is null)
            {
                return;
            }

            try
            {
                if (pattern.CurrentIsSelected != 0)
                {
                    return;
                }

                pattern.Select();
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-NAV: Select failed: {ex.Message}");
            }
        }

        private static void TryScrollIntoView(IUIAutomationElement element)
        {
            IUIAutomationScrollItemPattern? pattern = UiaHelpers.TryGetPattern<IUIAutomationScrollItemPattern>(element, UiaHelpers.ScrollItemPatternId);
            if (pattern is null)
            {
                return;
            }

            try
            {
                pattern.ScrollIntoView();
            }
            catch (Exception ex)
            {
                EngineLog.Info($"UIA-NAV: ScrollIntoView failed: {ex.Message}");
            }
        }

        private static int SafeLength(IUIAutomationElementArray? array)
        {
            try
            {
                return array?.Length ?? 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}