using System.Xml.Linq;

namespace OneNoteMomtion.Core.Xml
{
    public static class OneNoteNs
    {
        public const string OneNote2013 = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        public const string OneNote2010 = "http://schemas.microsoft.com/office/onenote/2010/onenote";

        public static readonly XNamespace One = XNamespace.Get(OneNote2013);

        public static bool IsOneNoteNamespace(XNamespace ns)
        {
            if (ns == null)
            {
                return false;
            }

            string value = ns.NamespaceName;
            return value == OneNote2013 || value == OneNote2010;
        }
    }
}
