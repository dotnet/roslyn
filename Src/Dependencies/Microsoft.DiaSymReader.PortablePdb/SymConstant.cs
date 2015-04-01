// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(false)]
    public sealed class SymConstant : ISymUnmanagedConstant
    {
        private readonly SymReader _symReader;
        private readonly LocalConstantHandle _handle;

        internal SymConstant(SymReader symReader, LocalConstantHandle handle)
        {
            Debug.Assert(symReader != null);
            _symReader = symReader;
            _handle = handle;
        }

        public int GetName(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]char[] name)
        {
            var mdReader = _symReader.MetadataReader;
            var constant = mdReader.GetLocalConstant(_handle);

            var str = mdReader.GetString(constant.Name);
            return InteropUtilities.StringToBuffer(str, bufferLength, out count, name);
        }

        public int GetSignature(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]byte[] signature)
        {
            var mdReader = _symReader.MetadataReader;
            var constant = mdReader.GetLocalConstant(_handle);

            // TODO: decimal, DateTime
            return InteropUtilities.BytesToBuffer(new byte[] { (byte)constant.TypeCode }, bufferLength, out count, signature);
        }

        public int GetValue(out object value)
        {
            var mdReader = _symReader.MetadataReader;
            var constant = mdReader.GetLocalConstant(_handle);

            var valueReader = mdReader.GetBlobReader(constant.Value);
            value = valueReader.ReadConstant(constant.TypeCode);

            return HResult.S_OK;
        }
    }
}
