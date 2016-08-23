// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

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
        private class BaseNameProvider : ISignatureTypeProvider<object>
        {
            private static char[] s_dotSeparator = { '.' };

            private static readonly ObjectPool<BaseNameProvider> s_providerPool =
                new ObjectPool<BaseNameProvider>(() => new BaseNameProvider());

            private List<string> _nameParts;

            private BaseNameProvider()
            {
            }

            public static BaseNameProvider Allocate(List<string> nameParts)
            {
                var provider = s_providerPool.Allocate();
                Debug.Assert(provider._nameParts == null);
                provider._nameParts = nameParts;
                return provider;
            }

            public static void Free(BaseNameProvider provider)
            {
                Debug.Assert(provider._nameParts != null);
                provider._nameParts = null;
                s_providerPool.Free(provider);
            }

            public object GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                // Add the namespace and name of this type definition.  But only do this
                // for the first type we hit.  Any further types will be things like
                // type arguments, as we do not want to add those to the list.
                if (_nameParts.Count == 0)
                {
                    var typeDefinition = reader.GetTypeDefinition(handle);
                    var declaringType = typeDefinition.GetDeclaringType();
                    if (declaringType.IsNil)
                    {
                        // Not a nested type, just add the containing namespace.
                        AddNamespaceParts(reader, typeDefinition.NamespaceDefinition);
                    }
                    else
                    {
                        // We're a nested type, recurse and add the type we're declared in.
                        // It will handle adding the namespace properly.
                        GetTypeFromDefinition(reader, declaringType, rawTypeKind);
                    }

                    // Now add the simple name of the type itself.
                    _nameParts.Add(GetMetadataNameWithoutBackticks(reader, typeDefinition.Name));
                }
                return null;
            }

            private void AddNamespaceParts(
                MetadataReader reader, NamespaceDefinitionHandle namespaceHandle)
            {
                if (namespaceHandle.IsNil)
                {
                    return;
                }

                var namespaceDefinition = reader.GetNamespaceDefinition(namespaceHandle);
                AddNamespaceParts(reader, namespaceDefinition.Parent);
                _nameParts.Add(reader.GetString(namespaceDefinition.Name));
            }

            public object GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                // Add the namespace and name of this type reference.  But only do this
                // for the first type we hit.  Any further types will be things like
                // type arguments, as we do not want to add those to the list.
                if (_nameParts.Count == 0)
                {
                    var typeReference = reader.GetTypeReference(handle);
                    var namespaceString = reader.GetString(typeReference.Namespace);

                    // NOTE(cyrusn): Unfortunately, we are forced to allocate here
                    // no matter what.  The metadata reader API gives us no way to
                    // just get the component namespace parts for a namespace reference.
                    _nameParts.AddRange(namespaceString.Split(s_dotSeparator));
                    _nameParts.Add(GetMetadataNameWithoutBackticks(reader, typeReference.Name));
                }
                return null;
            }

            public object GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                // Create a decoder to process the type specification (which happens with
                // instantiated generics).  It will call back into us to get the appropriate 
                // name for the type def or type ref that the specification starts with.
                if (_nameParts.Count == 0)
                {
                    var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                    new SignatureDecoder<object>(this, reader).DecodeType(ref sigReader);
                }

                return null;
            }

            // We want the bare name as is, without any generic brackets, or backticks.
            public object GetGenericInstance(object genericType, ImmutableArray<object> typeArguments) => genericType;

            // All the signature elements that would normally augment the passed in type will
            // just pass it along unchanged.
            public object GetModifiedType(MetadataReader reader, bool isRequired, object modifier, object unmodifiedType) => unmodifiedType;
            public object GetPinnedType(object elementType) => elementType;
            public object GetArrayType(object elementType, ArrayShape shape) => elementType;
            public object GetByReferenceType(object elementType) => elementType;
            public object GetPointerType(object elementType) => elementType;
            public object GetSZArrayType(object elementType) => elementType;

            // We'll never get function pointer types in any types we care about, so we can
            // just return the empty string.  Similarly, as we never construct generics,
            // there is no need to provide anything for the generic parameter names.
            public object GetFunctionPointerType(MethodSignature<object> signature) => null;
            public object GetGenericMethodParameter(int index) => null;
            public object GetGenericTypeParameter(int index) => null;

            public object GetPrimitiveType(PrimitiveTypeCode typeCode) => null;
        }
    }
}