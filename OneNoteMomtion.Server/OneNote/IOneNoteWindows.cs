using System.Runtime.InteropServices;

namespace OneNoteMomtion.Server.OneNote
{
    [ComImport]
    [Guid("6d4b9c3e-cc05-493f-85e2-43d1006df96a")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface IOneNoteWindows
    {
        void GetItem();

        void GetCount();

        void GetNewEnum();

        [return: MarshalAs(UnmanagedType.Interface)]
        IOneNoteWindow GetCurrentWindow();
    }
}
