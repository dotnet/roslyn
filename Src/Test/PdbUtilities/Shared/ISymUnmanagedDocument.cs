// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [ComVisible(false)]
    [Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISymUnmanagedDocument
    {
        int __FindClosestLine(int line, out int pRetVal);
        int __GetCheckSum(int cData, out int pcData, byte[] data);
        int __GetCheckSumAlgorithmId(ref Guid pRetVal);
        int __GetDocumentType(ref Guid pRetVal);
        int __GetLanguage(ref Guid pRetVal);
        int __GetLanguageVendor(ref Guid pRetVal);
        int __GetSourceLength(out int pRetVal);
        int __GetSourceRange(int startLine, int startColumn, int endLine, int endColumn, int cSourceBytes, out int pcSourceBytes, byte[] source);
        int __GetURL(int cchUrl, out int pcchUrl, IntPtr szUrl);
        int __HasEmbeddedSource(out bool pRetVal);
    }
}
