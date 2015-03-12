// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(true)]
    public sealed class SymScope : ISymUnmanagedScope2
    {
        public int GetChildren(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedScope[] children)
        {
            throw new NotImplementedException();
        }

        public int GetConstantCount(out int count)
        {
            throw new NotImplementedException();
        }

        public int GetConstants(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedConstant[] constants)
        {
            throw new NotImplementedException();
        }

        public int GetEndOffset(out int offset)
        {
            throw new NotImplementedException();
        }

        public int GetLocalCount(out int count)
        {
            throw new NotImplementedException();
        }

        public int GetLocals(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedVariable[] locals)
        {
            throw new NotImplementedException();
        }

        public int GetMethod([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            throw new NotImplementedException();
        }

        public int GetNamespaces(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedNamespace[] namespaces)
        {
            throw new NotImplementedException();
        }

        public int GetParent([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            throw new NotImplementedException();
        }

        public int GetStartOffset(out int offset)
        {
            throw new NotImplementedException();
        }
    }
}
