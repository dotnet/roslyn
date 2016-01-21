// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

// Point-in-time conflict between System.Reflection.Metadata and temporary internal Roslyn.Reflection.Metadata
// Replace this with using System.Reflection.Metadata.Decoding and uncomment type parameters when switching
// back to public System.Reflection.Metadata API. 
using ArrayShape = Roslyn.Reflection.Metadata.Decoding.ArrayShape;
using CustomModifier = Roslyn.Reflection.Metadata.Decoding.CustomModifier<object>;
using MethodSignature = Roslyn.Reflection.Metadata.Decoding.MethodSignature<object>;
using ISignatureTypeProvider = Roslyn.Reflection.Metadata.Decoding.ISignatureTypeProvider<object>;
using PrimitiveTypeCode = Roslyn.Reflection.Metadata.Decoding.PrimitiveTypeCode;
using SignatureDecoder = Roslyn.Reflection.Metadata.Decoding.SignatureDecoder;

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

        public unsafe int GetSignature(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]byte[] signature)
        {
            var localSignatureHandle = _symMethod.MetadataReader.GetMethodDebugInformation(_symMethod.DebugHandle).LocalSignature;
            var metadataImport = _symMethod.SymReader.PdbReader.GetMetadataImport();
            var local = _symMethod.MetadataReader.GetLocalVariable(_handle);

            byte* signaturePtr;
            int signatureLength;
            int hr = metadataImport.GetSigFromToken(MetadataTokens.GetToken(localSignatureHandle), out signaturePtr, out signatureLength);
            if (hr != HResult.S_OK)
            {
                count = 0;
                return hr;
            }

            var signatureReader = new BlobReader(signaturePtr, signatureLength);

           SignatureHeader header = signatureReader.ReadSignatureHeader();
            if (header.Kind != SignatureKind.LocalVariables)
            {
                count = 0;
                return HResult.E_FAIL;
            }

            int slotCount = signatureReader.ReadCompressedInteger();
            int slotIndex = local.Index;
            if (slotIndex >= slotCount)
            {
                count = 0;
                return HResult.E_FAIL;
            }

            var typeProvider = new DummyTypeProvider(_symMethod.MetadataReader);

            for (int i = 0; i < slotIndex - 1; i++)
            {
                SignatureDecoder.DecodeType(ref signatureReader, typeProvider);
            }

            int localSlotStart = signatureReader.Offset;
            SignatureDecoder.DecodeType(ref signatureReader, typeProvider);
            int localSlotLength = signatureReader.Offset - localSlotStart;

            if (localSlotLength <= bufferLength)
            {
                Marshal.Copy((IntPtr)(signaturePtr + localSlotStart), signature, 0, localSlotLength);
            }

            count = localSlotLength;
            return HResult.S_OK;
        }

        private sealed class DummyTypeProvider : ISignatureTypeProvider/*<object>*/
        {
            public DummyTypeProvider(MetadataReader reader)
            {
                Reader = reader;
            }

            // TODO: this property shouldn't be needed
            public MetadataReader Reader { get; }

            public object GetArrayType(object elementType, ArrayShape shape) => null;
            public object GetByReferenceType(object elementType) => null;
            public object GetFunctionPointerType(MethodSignature/*<object>*/ signature) => null;
            public object GetGenericInstance(object genericType, ImmutableArray<object> typeArguments) => null;
            public object GetGenericMethodParameter(int index) => null;
            public object GetGenericTypeParameter(int index) => null;
            public object GetModifiedType(object unmodifiedType, ImmutableArray<CustomModifier/*<object>*/> customModifiers) => null;
            public object GetPinnedType(object elementType) => null;
            public object GetPointerType(object elementType) => null;
            public object GetPrimitiveType(PrimitiveTypeCode typeCode) => null;
            public object GetSZArrayType(object elementType) => null;
            public object GetTypeFromDefinition(TypeDefinitionHandle handle, bool? isValueType) => null;
            public object GetTypeFromReference(TypeReferenceHandle handle, bool? isValueType) => null;
        }
    }
}
