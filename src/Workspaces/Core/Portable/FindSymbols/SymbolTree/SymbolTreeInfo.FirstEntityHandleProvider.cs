// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        /// <summary>
        /// Used to produce the simple-full-name components of a type from metadata.
        /// The name is 'simple' in that it does not contain things like backticks,
        /// generic arguments, or nested type + separators.  Instead just hte name
        /// of the type, any containing types, and the component parts of its namespace
        /// are added.  For example, for the type "X.Y.O`1.I`2, we will produce [X, Y, O, I]
        /// 
        /// </summary>
        private class FirstEntityHandleProvider : ISignatureTypeProvider<EntityHandle, object?>
        {
            public static readonly FirstEntityHandleProvider Instance = new();

            public EntityHandle GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle)
            {
                // Create a decoder to process the type specification (which happens with
                // instantiated generics).  It will call back into us to get the first handle
                // for the type def or type ref that the specification starts with.
                var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                return new SignatureDecoder<EntityHandle, object?>(this, reader, genericContext: null).DecodeType(ref sigReader);
            }

            public EntityHandle GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
                => GetTypeFromSpecification(reader, handle);

            public EntityHandle GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => handle;
            public EntityHandle GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => handle;

            // We want the first handle as is, without any handles for the generic args.
            public EntityHandle GetGenericInstantiation(EntityHandle genericType, ImmutableArray<EntityHandle> typeArguments) => genericType;

            // All the signature elements that would normally augment the passed in type will
            // just pass it along unchanged.
            public EntityHandle GetModifiedType(EntityHandle modifier, EntityHandle unmodifiedType, bool isRequired) => unmodifiedType;
            public EntityHandle GetPinnedType(EntityHandle elementType) => elementType;
            public EntityHandle GetArrayType(EntityHandle elementType, ArrayShape shape) => elementType;
            public EntityHandle GetByReferenceType(EntityHandle elementType) => elementType;
            public EntityHandle GetPointerType(EntityHandle elementType) => elementType;
            public EntityHandle GetSZArrayType(EntityHandle elementType) => elementType;

            // We'll never get function pointer types in any types we care about, so we can
            // just return the empty string.  Similarly, as we never construct generics,
            // there is no need to provide anything for the generic parameter names.
            public EntityHandle GetFunctionPointerType(MethodSignature<EntityHandle> signature) => default;
            public EntityHandle GetGenericMethodParameter(object? genericContext, int index) => default;
            public EntityHandle GetGenericTypeParameter(object? genericContext, int index) => default;

            public EntityHandle GetPrimitiveType(PrimitiveTypeCode typeCode) => default;
        }
    }
}
