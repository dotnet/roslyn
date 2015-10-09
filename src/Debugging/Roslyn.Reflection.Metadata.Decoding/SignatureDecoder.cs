// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// NOTE: This is a temporary internal copy of code that will be cut from System.Reflection.Metadata v1.1 and
//       ship in System.Reflection.Metadata v1.2 (with breaking changes). Remove and use the public API when
//       a v1.2 prerelease is available and code flow is such that we can start to depend on it.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Roslyn.Reflection.Metadata.Decoding
{
    /// <summary>
    /// Decodes signature blobs.
    /// </summary>
    internal static class SignatureDecoder
    {
        /// <summary>
        /// Decodes a type definition, reference or specification to its representation as TType.
        /// </summary>
        /// <param name="handle">A type definition, reference, or specification handle.</param>
        /// <param name="provider">The type provider.</param>
        /// <param name="isValueType">Is the type a class or a value type. Null signifies that the current type signature does not have the prefix</param>
        /// <exception cref="System.BadImageFormatException">The handle does not represent a valid type reference, definition, or specification.</exception>
        public static TType DecodeType<TType>(Handle handle, ISignatureTypeProvider<TType> provider, bool? isValueType)
        {
            switch (handle.Kind)
            {
                case HandleKind.TypeReference:
                    return provider.GetTypeFromReference((TypeReferenceHandle)handle, isValueType);

                case HandleKind.TypeDefinition:
                    return provider.GetTypeFromDefinition((TypeDefinitionHandle)handle, isValueType);

                case HandleKind.TypeSpecification:
                    return DecodeTypeSpecification((TypeSpecificationHandle)handle, provider);

                default:
                    throw new BadImageFormatException();
            }
        }

        /// <summary>
        /// Decodes a type specification.
        /// </summary>
        /// <param name="handle">The type specification handle.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The decoded type.</returns>
        /// <exception cref="System.BadImageFormatException">The type specification has an invalid signature.</exception>
        private static TType DecodeTypeSpecification<TType>(TypeSpecificationHandle handle, ISignatureTypeProvider<TType> provider)
        {
            BlobHandle blobHandle = provider.Reader.GetTypeSpecification(handle).Signature;
            BlobReader blobReader = provider.Reader.GetBlobReader(blobHandle);
            return DecodeType(ref blobReader, provider);
        }

        /// <summary>
        /// Decodes a type from within a signature from a BlobReader positioned at its leading SignatureTypeCode.
        /// </summary>
        /// <param name="blobReader">The blob reader.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The decoded type.</returns>
        /// <exception cref="System.BadImageFormatException">The reader was not positioned at a valid signature type.</exception>
        public static TType DecodeType<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider)
        {
            return DecodeType(ref blobReader, blobReader.ReadCompressedInteger(), provider);
        }

        /// <summary>
        /// Decodes a type from within a signature from a BlobReader positioned immediately past the given SignatureTypeCode.
        /// </summary>
        /// <param name="blobReader">The blob reader.</param>
        /// <param name="typeCode">The SignatureTypeCode that immediately preceded the reader's current position.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The decoded type.</returns>
        /// <exception cref="System.BadImageFormatException">The reader was not positioned at a valud signature type.</exception>
        private static TType DecodeType<TType>(ref BlobReader blobReader, int typeCode, ISignatureTypeProvider<TType> provider)
        {
            TType elementType;
            int index;
            if(typeCode > byte.MaxValue)
            {
                typeCode = (int)SignatureTypeCode.Invalid;
            }
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
                    return provider.GetPrimitiveType((PrimitiveTypeCode)typeCode);

                case (int)SignatureTypeCode.Pointer:
                    elementType = DecodeType(ref blobReader, provider);
                    return provider.GetPointerType(elementType);

                case (int)SignatureTypeCode.ByReference:
                    elementType = DecodeType(ref blobReader, provider);
                    return provider.GetByReferenceType(elementType);

                case (int)SignatureTypeCode.Pinned:
                    elementType = DecodeType(ref blobReader, provider);
                    return provider.GetPinnedType(elementType);

                case (int)SignatureTypeCode.SZArray:
                    elementType = DecodeType(ref blobReader, provider);
                    return provider.GetSZArrayType(elementType);

                case (int)SignatureTypeCode.FunctionPointer:
                    MethodSignature<TType> methodSignature = DecodeMethodSignature(ref blobReader, provider);
                    return provider.GetFunctionPointerType(methodSignature);

                case (int)SignatureTypeCode.Array:
                    return DecodeArrayType(ref blobReader, provider);

                case (int)SignatureTypeCode.RequiredModifier:
                    return DecodeModifiedType(ref blobReader, provider, isRequired: true);

                case (int)SignatureTypeCode.OptionalModifier:
                    return DecodeModifiedType(ref blobReader, provider, isRequired: false);

                case (int)SignatureTypeCode.GenericTypeInstance:
                    return DecodeGenericTypeInstance(ref blobReader, provider);

                case (int)SignatureTypeCode.GenericTypeParameter:
                    index = blobReader.ReadCompressedInteger();
                    return provider.GetGenericTypeParameter(index);

                case (int)SignatureTypeCode.GenericMethodParameter:
                    index = blobReader.ReadCompressedInteger();
                    return provider.GetGenericMethodParameter(index);

                case 0x11://(int)CorElementType.ELEMENT_TYPE_CLASS
                    return DecodeTypeHandle(ref blobReader, provider, false);

                case 0x12: //(int)CorElementType.ELEMENT_TYPE_VALUETYPE:
                    return DecodeTypeHandle(ref blobReader, provider, true);

                default:
                    throw new BadImageFormatException();
            }
        }

        // Decodes a list of types preceded by their count as a compressed integer.
        private static ImmutableArray<TType> DecodeTypes<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider)
        {
            int count = blobReader.ReadCompressedInteger();
            if (count == 0)
            {
                return ImmutableArray<TType>.Empty;
            }

            var types = new TType[count];

            for (int i = 0; i < count; i++)
            {
                types[i] = DecodeType(ref blobReader, provider);
            }

            return ImmutableArray.Create(types);
        }

        /// <summary>
        /// Decodes a method signature blob.
        /// </summary>
        /// <param name="handle">Handle to the method signature.</param>
        /// <returns>The decoded method signature.</returns>
        /// <param name="provider">The type provider.</param>
        /// <exception cref="System.BadImageFormatException">The method signature is invalid.</exception>
        public static MethodSignature<TType> DecodeMethodSignature<TType>(BlobHandle handle, ISignatureTypeProvider<TType> provider)
        {
            BlobReader blobReader = provider.Reader.GetBlobReader(handle);
            return DecodeMethodSignature(ref blobReader, provider);
        }

        /// <summary>
        /// Decodes a method signature blob.
        /// </summary>
        /// <param name="blobReader">BlobReader positioned at a method signature.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The decoded method signature.</returns>
        /// <exception cref="System.BadImageFormatException">The method signature is invalid.</exception>
        private static MethodSignature<TType> DecodeMethodSignature<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();

            if (header.Kind != SignatureKind.Method && header.Kind != SignatureKind.Property)
            {
                throw new BadImageFormatException();
            }

            int genericParameterCount = 0;
            if (header.IsGeneric)
            {
                genericParameterCount = blobReader.ReadCompressedInteger();
            }

            int parameterCount = blobReader.ReadCompressedInteger();
            TType returnType = DecodeType(ref blobReader, provider);

            if (parameterCount == 0)
            {
                return new MethodSignature<TType>(header, returnType, 0, genericParameterCount, ImmutableArray<TType>.Empty);
            }

            var parameterTypes = new TType[parameterCount];
            SignatureTypeCode typeCode;
            int parameterIndex;

            for (parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                var reader = blobReader;
                typeCode = reader.ReadSignatureTypeCode();

                if (typeCode == SignatureTypeCode.Sentinel)
                {
                    break;
                }
                parameterTypes[parameterIndex] = DecodeType(ref blobReader, provider);
            }

            int requiredParameterCount = parameterIndex;

            for (; parameterIndex < parameterCount; parameterIndex++)
            {
                parameterTypes[parameterIndex] = DecodeType(ref blobReader, provider);
            }

            return new MethodSignature<TType>(header, returnType, requiredParameterCount, genericParameterCount, ImmutableArray.Create(parameterTypes));
        }

        /// <summary>
        /// Decodes a method specification signature blob.
        /// </summary>
        /// <param name="handle">The handle to the method specification signature blob. See <see cref="MethodSpecification.Signature"/>.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The types used to instantiate a generic method via a method specification.</returns>
        /// <exception cref="System.BadImageFormatException">The method specification signature is invalid.</exception>
        public static ImmutableArray<TType> DecodeMethodSpecificationSignature<TType>(BlobHandle handle, ISignatureTypeProvider<TType> provider)
        {
            BlobReader blobReader = provider.Reader.GetBlobReader(handle);
            return DecodeMethodSpecificationSignature(ref blobReader, provider);
        }

        /// <summary>
        /// Decodes a method specification signature blob.
        /// </summary>
        /// <param name="blobReader">A BlobReader positioned at a valid method specification signature.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The types used to instantiate a generic method via the method specification.</returns>
        public static ImmutableArray<TType> DecodeMethodSpecificationSignature<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            if (header.Kind != SignatureKind.MethodSpecification)
            {
                throw new BadImageFormatException();
            }

            return DecodeTypes(ref blobReader, provider);
        }

        /// <summary>
        /// Decodes a local variable signature blob.
        /// </summary>
        /// <param name="handle">The local variable signature handle.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The local variable types.</returns>
        /// <exception cref="System.BadImageFormatException">The local variable signature is invalid.</exception>
        public static ImmutableArray<TType> DecodeLocalSignature<TType>(StandaloneSignatureHandle handle, ISignatureTypeProvider<TType> provider)
        {
            BlobHandle blobHandle = provider.Reader.GetStandaloneSignature(handle).Signature;
            BlobReader blobReader = provider.Reader.GetBlobReader(blobHandle);
            return DecodeLocalSignature(ref blobReader, provider);
        }

        /// <summary>
        /// Decodes a local variable signature blob and advances the reader past the signature.
        /// </summary>
        /// <param name="blobReader">The blob reader.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The local variable types.</returns>
        /// <exception cref="System.BadImageFormatException">The local variable signature is invalid.</exception>
        public static ImmutableArray<TType> DecodeLocalSignature<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            if (header.Kind != SignatureKind.LocalVariables)
            {
                throw new BadImageFormatException();
            }

            return DecodeTypes(ref blobReader, provider);
        }

        /// <summary>
        /// Decodes a field signature.
        /// </summary>
        /// <param name="handle">The field signature handle.</param>
        /// <param name="provider">The type provider.</param>
        /// <returns>The decoded field type.</returns>
        /// <exception cref="System.BadImageFormatException">The field signature is invalid.</exception>
        public static TType DecodeFieldSignature<TType>(BlobHandle handle, ISignatureTypeProvider<TType> provider)
        {
            BlobReader blobReader = provider.Reader.GetBlobReader(handle);
            return DecodeFieldSignature(ref blobReader, provider);
        }

        /// <summary>
        /// Decodes a field signature.
        /// </summary>
        /// <returns>The decoded field type.</returns>
        public static TType DecodeFieldSignature<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();

            if (header.Kind != SignatureKind.Field)
            {
                throw new BadImageFormatException();
            }

            return DecodeType(ref blobReader, provider);
        }

        // Decodes a generalized (non-SZ/vector) array type represented by the element type followed by
        // its rank and optional sizes and lower bounds.
        private static TType DecodeArrayType<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider)
        {
            TType elementType = DecodeType(ref blobReader, provider);
            int rank = blobReader.ReadCompressedInteger();
            var sizes = ImmutableArray<int>.Empty;
            var lowerBounds = ImmutableArray<int>.Empty;

            int sizesCount = blobReader.ReadCompressedInteger();
            if (sizesCount > 0)
            {
                var array = new int[sizesCount];
                for (int i = 0; i < sizesCount; i++)
                {
                    array[i] = blobReader.ReadCompressedInteger();
                }
                sizes = ImmutableArray.Create(array);
            }

            int lowerBoundsCount = blobReader.ReadCompressedInteger();
            if (lowerBoundsCount > 0)
            {
                var array = new int[lowerBoundsCount];
                for (int i = 0; i < lowerBoundsCount; i++)
                {
                    array[i] = blobReader.ReadCompressedSignedInteger();
                }
                lowerBounds = ImmutableArray.Create(array);
            }

            var arrayShape = new ArrayShape(rank, sizes, lowerBounds);
            return provider.GetArrayType(elementType, arrayShape);
        }

        // Decodes a generic type instantiation encoded as the generic type followed by the types used to instantiate it.
        private static TType DecodeGenericTypeInstance<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider)
        {
            TType genericType = DecodeType(ref blobReader, provider);
            ImmutableArray<TType> types = DecodeTypes(ref blobReader, provider);
            return provider.GetGenericInstance(genericType, types);
        }

        // Decodes a type with custom modifiers starting with the first modifier type that is required iff isRequired is passed,\
        // followed by an optional sequence of additional modifiers (<SignaureTypeCode.Required|OptionalModifier> <type>) and 
        // terminated by the unmodified type.
        private static TType DecodeModifiedType<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider, bool isRequired)
        {
            TType type = DecodeTypeHandle(ref blobReader, provider, null);
            var modifier = new CustomModifier<TType>(type, isRequired);

            ImmutableArray<CustomModifier<TType>> modifiers;
            int typeCode = blobReader.ReadCompressedInteger();

            isRequired = typeCode == (int)SignatureTypeCode.RequiredModifier;
            if (!isRequired && typeCode != (int)SignatureTypeCode.OptionalModifier)
            {
                // common case: 1 modifier.
                modifiers = ImmutableArray.Create(modifier);
            }
            else
            {
                // uncommon case: multiple modifiers.
                var builder = ImmutableArray.CreateBuilder<CustomModifier<TType>>();
                builder.Add(modifier);

                do
                {
                    type = DecodeTypeHandle(ref blobReader, provider, null);
                    modifier = new CustomModifier<TType>(type, isRequired);
                    builder.Add(modifier);
                    typeCode = blobReader.ReadCompressedInteger();
                    isRequired = typeCode == (int)SignatureTypeCode.RequiredModifier;
                } while (isRequired || typeCode == (int)SignatureTypeCode.OptionalModifier);

                modifiers = builder.ToImmutable();
            }
            TType unmodifiedType = DecodeType(ref blobReader, typeCode, provider);
            return provider.GetModifiedType(unmodifiedType, modifiers);
        }

        // Decodes a type definition, reference, or specification from the type handle at the given blob reader's current position.
        private static TType DecodeTypeHandle<TType>(ref BlobReader blobReader, ISignatureTypeProvider<TType> provider, bool? isValueType)
        {
            Handle handle = blobReader.ReadTypeHandle();
            return DecodeType(handle, provider, isValueType);
        }
    }
}
