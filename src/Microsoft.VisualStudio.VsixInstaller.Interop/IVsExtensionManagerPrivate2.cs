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
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaNames,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaVersions,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaAuthors,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaExtensionIDs);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetExtensionProperties(
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaNames,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaVersions,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaAuthors,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaContentLocations,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out Array prgsaExtensionIDs);

        [MethodImpl(MethodImplOptions.InternalCall)]
        ulong GetLastWriteTime([In] [MarshalAs(UnmanagedType.LPWStr)] string szContentTypeName);
    }
}
