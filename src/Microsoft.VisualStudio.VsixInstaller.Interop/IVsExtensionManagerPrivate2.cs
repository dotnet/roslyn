// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("6B741746-E3C9-434A-9E20-6E330D88C7F6")]
    public interface IVsExtensionManagerPrivate2
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetAssetProperties(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string szAssetTypeName,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaNames,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaVersions,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaAuthors,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaExtensionIDs);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetExtensionProperties(
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaNames,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaVersions,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaAuthors,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaContentLocations,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] prgsaExtensionIDs);

        [MethodImpl(MethodImplOptions.InternalCall)]
        ulong GetLastWriteTime([In] [MarshalAs(UnmanagedType.LPWStr)] string szContentTypeName);
    }
}
