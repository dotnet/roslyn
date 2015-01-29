// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [ComImport]
    [Guid("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    internal interface ISymUnmanagedVariable
    {
        [PreserveSig]
        int GetName(int cchName, out int pcchName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] char[] szName);
        [PreserveSig]
        int GetAttributes(out int pRetVal);
        [PreserveSig]
        int GetSignature(int cSig, out int pcSig, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] byte[] sig);
        [PreserveSig]
        int GetAddressKind(out int pRetVal);
        [PreserveSig]
        int GetAddressField1(out int pRetVal);
        [PreserveSig]
        int GetAddressField2(out int pRetVal);
        [PreserveSig]
        int GetAddressField3(out int pRetVal);
        [PreserveSig]
        int GetStartOffset(out int pRetVal);
        [PreserveSig]
        int GetEndOffset(out int pRetVal);
    }
}
