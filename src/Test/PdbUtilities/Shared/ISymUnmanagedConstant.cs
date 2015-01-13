// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [
        ComImport,
        Guid("48B25ED8-5BAD-41bc-9CEE-CD62FABC74E9"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedConstant
    {
        [PreserveSig]
        int GetName(int cchName, out int pcchName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] char[] name);
        [PreserveSig]
        int GetValue(out object pValue);
        [PreserveSig]
        int GetSignature(int cSig, out int pcSig, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] byte[] sig);
    }
}
