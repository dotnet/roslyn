// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.SymbolStore;
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
        int GetToken(out SymbolToken pToken);
        [PreserveSig]
        int GetSequencePointCount(out int retVal);
        [PreserveSig]
        int GetRootScope([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope retVal);
        [PreserveSig]
        int GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope retVal);
        [PreserveSig]
        int GetOffset(ISymUnmanagedDocument document, int line, int column, out int retVal);
        [PreserveSig]
        int GetRanges(ISymUnmanagedDocument document, int line, int column, int cRanges, out int pcRanges, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] [In] [Out] int[] ranges);
        [PreserveSig]
        int GetParameters(int cParams, out int pcParams, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedVariable[] parms);
        [PreserveSig]
        int GetNamespace([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedNamespace retVal);
        [PreserveSig]
        int GetSourceStartEnd(ISymUnmanagedDocument[] docs, [MarshalAs(UnmanagedType.LPArray)] [In] [Out] int[] lines, [MarshalAs(UnmanagedType.LPArray)] [In] [Out] int[] columns, out bool retVal);
        [PreserveSig]
        int GetSequencePoints(int cPoints, out int pcPoints, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] int[] offsets, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] ISymUnmanagedDocument[] documents, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] int[] lines, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] int[] columns, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] int[] endLines, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] int[] endColumns);
    }
}
