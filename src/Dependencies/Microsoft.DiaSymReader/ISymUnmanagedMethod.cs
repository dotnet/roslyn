// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    [ComImport]
    [Guid("B62B923C-B500-3158-A543-24F307A8B7E1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    public interface ISymUnmanagedMethod
    {
        [PreserveSig]
        int GetToken(out int methodToken);

        [PreserveSig]
        int GetSequencePointCount(out int count);

        [PreserveSig]
        int GetRootScope([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope scope);

        [PreserveSig]
        int GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope scope);

        /// <summary>
        /// Gets the IL offset within the method that corresponds to the specified position.
        /// </summary>
        /// <param name="document">The document for which the offset is requested. </param>
        /// <param name="line">The document line corresponding to the offset. </param>
        /// <param name="column">The document column corresponding to the offset. </param>
        /// <param name="offset">The offset within the specified document.</param>
        /// <returns>HResult.</returns>
        [PreserveSig]
        int GetOffset(ISymUnmanagedDocument document, int line, int column, out int offset);

        /// <summary>
        /// Gets an array of start and end offset pairs that correspond to the ranges of IL that a given position covers within this method.
        /// </summary>
        [PreserveSig]
        int GetRanges(
            ISymUnmanagedDocument document,
            int line,
            int column,
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] ranges);

        /// <summary>
        /// Gets method parameters.
        /// </summary>
        [PreserveSig]
        int GetParameters(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] parameters);

        [PreserveSig]
        int GetNamespace([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedNamespace @namespace);

        /// <summary>
        /// Gets the start and end positions for the source of the current method.
        /// </summary>
        /// <param name="documents">The starting and ending source documents.</param>
        /// <param name="lines">The starting and ending lines in the corresponding source documents. </param>
        /// <param name="columns">The starting and ending columns in the corresponding source documents. </param>
        /// <param name="defined">true if the positions were defined; otherwise, false.</param>
        /// <returns>HResult</returns>
        [PreserveSig]
        int GetSourceStartEnd(
            ISymUnmanagedDocument[] documents,
            [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] lines,
            [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] columns,
            [MarshalAs(UnmanagedType.Bool)]out bool defined);

        [PreserveSig]
        int GetSequencePoints(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] offsets,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] documents,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] startLines,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] startColumns,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endLines,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endColumns);
    }
}
