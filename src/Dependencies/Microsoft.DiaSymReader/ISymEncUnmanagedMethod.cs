// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.DiaSymReader
{
    [ComImport]
    [Guid("85E891DA-A631-4c76-ACA2-A44A39C46B8C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    public interface ISymEncUnmanagedMethod
    {
        /// <summary>
        /// Get the file name for the line associated with offset dwOffset.
        /// </summary>
        [PreserveSig]
        int GetFileNameFromOffset(
            int offset,
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] name);

        /// <summary>
        /// Get the Line information associated with <paramref name="offset"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="offset"/> is not a sequence point it is associated with the previous one.
        /// <paramref name="sequencePointOffset"/> provides the associated sequence point.
        /// </remarks>
        [PreserveSig]
        int GetLineFromOffset(
            int offset,
            out int startLine,
            out int startColumn,
            out int endLine,
            out int endColumn,
            out int sequencePointOffset);

        /// <summary>
        /// Get the number of Documents that this method has lines in.
        /// </summary>
        [PreserveSig]
        int GetDocumentsForMethodCount(out int count);

        /// <summary>
        /// Get the documents this method has lines in.
        /// </summary>
        [PreserveSig]
        int GetDocumentsForMethod(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]ISymUnmanagedDocument[] documents);

        /// <summary>
        /// Get the smallest start line and largest end line, for the method, in a specific document.
        /// </summary>
        [PreserveSig]
        int GetSourceExtentInDocument(ISymUnmanagedDocument document, out int startLine, out int endLine);
    }
}