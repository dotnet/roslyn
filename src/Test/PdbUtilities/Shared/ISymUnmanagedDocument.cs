// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [ComVisible(false)]
    [Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISymUnmanagedDocument
    {
        [PreserveSig]
        int GetURL(int cchUrl, out int pcchUrl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] char[] szUrl);
        [PreserveSig]
        int GetDocumentType(ref Guid pRetVal);
        [PreserveSig]
        int GetLanguage(ref Guid pRetVal);
        [PreserveSig]
        int GetLanguageVendor(ref Guid pRetVal);
        [PreserveSig]
        int GetCheckSumAlgorithmId(ref Guid pRetVal);
        [PreserveSig]
        int GetCheckSum(int cData, out int pcData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] [Out] byte[] data);
        [PreserveSig]
        int FindClosestLine(int line, out int pRetVal);
        [PreserveSig]
        int HasEmbeddedSource(out bool pRetVal);
        [PreserveSig]
        int GetSourceLength(out int pRetVal);
        [PreserveSig]
        int GetSourceRange(int startLine, int startColumn, int endLine, int endColumn, int cSourceBytes, out int pcSourceBytes, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] [In] [Out] byte[] source);
    }
}
