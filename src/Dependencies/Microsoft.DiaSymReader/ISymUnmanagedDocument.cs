// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.DiaSymReader
{
    [ComImport]
    [ComVisible(false)]
    [Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISymUnmanagedDocument
    {
        [PreserveSig]
        int GetUrl(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] url);

        [PreserveSig]
        int GetDocumentType(ref Guid documentType);

        [PreserveSig]
        int GetLanguage(ref Guid language);

        [PreserveSig]
        int GetLanguageVendor(ref Guid vendor);

        [PreserveSig]
        int GetChecksumAlgorithmId(ref Guid algorithm);

        [PreserveSig]
        int GetChecksum(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] checksum);

        [PreserveSig]
        int FindClosestLine(int line, out int closestLine);

        [PreserveSig]
        int HasEmbeddedSource([MarshalAs(UnmanagedType.Bool)]out bool value);

        [PreserveSig]
        int GetSourceLength(out int length);

        [PreserveSig]
        int GetSourceRange(
            int startLine,
            int startColumn,
            int endLine,
            int endColumn,
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] source);
    }
}
