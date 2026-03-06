using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DnDBattle.Data.Services
{
    // ============================================================
    // HRESULT constants used to check dialog return values.
    // ============================================================
    // S_OK   = The user selected a folder successfully.
    // S_FALSE / ERROR_CANCELLED = The user pressed Cancel.
    // ============================================================

    /// <summary>
    /// A self-contained folder-browser dialog that uses the Windows Shell
    /// COM API (IFileOpenDialog) directly via interop.
    /// No reference to System.Windows.Forms is required.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class NativeFolderBrowser
    {
        // CLSID for the FileOpenDialog COM coclass.
        // This is the "object" Windows will create behind the scenes.
        private const string CLSID_FileOpenDialog = "DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7";

        // IID for the IFileOpenDialog COM interface.
        private const string IID_IFileOpenDialog = "D57C7288-D4AD-4768-BE02-9D969532D960";

        // IID for the IShellItem COM interface.
        // This represents the item (folder) the user picked.
        private const string IID_IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";

        /// <summary>
        /// A subset of the FILEOPENDIALOGOPTIONS flags.
        /// We only declare the ones we actually need.
        /// </summary>
        [Flags]
        private enum FOS : uint
        {
            /// <summary>
            /// Causes the dialog to return only file-system items
            /// (i.e., items with a real path on disk).
            /// </summary>
            FOS_FORCEFILESYSTEM = 0x00000040,

            /// <summary>
            /// The key flag: tells the dialog to pick *folders*
            /// instead of files.
            /// </summary>
            FOS_PICKFOLDERS = 0x00000020,
        }

        // SIGDN "Shell Item Display Name"
        private enum SIGDN : uint
        {
            /// <summary>
            /// Returns the full file-system path, e.g.
            /// "C:\Users\You\Documents".
            /// </summary>
            SIGDN_FILESYSPATH = 0x80058000,
        }

        // Represents ONE shell object (file or folder).
        // Ive only gotten GetDisplayName.
        [ComImport]
        [Guid(IID_IShellItem)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(
                IntPtr pbc,
                ref Guid bhid,
                ref Guid riid,
                out IntPtr ppv);

            void GetParent(out IShellItem ppsi);

            void GetDisplayName(
                SIGDN sigdnName,
                [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport]
        [Guid(IID_IFileOpenDialog)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            // === IModalWindow ===
            // Show displays the dialog.  hwndOwner can be IntPtr.Zero
            // for no parent window.
            // Returns S_OK if a folder was chosen, or an error/cancel
            // HRESULT otherwise.
            [PreserveSig]
            int Show(IntPtr hwndOwner);

            // === IFileDialog methods (vtable placeholders) ===
            // We must declare every method in order so that the COM
            // vtable lines up, even if we don't call most of them.

            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);

            void SetOptions(FOS fos);

            void GetOptions(out FOS pfos);

            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);

            void GetResult(out IShellItem ppsi);

            void AddPlace(IShellItem psi, int fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pdzDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);

            void GetResult(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        /// <summary>
        /// Opens a native folder-picker dialog (Vista+ style).
        /// </summary>
        /// <param name="selectedPath">
        /// The full path the user selected, or <c>null</c> if
        /// the dialog was cancelled.
        /// </param>
        /// <returns>
        /// <c>true</c> if a folder was selected;
        /// <c>false</c> if the user cancelled.
        /// </returns>
        public static bool ShowDialog(out string selectedPath)
        {
            selectedPath = null;

            Type dialogType = Type.GetTypeFromCLSID(new Guid(CLSID_FileOpenDialog));
            object dialogObj = Activator.CreateInstance(dialogType);

            IFileOpenDialog dialog = (IFileOpenDialog)dialogObj;

            try
            {
                dialog.GetOptions(out FOS options);

                dialog.SetOptions(options | FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);

                int hr = dialog.Show(IntPtr.Zero);

                if (hr != 0)
                    return false;

                dialog.GetResult(out IShellItem item);

                item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);

                selectedPath = path;
                return true;
            }
            finally
            {
                Marshal.ReleaseComObject(dialogObj);
            }
        }
    }
}
