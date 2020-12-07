// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

#pragma warning disable CS0436 // Type conflicts with imported type: SuppressUnmanagedCodeSecurity

namespace Microsoft.CodeAnalysis.Interop
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D332DB9E-B9B3-4125-8207-A14884F53216"), SuppressUnmanagedCodeSecurity]
    internal interface IClrMetaHost
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetRuntime(
            [In, MarshalAs(UnmanagedType.LPWStr)] string version,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId);

        [PreserveSig]
        int GetVersionFromFile(
            [In, MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int bufferLength);

        [return: MarshalAs(UnmanagedType.Interface)]
        object EnumerateInstalledRuntimes();

        [return: MarshalAs(UnmanagedType.Interface)]
        object EnumerateLoadedRuntimes(
            [In] IntPtr processHandle);

        // Placeholder for RequestRuntimeLoadedNotification
        [PreserveSig]
        int Reserved01(
            [In] IntPtr reserved1);

        [return: MarshalAs(UnmanagedType.Interface)]
        object QueryLegacyV2RuntimeBinding(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId);

        void ExitProcess(
            [In, MarshalAs(UnmanagedType.U4)] int exitCode);
    }
}

