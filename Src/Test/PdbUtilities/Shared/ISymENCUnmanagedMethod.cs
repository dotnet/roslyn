// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [ComVisible(false)]
    [Guid("85E891DA-A631-4c76-ACA2-A44A39C46B8C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISymENCUnmanagedMethod
    {
        /// <summary>
        /// Get the file name for the line associated with offset dwOffset.
        /// </summary>
        void __GetFileNameFromOffset(int dwOffset, int cchName, out int pcchName, StringBuilder name);

        /// <summary>
        /// Get the Line information associated with dwOffset.
	    /// If dwOffset is not a sequence point it is associated with the previous one.
        /// pdwStartOffset provides the associated sequence point.
        /// </summary>
        void __GetLineFromOffset(int dwOffset, out int pline, out int pcolumn, out int pendLine, out int pendColumn, out int pdwStartOffset);

        /// <summary>
        /// Get the number of Documents that this method has lines in.
        /// </summary>
        int GetDocumentsForMethodCount();

        /// <summary>
        /// Get the documents this method has lines in.
        /// </summary>
        void GetDocumentsForMethod(
            int cDocs,
            out int pcDocs,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]ISymUnmanagedDocument[] documents);

        /// <summary>
        /// Get the smallest start line and largest end line, for the method, in a specific document.
        /// </summary>
        void GetSourceExtentInDocument(ISymUnmanagedDocument document, out int pstartLine, out int pendLine);
    }
}
