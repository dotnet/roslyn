// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [ComImport]
    [Guid("753E55C6-E779-4A7A-BCD1-FD87181D52C0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsExtensionManagerPrivate
    {
        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int GetEnabledExtensionContentLocations(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string szContentTypeName,
            [In] uint cContentLocations,
            [Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 1)] string[] rgbstrContentLocations,
            [Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 1)] string[] rgbstrUniqueExtensionStrings,
            out uint pcContentLocations);

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int GetEnabledExtensionContentLocationsWithNames(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string szContentTypeName,
            [In] uint cContentLocations,
            [Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 1)] string[] rgbstrContentLocations,
            [Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 1)] string[] rgbstrUniqueExtensionStrings,
            [Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 1)] string[] rgbstrExtensionNames,
            out uint pcContentLocations);

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int GetDisabledExtensionContentLocations(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string szContentTypeName,
            [In] uint cContentLocations,
            [Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 1)] string[] rgbstrContentLocations,
            [Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 1)] string[] rgbstrUniqueExtensionStrings,
            out uint pcContentLocations);

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int GetLastConfigurationChange([Out] [MarshalAs(UnmanagedType.LPArray)] DateTime[] pTimestamp);

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int LogAllInstalledExtensions();

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int GetUniqueExtensionString(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string szExtensionIdentifier,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrUniqueString);
    }
}
