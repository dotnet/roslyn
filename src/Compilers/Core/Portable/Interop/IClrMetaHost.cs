// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Security;

#pragma warning disable CS0436 // Type conflicts with imported type: SuppressUnmanagedCodeSecurity

namespace Microsoft.CodeAnalysis.Interop
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D332DB9E-B9B3-4125-8207-A14884F53216"), SuppressUnmanagedCodeSecurity]
    [GeneratedWhenPossibleComInterface]
    internal partial interface IClrMetaHost
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetRuntime(
            [MarshalAs(UnmanagedType.LPWStr)] string version,
            in Guid interfaceId);

        // Remaining methods are unused and stubbed to preserve vtable layout.
        // Methods using StringBuilder are not compatible with GeneratedComInterface.
        [PreserveSig]
        int __GetVersionFromFile(/*string filePath, StringBuilder buffer, ref int bufferLength*/);
        [PreserveSig]
        int __EnumerateInstalledRuntimes();
        [PreserveSig]
        int __EnumerateLoadedRuntimes(/*IntPtr processHandle*/);
        [PreserveSig]
        int __Reserved01(/*IntPtr reserved1*/);
        [PreserveSig]
        int __QueryLegacyV2RuntimeBinding(/*Guid interfaceId*/);
        [PreserveSig]
        int __ExitProcess(/*int exitCode*/);
    }
}
