using System.Runtime.InteropServices;

namespace OneNoteMomtion.Server.OneNote
{
    [ComImport]
    [Guid("8e8304b8-cbd1-44f8-b0e8-89c625b2002e")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface IOneNoteWindow
    {
        void GetWindowHandle();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetCurrentPageId();
    }
}
