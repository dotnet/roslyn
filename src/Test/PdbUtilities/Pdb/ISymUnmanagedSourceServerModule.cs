// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Roslyn.Test.PdbUtilities
{
    [ComImport]
    [Guid("997DD0CC-A76F-4c82-8D79-EA87559D27AD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    public interface ISymUnmanagedSourceServerModule
    {
        // returns the source server data for the module
        // caller must free using CoTaskMemFree()
        [PreserveSig]
        unsafe int GetSourceServerData(out int pDataByteCount, out byte* ppData);
    }
}
