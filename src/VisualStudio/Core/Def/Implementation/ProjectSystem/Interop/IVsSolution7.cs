using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop
{
    [ComImport]
    [Guid("D32B0C42-8AEE-4772-B5C3-04565CDA5A47")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsSolution7
    {
        void OpenFolder([MarshalAs(UnmanagedType.LPWStr)]string folderPath);
        void CloseFolder([MarshalAs(UnmanagedType.LPWStr)]string folderPath);
        bool IsSolutionLoadDeferred();
    }
}
