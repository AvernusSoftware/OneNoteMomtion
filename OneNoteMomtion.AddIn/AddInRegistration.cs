using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace OneNoteMomtion.AddIn
{
    internal static class AddInRegistration
    {
        private const string AddInKeyPath = @"Software\Microsoft\Office\OneNote\AddIns\" + AddIn.ProgIdValue;

        private const int LoadAtStartup = 3;

        [ComRegisterFunction]
        public static void Register(Type type)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(AddInKeyPath))
                {
                    if (key == null)
                    {
                        return;
                    }

                    key.SetValue("FriendlyName", "OneNoteMomtion", RegistryValueKind.String);
                    key.SetValue("Description", "Editing tools for OneNote: Markdown shortcuts, styles, syntax highlighting and text case.", RegistryValueKind.String);
                    key.SetValue("LoadBehavior", LoadAtStartup, RegistryValueKind.DWord);
                }

                ShimLog.Info("Registered the add-in under HKCU\\" + AddInKeyPath + ".");
            }
            catch (Exception ex)
            {
                ShimLog.Error("Registration under HKCU failed.", ex);
                throw;
            }
        }

        [ComUnregisterFunction]
        public static void Unregister(Type type)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(AddInKeyPath, false);
                ShimLog.Info("Unregistered the add-in.");
            }
            catch (Exception ex)
            {
                ShimLog.Error("Unregistration from HKCU failed.", ex);
            }
        }
    }
}