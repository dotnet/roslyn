// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(false)]
    public sealed class SymScope : ISymUnmanagedScope2
    {
        internal readonly ScopeData _data;

        internal SymScope(ScopeData data)
        {
            Debug.Assert(data != null);
            _data = data;
        }

        public int GetChildren(
            int bufferLength,
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedScope[] children)
        {
            var childrenData = _data.GetChildren();

            int i = 0;
            foreach (var childData in childrenData)
            {
                if (i >= bufferLength)
                {
                    break;
                }

                children[i++] = new SymScope(childData);
            }

            count = (bufferLength == 0) ? childrenData.Length : i;
            return HResult.S_OK;
        }

        public int GetConstantCount(out int count)
        {
            return GetConstants(0, out count, null);
        }

        public int GetConstants(
            int bufferLength,
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedConstant[] constants)
        {
            return _data.GetConstants(bufferLength, out count, constants);
        }

        public int GetLocalCount(out int count)
        {
            return GetLocals(0, out count, null);
        }

        public int GetLocals(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedVariable[] locals)
        {
            return _data.GetLocals(bufferLength, out count, locals);
        }

        public int GetMethod([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedMethod method)
        {
            method = _data.SymMethod;
            return HResult.S_OK;
        }

        public int GetParent([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            var parentData = _data.Parent;
            if (parentData == null)
            {
                scope = null;

                // TODO: ??
                return HResult.E_FAIL; 
            }

            scope = new SymScope(parentData);
            return HResult.S_OK;
        }

        public int GetStartOffset(out int offset)
        {
            throw new NotImplementedException();
        }

        public int GetEndOffset(out int offset)
        {
            throw new NotImplementedException();
        }

        public int GetNamespaces(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedNamespace[] namespaces)
        {
            throw new NotImplementedException();
        }
    }
}
