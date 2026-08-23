using System;
using System.Runtime.InteropServices;

namespace OneNoteMomtion.Server.OneNote
{
    [ComImport]
    [Guid("452ac71a-b655-4967-a208-a4cc39dd7949")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface IApplication
    {
        void GetHierarchy([MarshalAs(UnmanagedType.BStr)] string? startNodeId, HierarchyScope scope, [MarshalAs(UnmanagedType.BStr)] out string hierarchyXml, XmlSchema schema);

        void UpdateHierarchy();

        void OpenHierarchy();

        void DeleteHierarchy();

        void CreateNewPage();

        void CloseNotebook();

        void GetHierarchyParent();

        void GetPageContent([MarshalAs(UnmanagedType.BStr)] string pageId, [MarshalAs(UnmanagedType.BStr)] out string pageXml, PageInfo pageInfo, XmlSchema schema);

        void UpdatePageContent([MarshalAs(UnmanagedType.BStr)] string pageChangesXml, DateTime expectedLastModified, XmlSchema schema, [MarshalAs(UnmanagedType.VariantBool)] bool force);

        void GetBinaryPageContent();

        void DeletePageContent();

        void NavigateTo([MarshalAs(UnmanagedType.BStr)] string hierarchyObjectId, [MarshalAs(UnmanagedType.BStr)] string objectId, [MarshalAs(UnmanagedType.VariantBool)] bool newWindow);

        void NavigateToUrl();

        void Publish();

        void OpenPackage();

        void GetHyperlinkToObject();

        void FindPages();

        void FindMeta();

        void GetSpecialLocation();

        void MergeFiles();

        void QuickFiling();

        void SyncHierarchy();

        void SetFilingLocation();

        [return: MarshalAs(UnmanagedType.Interface)]
        IOneNoteWindows GetWindows();
    }
}
