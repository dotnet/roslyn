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
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891"), SuppressUnmanagedCodeSecurity]
    [GeneratedWhenPossibleComInterface]
    internal partial interface IClrRuntimeInfo
    {
        // Unused methods stubbed to preserve vtable layout.
        // Methods using StringBuilder are not compatible with GeneratedComInterface.
        [PreserveSig]
        int __GetVersionString(/*StringBuilder buffer, ref int bufferLength*/);
        [PreserveSig]
        int __GetRuntimeDirectory(/*StringBuilder buffer, ref int bufferLength*/);
        [PreserveSig]
        int __IsLoaded(/*IntPtr processHandle*/);
        [PreserveSig]
        int __LoadErrorString(/*int resourceId, StringBuilder buffer, ref int bufferLength*/);
        [PreserveSig]
        int __LoadLibrary(/*string dllName*/);
        [PreserveSig]
        int __GetProcAddress(/*string procName*/);

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetInterface(
            in Guid coClassId,
            in Guid interfaceId);
    }
}
