// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(true)]
    public sealed class SymMethod : ISymUnmanagedMethod
    {
        private readonly MethodDefinitionHandle _handle;
        private readonly SymReader _symReader;

        internal SymMethod(SymReader symReader, MethodDefinitionHandle handle)
        {
            Debug.Assert(symReader != null);
            _symReader = symReader;
            _handle = handle;
        }

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
            // TODO: binary search
            var mdReader = _symReader.MetadataReader;
            for (int i = 0; i < mdReader.LocalScopes.Count; i++)
            {
                LocalScopeHandle scopeHandle = MetadataTokens.LocalScopeHandle(i + 1);
                var s = mdReader.GetLocalScope(scopeHandle);
                if (s.Method == _handle)
                {
                    scope = new SymScope(this, scopeHandle);
                    return HResult.S_OK;
                }
            }

            scope = null;
            return HResult.E_INVALIDARG;
        }

        public int GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            throw new NotImplementedException();
        }

        public int GetSequencePointCount(out int count)
        {
            throw new NotImplementedException();
        }

        public int GetSequencePoints(
            int bufferLength,
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] offsets,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedDocument[] documents, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] startLines, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] startColumns,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] endLines,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] endColumns)
        {
            var mdReader = _symReader.MetadataReader;

            var body = mdReader.GetMethodBody(_handle);
            var spReader = mdReader.GetSequencePointsReader(body.SequencePoints);

            SymDocument currentDocument = null;

            int i = 0;
            while (spReader.MoveNext())
            {
                if (bufferLength != 0 && i >= bufferLength)
                {
                    break;
                }

                var sp = spReader.Current;

                if (offsets != null)
                {
                    offsets[i] = sp.Offset;
                }

                if (startLines != null)
                {
                    startLines[i] = sp.StartLine;
                }

                if (startColumns != null)
                {
                    startColumns[i] = sp.StartColumn;
                }

                if (endLines != null)
                {
                    endLines[i] = sp.EndLine;
                }

                if (endColumns != null)
                {
                    endColumns[i] = sp.EndColumn;
                }

                if (documents != null)
                {
                    if (currentDocument == null || currentDocument.Handle != sp.Document)
                    {
                        currentDocument = new SymDocument(_symReader, sp.Document);
                    }

                    documents[i] = currentDocument;
                }

                i++;
            }

            count = i;
            return HResult.S_OK;
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

