// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
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
}
