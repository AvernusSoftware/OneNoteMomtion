using System;
using System.Globalization;
using System.Text;
using Interop.UIAutomationClient;

namespace OneNoteMomtion.Server.Ui
{
    internal static class UiaHelpers
    {
        public const int InvokePatternId = 10000;
        public const int ExpandCollapsePatternId = 10005;
        public const int ScrollItemPatternId = 10017;
        public const int SelectionItemPatternId = 10010;
        public const int LegacyIAccessiblePatternId = 10018;

        public const int NamePropertyId = 30005;
        public const int ClassNamePropertyId = 30012;

        public static T? TryGetPattern<T>(IUIAutomationElement? element, int patternId) where T : class
        {
            if (element is null)
            {
                return null;
            }

            try
            {
                return element.GetCurrentPattern(patternId) as T;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string NameOf(IUIAutomationElement element)
        {
            try
            {
                return element.CurrentName ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static string ClassNameOf(IUIAutomationElement element)
        {
            try
            {
                return element.CurrentClassName ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static bool IsOffscreen(IUIAutomationElement element)
        {
            try
            {
                return element.CurrentIsOffscreen != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasArea(IUIAutomationElement element)
        {
            try
            {
                tagRECT rect = element.CurrentBoundingRectangle;
                return rect.right > rect.left && rect.bottom > rect.top;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string Describe(IUIAutomationElement element)
        {
            StringBuilder builder = new();

            builder.Append("name='").Append(Read(() => element.CurrentName)).Append('\'');
            builder.Append(" type='").Append(Read(() => element.CurrentLocalizedControlType)).Append('\'');
            builder.Append(" automationId='").Append(Read(() => element.CurrentAutomationId)).Append('\'');
            builder.Append(" class='").Append(Read(() => element.CurrentClassName)).Append('\'');
            builder.Append(" offscreen=").Append(Read(() => element.CurrentIsOffscreen.ToString(CultureInfo.InvariantCulture)));
            builder.Append(" rect=").Append(Read(() => Format(element.CurrentBoundingRectangle)));
            builder.Append(" patterns=[").Append(SupportedPatterns(element)).Append(']');

            return builder.ToString();
        }

        public static string SupportedPatterns(IUIAutomationElement element)
        {
            StringBuilder builder = new();

            IUIAutomationExpandCollapsePattern? expandCollapse = TryGetPattern<IUIAutomationExpandCollapsePattern>(element, ExpandCollapsePatternId);
            if (expandCollapse is not null)
            {
                Separate(builder).Append("ExpandCollapse(").Append(Read(() => expandCollapse.CurrentExpandCollapseState.ToString())).Append(')');
            }

            IUIAutomationSelectionItemPattern? selectionItem = TryGetPattern<IUIAutomationSelectionItemPattern>(element, SelectionItemPatternId);
            if (selectionItem is not null)
            {
                Separate(builder).Append("SelectionItem(selected=").Append(Read(() => selectionItem.CurrentIsSelected.ToString(CultureInfo.InvariantCulture))).Append(')');
            }

            if (TryGetPattern<IUIAutomationInvokePattern>(element, InvokePatternId) is not null)
            {
                Separate(builder).Append("Invoke");
            }

            if (TryGetPattern<IUIAutomationScrollItemPattern>(element, ScrollItemPatternId) is not null)
            {
                Separate(builder).Append("ScrollItem");
            }

            IUIAutomationLegacyIAccessiblePattern? legacy = TryGetPattern<IUIAutomationLegacyIAccessiblePattern>(element, LegacyIAccessiblePatternId);
            if (legacy is not null)
            {
                Separate(builder).Append("Legacy(role=").Append(Read(() => legacy.CurrentRole.ToString(CultureInfo.InvariantCulture)));
                builder.Append(" state=0x").Append(Read(() => legacy.CurrentState.ToString("X", CultureInfo.InvariantCulture)));
                builder.Append(" action='").Append(Read(() => legacy.CurrentDefaultAction)).Append("')");
            }

            return builder.ToString();
        }

        public static string Read(Func<string> getter)
        {
            try
            {
                return getter() ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"<err:{ex.Message}>";
            }
        }

        private static string Format(tagRECT rect)
        {
            return $"({rect.left},{rect.top},{(rect.right - rect.left)}x{(rect.bottom - rect.top)})";
        }

        private static StringBuilder Separate(StringBuilder builder)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            return builder;
        }
    }
}
