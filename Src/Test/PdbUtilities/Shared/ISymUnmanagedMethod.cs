// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [ComImport]
    [Guid("B62B923C-B500-3158-A543-24F307A8B7E1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    internal interface ISymUnmanagedMethod
    {
        [PreserveSig]
        int GetToken(out int retVal);
        int __GetSequencePointCount(out int retVal);

        [PreserveSig]
        int GetRootScope([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope retVal);
        int __GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope retVal);

        int __GetOffset(/*ISymUnmanagedDocument document, int line, int column, out int retVal*/);
        int __GetRanges(/*ISymUnmanagedDocument document, int line, int column, int cRanges, out int pcRanges, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] ranges*/);
        int __GetParameters(/*int cParams, out int pcParams, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] parms*/);
        int __GetNamespace(/*[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedNamespace retVal*/);
        int __GetSourceStartEnd(/*ISymUnmanagedDocument[] docs, [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] lines, [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] columns, out Boolean retVal*/);

        int __GetSequencePoints(
            int cPoints,
            out int pcPoints,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] offsets,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] documents,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] lines,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] columns,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endLines,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endColumns);
    }
}
