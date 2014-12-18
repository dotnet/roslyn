// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        int GetName(int cchName, out int pcchName, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] name);
        int __GetAttributes(out uint pRetVal);
        [PreserveSig]
        int GetSignature(int cSig, out int pcSig, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] sig);

        // the following methods are useless (not implemented, or returning a constant):
        int __GetAddressKind(out int pRetVal);
        [PreserveSig]
        int GetAddressField1(out int pRetVal);
        int __GetAddressField2(out int pRetVal);
        int __GetAddressField3(out int pRetVal);
        int __GetStartOffset(out int pRetVal);
        int __GetEndOffset(out int pRetVal);
    } 
}
