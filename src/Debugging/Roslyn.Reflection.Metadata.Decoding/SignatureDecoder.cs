// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
    /// <summary>
    /// Decodes signature blobs.
    /// See Metadata Specification section II.23.2: Blobs and signatures.
    /// </summary>
#if SRM && FUTURE
    public
#endif
    internal struct SignatureDecoder<TType>
    {
        private readonly ISignatureTypeProvider<TType> _provider;
        private readonly MetadataReader _metadataReaderOpt;
        private readonly SignatureDecoderOptions _options;

        /// <summary>
        /// Creates a new SignatureDecoder.
        /// </summary>
        /// <param name="provider">The provider used to obtain type symbols as the signature is decoded.</param>
        /// <param name="metadataReader">
        /// The metadata reader from which the signature was obtained. It may be null if the given provider allows it.
        /// However, if <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is specified, it should
        /// be non-null to evaluate WinRT projections from class to value type or vice-versa correctly.
        /// </param>
        /// <param name="options">Set of optional decoder features to enable.</param>
        public SignatureDecoder(
            ISignatureTypeProvider<TType> provider,
            MetadataReader metadataReader = null,
            SignatureDecoderOptions options = SignatureDecoderOptions.None)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            _metadataReaderOpt = metadataReader;
            _provider = provider;
            _options = options;
        }

        /// <summary>
        /// Decodes a type embedded in a signature and advances the reader past the type.
        /// </summary>
        /// <param name="blobReader">The blob reader positioned at the leading SignatureTypeCode</param>
        /// <param name="allowTypeSpecifications">Allow a <see cref="TypeSpecificationHandle"/> to follow a (CLASS | VALUETYPE) in the signature.
        /// At present, the only context where that would be valid is in a LocalConstantSig as defined by the Portable PDB specification.
        /// </param>
        /// <returns>The decoded type.</returns>
        /// <exception cref="System.BadImageFormatException">The reader was not positioned at a valid signature type.</exception>
        public TType DecodeType(ref BlobReader blobReader, bool allowTypeSpecifications = false)
        {
            return DecodeType(ref blobReader, allowTypeSpecifications, blobReader.ReadCompressedInteger());
        }

        private TType DecodeType(ref BlobReader blobReader, bool allowTypeSpecifications, int typeCode)
        {
            TType elementType;
            int index;

            switch (typeCode)
            {
                case (int)SignatureTypeCode.Boolean:
                case (int)SignatureTypeCode.Char:
                case (int)SignatureTypeCode.SByte:
                case (int)SignatureTypeCode.Byte:
                case (int)SignatureTypeCode.Int16:
                case (int)SignatureTypeCode.UInt16:
                case (int)SignatureTypeCode.Int32:
                case (int)SignatureTypeCode.UInt32:
                case (int)SignatureTypeCode.Int64:
                case (int)SignatureTypeCode.UInt64:
                case (int)SignatureTypeCode.Single:
                case (int)SignatureTypeCode.Double:
                case (int)SignatureTypeCode.IntPtr:
                case (int)SignatureTypeCode.UIntPtr:
                case (int)SignatureTypeCode.Object:
                case (int)SignatureTypeCode.String:
                case (int)SignatureTypeCode.Void:
                case (int)SignatureTypeCode.TypedReference:
                    return _provider.GetPrimitiveType((PrimitiveTypeCode)typeCode);

                case (int)SignatureTypeCode.Pointer:
                    elementType = DecodeType(ref blobReader);
                    return _provider.GetPointerType(elementType);

                case (int)SignatureTypeCode.ByReference:
                    elementType = DecodeType(ref blobReader);
                    return _provider.GetByReferenceType(elementType);

                case (int)SignatureTypeCode.Pinned:
                    elementType = DecodeType(ref blobReader);
                    return _provider.GetPinnedType(elementType);

                case (int)SignatureTypeCode.SZArray:
                    elementType = DecodeType(ref blobReader);
                    return _provider.GetSZArrayType(elementType);

                case (int)SignatureTypeCode.FunctionPointer:
                    MethodSignature<TType> methodSignature = DecodeMethodSignature(ref blobReader);
                    return _provider.GetFunctionPointerType(methodSignature);

                case (int)SignatureTypeCode.Array:
                    return DecodeArrayType(ref blobReader);

                case (int)SignatureTypeCode.RequiredModifier:
                    return DecodeModifiedType(ref blobReader, isRequired: true);

                case (int)SignatureTypeCode.OptionalModifier:
                    return DecodeModifiedType(ref blobReader, isRequired: false);

                case (int)SignatureTypeCode.GenericTypeInstance:
                    return DecodeGenericTypeInstance(ref blobReader);

                case (int)SignatureTypeCode.GenericTypeParameter:
                    index = blobReader.ReadCompressedInteger();
                    return _provider.GetGenericTypeParameter(index);

                case (int)SignatureTypeCode.GenericMethodParameter:
                    index = blobReader.ReadCompressedInteger();
                    return _provider.GetGenericMethodParameter(index);

                case (int)SignatureTypeHandleCode.Class:
                case (int)SignatureTypeHandleCode.ValueType:
                    return DecodeTypeHandle(ref blobReader, (SignatureTypeHandleCode)typeCode, allowTypeSpecifications);

                default:
#if SRM
                    throw new BadImageFormatException(SR.Format(SR.UnexpectedSignatureTypeCode, typeCode));
#else
                    throw new BadImageFormatException();
#endif
            }
        }

        /// <summary> 
        /// Decodes a list of types, with at least one instance that is preceded by its count as a compressed integer.
        /// </summary>
        private ImmutableArray<TType> DecodeTypeSequence(ref BlobReader blobReader)
        {
            int count = blobReader.ReadCompressedInteger();
            if (count == 0)
            {
                // This method is used for Local signatures and method specs, neither of which can have
                // 0 elements. Parameter sequences can have 0 elements, but they are handled separately
                // to deal with the sentinel/varargs case.
#if SRM
                throw new BadImageFormatException(SR.SignatureTypeSequenceMustHaveAtLeastOneElement);
#else
                throw new BadImageFormatException();
#endif
            }

            var types = ImmutableArray.CreateBuilder<TType>(count);

            for (int i = 0; i < count; i++)
            {
                types.Add(DecodeType(ref blobReader));
            }

            return types.MoveToImmutable();
        }

        /// <summary>
        /// Decodes a method (definition, reference, or standalone) or property signature blob.
        /// </summary>
        /// <param name="blobReader">BlobReader positioned at a method signature.</param>
        /// <returns>The decoded method signature.</returns>
        /// <exception cref="System.BadImageFormatException">The method signature is invalid.</exception>
        public MethodSignature<TType> DecodeMethodSignature(ref BlobReader blobReader)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            CheckMethodOrPropertyHeader(header);

            int genericParameterCount = 0;
            if (header.IsGeneric)
            {
                genericParameterCount = blobReader.ReadCompressedInteger();
            }

            int parameterCount = blobReader.ReadCompressedInteger();
            TType returnType = DecodeType(ref blobReader);
            ImmutableArray<TType> parameterTypes;
            int requiredParameterCount;

            if (parameterCount == 0)
            {
                requiredParameterCount = 0;
                parameterTypes = ImmutableArray<TType>.Empty;
            }
            else
            {
                var parameterBuilder = ImmutableArray.CreateBuilder<TType>(parameterCount);
                int parameterIndex;

                for (parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
                {
                    int typeCode = blobReader.ReadCompressedInteger();
                    if (typeCode == (int)SignatureTypeCode.Sentinel)
                    {
                        break;
                    }
                    parameterBuilder.Add(DecodeType(ref blobReader, allowTypeSpecifications: false, typeCode: typeCode));
                }

                requiredParameterCount = parameterIndex;
                for (; parameterIndex < parameterCount; parameterIndex++)
                {
                    parameterBuilder.Add(DecodeType(ref blobReader));
                }
                parameterTypes = parameterBuilder.MoveToImmutable();
            }

            return new MethodSignature<TType>(header, returnType, requiredParameterCount, genericParameterCount, parameterTypes);
        }

        /// <summary>
        /// Decodes a method specification signature blob and advances the reader past the signature.
        /// </summary>
        /// <param name="blobReader">A BlobReader positioned at a valid method specification signature.</param>
        /// <returns>The types used to instantiate a generic method via the method specification.</returns>
        public ImmutableArray<TType> DecodeMethodSpecificationSignature(ref BlobReader blobReader)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            CheckHeader(header, SignatureKind.MethodSpecification);
            return DecodeTypeSequence(ref blobReader);
        }

        /// <summary>
        /// Decodes a local variable signature blob and advances the reader past the signature.
        /// </summary>
        /// <param name="blobReader">The blob reader positioned at a local variable signature.</param>
        /// <returns>The local variable types.</returns>
        /// <exception cref="System.BadImageFormatException">The local variable signature is invalid.</exception>
        public ImmutableArray<TType> DecodeLocalSignature(ref BlobReader blobReader)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            CheckHeader(header, SignatureKind.LocalVariables);
            return DecodeTypeSequence(ref blobReader);
        }

        /// <summary>
        /// Decodes a field signature blob and advances the reader past the signature.
        /// </summary>
        /// <param name="blobReader">The blob reader positioned at a field signature.</param>
        /// <returns>The decoded field type.</returns>
        public TType DecodeFieldSignature(ref BlobReader blobReader)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            CheckHeader(header, SignatureKind.Field);
            return DecodeType(ref blobReader);
        }

        private TType DecodeArrayType(ref BlobReader blobReader)
        {
            // PERF_TODO: Cache/reuse common case of small number of all-zero lower-bounds.

            TType elementType = DecodeType(ref blobReader);
            int rank = blobReader.ReadCompressedInteger();
            var sizes = ImmutableArray<int>.Empty;
            var lowerBounds = ImmutableArray<int>.Empty;

            int sizesCount = blobReader.ReadCompressedInteger();
            if (sizesCount > 0)
            {
                var builder = ImmutableArray.CreateBuilder<int>(sizesCount);
                for (int i = 0; i < sizesCount; i++)
                {
                    builder.Add(blobReader.ReadCompressedInteger());
                }
                sizes = builder.MoveToImmutable();
            }

            int lowerBoundsCount = blobReader.ReadCompressedInteger();
            if (lowerBoundsCount > 0)
            {
                var builder = ImmutableArray.CreateBuilder<int>(lowerBoundsCount);
                for (int i = 0; i < lowerBoundsCount; i++)
                {
                    builder.Add(blobReader.ReadCompressedSignedInteger());
                }
                lowerBounds = builder.MoveToImmutable();
            }

            var arrayShape = new ArrayShape(rank, sizes, lowerBounds);
            return _provider.GetArrayType(elementType, arrayShape);
        }

        private TType DecodeGenericTypeInstance(ref BlobReader blobReader)
        {
            TType genericType = DecodeType(ref blobReader);
            ImmutableArray<TType> types = DecodeTypeSequence(ref blobReader);
            return _provider.GetGenericInstance(genericType, types);
        }

        private TType DecodeModifiedType(ref BlobReader blobReader, bool isRequired)
        {
            TType modifier = DecodeTypeDefOrRefOrSpec(ref blobReader, SignatureTypeHandleCode.Unresolved);
            TType unmodifiedType = DecodeType(ref blobReader);

            return _provider.GetModifiedType(_metadataReaderOpt, isRequired, modifier, unmodifiedType);
        }

        private TType DecodeTypeDefOrRef(ref BlobReader blobReader, SignatureTypeHandleCode code)
        {
            return DecodeTypeHandle(ref blobReader, code, allowTypeSpecifications: false);
        }

        private TType DecodeTypeDefOrRefOrSpec(ref BlobReader blobReader, SignatureTypeHandleCode code)
        {
            return DecodeTypeHandle(ref blobReader, code, allowTypeSpecifications: true);
        }

        private TType DecodeTypeHandle(ref BlobReader blobReader, SignatureTypeHandleCode code, bool allowTypeSpecifications)
        {
            // Force no differentiation of class vs. value type unless the option is enabled.
            // Avoids cost of WinRT projection.
            if ((_options & SignatureDecoderOptions.DifferentiateClassAndValueTypes) == 0)
            {
                code = SignatureTypeHandleCode.Unresolved;
            }

            EntityHandle handle = blobReader.ReadTypeHandle();
            if (!handle.IsNil)
            {
                switch (handle.Kind)
                {
                    case HandleKind.TypeDefinition:
                        var typeDef = (TypeDefinitionHandle)handle;
                        return _provider.GetTypeFromDefinition(_metadataReaderOpt, typeDef, code);

                    case HandleKind.TypeReference:
                        var typeRef = (TypeReferenceHandle)handle;
                        if (code != SignatureTypeHandleCode.Unresolved)
                        {
                            ProjectClassOrValueType(typeRef, ref code);
                        }
                        return _provider.GetTypeFromReference(_metadataReaderOpt, typeRef, code);

                    case HandleKind.TypeSpecification:
                        if (!allowTypeSpecifications)
                        {
#if SRM
                            // To prevent cycles, the token following (CLASS | VALUETYPE) must not be a type spec.
                            // https://github.com/dotnet/coreclr/blob/8ff2389204d7c41b17eff0e9536267aea8d6496f/src/md/compiler/mdvalidator.cpp#L6154-L6160
                            throw new BadImageFormatException(SR.NotTypeDefOrRefHandle);
#else
                            throw new BadImageFormatException();
#endif

                        }

                        if (code != SignatureTypeHandleCode.Unresolved)
                        {
                            // TODO: We need more work here in differentiating case because instantiations can project class 
                            // to value type as in IReference<T> -> Nullable<T>. Unblocking Roslyn work where the differentiation
                            // feature is not used. Note that the use-case of custom-mods will not hit this because there is no
                            // CLASS | VALUETYPE before the modifier token and so it always comes in unresolved.
                            code = SignatureTypeHandleCode.Unresolved; // never lie in the meantime.
                        }

                        var typeSpec = (TypeSpecificationHandle)handle;
                        return _provider.GetTypeFromSpecification(_metadataReaderOpt, typeSpec, SignatureTypeHandleCode.Unresolved);

                    default:
                        // indicates an error returned from ReadTypeHandle, otherwise unreachable.
                        Debug.Assert(handle.IsNil); // will fall through to throw in release.
                        break;
                }
            }

#if SRM
            throw new BadImageFormatException(SR.NotTypeDefOrRefOrSpecHandle);
#else
            throw new BadImageFormatException();
#endif
        }

        private void ProjectClassOrValueType(TypeReferenceHandle handle, ref SignatureTypeHandleCode code)
        {
            Debug.Assert(code != SignatureTypeHandleCode.Unresolved);
            Debug.Assert((_options & SignatureDecoderOptions.DifferentiateClassAndValueTypes) != 0);

            if (_metadataReaderOpt == null)
            {
                // If we're asked to differentiate value types without a reader, then 
                // return the designation unprojected as it occurs in the signature blob.
                return;
            }

#if SRM
            TypeReference typeRef = _metadataReaderOpt.GetTypeReference(handle);
            switch (typeRef.SignatureTreatment)
            {
                case TypeRefSignatureTreatment.ProjectedToClass:
                    code = SignatureTypeHandleCode.Class;
                    break;
                case TypeRefSignatureTreatment.ProjectedToValueType:
                    code = SignatureTypeHandleCode.ValueType;
                    break;
            }
#endif
        }

        private void CheckHeader(SignatureHeader header, SignatureKind expectedKind)
        {
            if (header.Kind != expectedKind)
            {
#if SRM
                throw new BadImageFormatException(SR.Format(SR.UnexpectedSignatureHeader, expectedKind, header.Kind, header.RawValue));
#else
                throw new BadImageFormatException();
#endif

            }
        }

        private void CheckMethodOrPropertyHeader(SignatureHeader header)
        {
            SignatureKind kind = header.Kind;
            if (kind != SignatureKind.Method && kind != SignatureKind.Property)
            {
#if SRM
                throw new BadImageFormatException(SR.Format(SR.UnexpectedSignatureHeader2, SignatureKind.Property, SignatureKind.Method, header.Kind, header.RawValue));
#else
                throw new BadImageFormatException();
#endif
            }
        }
    }
}
