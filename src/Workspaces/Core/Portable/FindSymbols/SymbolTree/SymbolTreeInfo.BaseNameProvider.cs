// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        /// <summary>
        /// Used to determine a simple name for a type that is referenced through
        /// a TypeSpecificationHandle.  BEcause we only care about hte base name
        /// (i.e. "IList", not IList`1 or S.C.G.IList or IList&lt;Int32&gt; or 
        /// IList[] or Foo(IList), etc.) we provide simple dummy implementations
        /// for most methods. 
        /// </summary>
        private class BaseNameProvider : ISignatureTypeProvider<string>
        {
            public static readonly BaseNameProvider Instance = new BaseNameProvider();

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                // Just get the simple name of the type definition.
                return GetMetadataNameWithoutBackticks(
                    reader, reader.GetTypeDefinition(handle).Name);
            }

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                // Just get the simple name of the type definition.
                return GetMetadataNameWithoutBackticks(
                    reader, reader.GetTypeReference(handle).Name);
            }

            public string GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                // Create a decoder to process the type specification (which happens with
                // instantiated generics).  It will call back into us to get the appropriate 
                // name for the type def or type ref that the specification starts with.
                var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                return new SignatureDecoder<string>(this, reader).DecodeType(ref sigReader);
            }

            // We want the bare name as is, without any generic brackets, or backticks.
            public string GetGenericInstance(string genericType, ImmutableArray<string> typeArguments) => genericType;

            // All the signature elements that would normally augment the passed in type will
            // just pass it along unchanged.
            public string GetModifiedType(MetadataReader reader, bool isRequired, string modifier, string unmodifiedType) => unmodifiedType;
            public string GetPinnedType(string elementType) => elementType;
            public string GetArrayType(string elementType, ArrayShape shape) => elementType;
            public string GetByReferenceType(string elementType) => elementType;
            public string GetPointerType(string elementType) => elementType;
            public string GetSZArrayType(string elementType) => elementType;

            // We'll never get function pointer types in any types we care about, so we can
            // just return the empty string.  Similarly, as we never construct generics,
            // there is no need to provide anything for the generic parameter names.
            public string GetFunctionPointerType(MethodSignature<string> signature) => "";
            public string GetGenericMethodParameter(int index) => "";
            public string GetGenericTypeParameter(int index) => "";

            public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        }
    }
}