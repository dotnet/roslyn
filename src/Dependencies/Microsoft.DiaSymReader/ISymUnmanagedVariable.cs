// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    [ComImport]
    [Guid("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    public interface ISymUnmanagedVariable
    {
        [PreserveSig]
        int GetName(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] name);

        [PreserveSig]
        int GetAttributes(out int attributes);

        [PreserveSig]
        int GetSignature(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] signature);

        [PreserveSig]
        int GetAddressKind(out int kind);

        [PreserveSig]
        int GetAddressField1(out int value);

        [PreserveSig]
        int GetAddressField2(out int value);

        [PreserveSig]
        int GetAddressField3(out int value);

        [PreserveSig]
        int GetStartOffset(out int offset);

        [PreserveSig]
        int GetEndOffset(out int offset);
    }
}
