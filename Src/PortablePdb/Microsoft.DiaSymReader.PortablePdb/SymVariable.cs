// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Decoding;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(false)]
    public sealed class SymVariable : ISymUnmanagedVariable
    {
        private const int ADDR_IL_OFFSET = 1;

        private readonly SymMethod _symMethod;
        private readonly LocalVariableHandle _handle;

        internal SymVariable(SymMethod symMethod, LocalVariableHandle handle)
        {
            Debug.Assert(symMethod != null);
            _symMethod = symMethod;
            _handle = handle;
        }

        private MetadataReader MetadataReader => _symMethod.MetadataReader;

        public int GetAttributes(out int attributes)
        {
            var variable = MetadataReader.GetLocalVariable(_handle);
            attributes = (int)variable.Attributes;
            return HResult.S_OK;
        }

        public int GetAddressField1(out int value)
        {
            var variable = MetadataReader.GetLocalVariable(_handle);
            value = variable.Index;
            return HResult.S_OK;
        }

        public int GetAddressField2(out int value)
        {
            // not implemented by DiaSymReader
            value = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetAddressField3(out int value)
        {
            // not implemented by DiaSymReader
            value = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetStartOffset(out int offset)
        {
            // not implemented by DiaSymReader
            offset = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetEndOffset(out int offset)
        {
            // not implemented by DiaSymReader
            offset = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetAddressKind(out int kind)
        {
            kind = ADDR_IL_OFFSET;
            return HResult.S_OK;
        }

        public int GetName(
            int bufferLength, 
            out int count, 
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]char[] name)
        {
            var variable = MetadataReader.GetLocalVariable(_handle);
            var str = MetadataReader.GetString(variable.Name);
            return InteropUtilities.StringToBuffer(str, bufferLength, out count, name);
        }

        public int GetSignature(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]byte[] signature)
        {
            // TODO:
            throw new NotImplementedException();
        }
    }
}
