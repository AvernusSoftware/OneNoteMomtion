using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace OneNoteMomtion.Server.OneNote
{
    internal sealed class OneNoteSession
    {
        private const int MaxAttempts = 3;
        private const int RetryDelayMilliseconds = 120;

        private static readonly DateTime OleZeroDate = new(1899, 12, 30);

        private readonly IApplication _application;

        public OneNoteSession(IApplication application)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
        }

        public string? GetCurrentPageId()
        {
            return WithRetry("CurrentPageId", () =>
            {
                string pageId = _application.GetWindows().GetCurrentWindow().GetCurrentPageId();
                return string.IsNullOrEmpty(pageId) ? null : pageId;
            });
        }

        public string? GetHierarchyXml(string? startNodeId, HierarchyScope scope)
        {
            return WithRetry("GetHierarchy", () =>
            {
                _application.GetHierarchy(startNodeId, scope, out string hierarchyXml, XmlSchema.Current);
                return hierarchyXml;
            });
        }

        public string? GetPageXml(string pageId, PageInfo pageInfo = PageInfo.Selection)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                return null;
            }

            return WithRetry("GetPageContent", () =>
            {
                _application.GetPageContent(pageId, out string pageXml, pageInfo, XmlSchema.Current);
                return pageXml;
            });
        }

        public bool UpdatePageXml(string pageXml)
        {
            if (string.IsNullOrEmpty(pageXml))
            {
                return false;
            }

            return WithRetry("UpdatePageContent", () =>
            {
                _application.UpdatePageContent(pageXml, OleZeroDate, XmlSchema.Current, force: true);
                return true;
            });
        }

        public void NavigateTo(string pageId, string objectId)
        {
            if (string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(objectId))
            {
                return;
            }

            WithRetry("NavigateTo", () => { _application.NavigateTo(pageId, objectId, newWindow: false); return true; });
        }

        public void NavigateToPage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                return;
            }

            WithRetry("NavigateToPage", () => { _application.NavigateTo(pageId, string.Empty, newWindow: false); return true; });
        }

        public bool TryGetCurrentPage(out string pageId, out string pageXml)
        {
            pageId = string.Empty;
            pageXml = string.Empty;

            string? id = GetCurrentPageId();
            if (id is null)
            {
                return false;
            }

            string? xml = GetPageXml(id);
            if (string.IsNullOrEmpty(xml))
            {
                return false;
            }

            pageId = id;
            pageXml = xml;
            return true;
        }

        private T? WithRetry<T>(string operation, Func<T> action)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return action();
                }
                catch (COMException ex) when (!OneNoteConnection.IsDisconnected(ex))
                {
                    if (attempt >= MaxAttempts)
                    {
                        EngineLog.Error($"{operation} failed after {attempt} attempts (HRESULT 0x{ex.HResult:X8}).", ex);

                        return default;
                    }

                    EngineLog.Warn($"{operation} returned 0x{ex.HResult:X8}, retrying ({attempt}/{MaxAttempts}).");
                    Thread.Sleep(RetryDelayMilliseconds * attempt);
                }
            }
        }
    }
}