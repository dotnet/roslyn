// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

[ComImport]
[Guid("9AD7EC03-4157-45B4-A999-403D6DB94578")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IVsEnumDebugName
{
    [PreserveSig]
    int Next(uint celt,
        [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface)] IVsDebugName[] rgelt,
        [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)] uint[] pceltFetched);

    [PreserveSig]
    int Skip(uint celt);

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int Clone(out IVsEnumDebugName ppEnum);

    [PreserveSig]
    int GetCount(out uint pceltCount);
}
