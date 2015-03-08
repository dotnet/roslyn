// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(true)]
    public sealed class SymMethod : ISymUnmanagedMethod
    {
        public int GetNamespace([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedNamespace @namespace)
        {
            throw new NotImplementedException();
        }

        public int GetOffset(ISymUnmanagedDocument document, int line, int column, out int offset)
        {
            throw new NotImplementedException();
        }

        public int GetParameters(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedVariable[] parameters)
        {
            throw new NotImplementedException();
        }

        public int GetRanges(ISymUnmanagedDocument document, int line, int column, int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]int[] ranges)
        {
            throw new NotImplementedException();
        }

        public int GetRootScope([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            throw new NotImplementedException();
        }

        public int GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            throw new NotImplementedException();
        }

        public int GetSequencePointCount(out int count)
        {
            throw new NotImplementedException();
        }

        public int GetSequencePoints(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] offsets, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedDocument[] documents, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] startLines, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] startColumns, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] endLines, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] endColumns)
        {
            throw new NotImplementedException();
        }

        public int GetSourceStartEnd(ISymUnmanagedDocument[] documents, [In, MarshalAs(UnmanagedType.LPArray), Out]int[] lines, [In, MarshalAs(UnmanagedType.LPArray), Out]int[] columns, out bool defined)
        {
            throw new NotImplementedException();
        }

        public int GetToken(out int methodToken)
        {
            throw new NotImplementedException();
        }
    }
}
