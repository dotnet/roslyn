// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(false)]
    public sealed class SymMethod : ISymUnmanagedMethod
    {
        private readonly MethodDefinitionHandle _handle;
        private readonly SymReader _symReader;
        private RootScopeData _lazyRootScopeData;

        internal SymMethod(SymReader symReader, MethodDefinitionHandle handle)
        {
            Debug.Assert(symReader != null);
            _symReader = symReader;
            _handle = handle;
        }

        internal SymReader SymReader => _symReader;
        internal MetadataReader MetadataReader => _symReader.MetadataReader;
        internal MethodDefinitionHandle Handle => _handle;

        public int GetNamespace([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedNamespace @namespace)
        {
            // SymReader doesn't support namspaces
            @namespace = null;
            return HResult.E_NOTIMPL;
        }

        public int GetOffset(ISymUnmanagedDocument document, int line, int column, out int offset)
        {
            // TODO:
            throw new NotImplementedException();
        }

        public int GetParameters(
            int bufferLength, 
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedVariable[] parameters)
        {
            // SymReader doesn't support parameter access. 
            count = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetRanges(
            ISymUnmanagedDocument document, 
            int line, 
            int column, 
            int bufferLength, 
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]int[] ranges)
        {
            // TODO:
            throw new NotImplementedException();
        }

        public int GetRootScope([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            if (_lazyRootScopeData == null)
            {
                _lazyRootScopeData = new RootScopeData(this);
            }

            // SymReader always creates a new scope instance
            scope = new SymScope(_lazyRootScopeData);
            return HResult.S_OK;
        }

        public int GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            // SymReader doesn't support. 
            scope = null;
            return HResult.S_OK;
        }

        public int GetSequencePointCount(out int count)
        {
            return GetSequencePoints(0, out count, null, null, null, null, null, null);
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
            // TODO: cache

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

        public int GetSourceStartEnd(
            ISymUnmanagedDocument[] documents, 
            [In, MarshalAs(UnmanagedType.LPArray), Out]int[] lines, 
            [In, MarshalAs(UnmanagedType.LPArray), Out]int[] columns, 
            out bool defined)
        {
            // This symbol reader doesn't support source start/end for methods.
            defined = false;
            return HResult.E_NOTIMPL;
        }

        public int GetToken(out int methodToken)
        {
            methodToken = MetadataTokens.GetToken(_handle);
            return HResult.S_OK;
        }
    }
}

