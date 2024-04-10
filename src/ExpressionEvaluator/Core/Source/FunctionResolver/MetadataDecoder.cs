// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct MetadataDecoder
    {
        private readonly MetadataReader _reader;
        private readonly ImmutableArray<string> _allTypeParameters;
        private readonly int _containingArity;
        private readonly ImmutableArray<string> _methodTypeParameters;

        internal MetadataDecoder(
            MetadataReader reader,
            ImmutableArray<string> allTypeParameters,
            int containingArity,
            ImmutableArray<string> methodTypeParameters)
        {
            _reader = reader;
            _allTypeParameters = allTypeParameters;
            _containingArity = containingArity;
            _methodTypeParameters = methodTypeParameters;
        }

        // cf. MetadataDecoder<>.GetSignatureForMethod.
        internal ImmutableArray<ParameterSignature> DecodeParameters(MethodDefinition methodDef)
        {
            var signatureReader = _reader.GetBlobReader(methodDef.Signature);
            var signatureHeader = signatureReader.ReadSignatureHeader();
            var typeParameterCount = signatureHeader.IsGeneric ? signatureReader.ReadCompressedInteger() : 0;
            var parameterCount = signatureReader.ReadCompressedInteger();
            var builder = ImmutableArray.CreateBuilder<ParameterSignature>(parameterCount);
            var returnType = DecodeParameter(ref signatureReader);
            for (int i = 0; i < parameterCount; i++)
            {
                builder.Add(DecodeParameter(ref signatureReader));
            }
            return builder.ToImmutable();
        }

        // cf. MetadataDecoder<>.DecodeParameterOrThrow.
        private ParameterSignature DecodeParameter(ref BlobReader signatureReader)
        {
            bool isByRef = false;
            while (true)
            {
                var typeCode = signatureReader.ReadSignatureTypeCode();
                switch (typeCode)
                {
                    case SignatureTypeCode.RequiredModifier:
                    case SignatureTypeCode.OptionalModifier:
                        // Skip modifiers.
                        break;
                    case SignatureTypeCode.ByReference:
                        isByRef = true;
                        break;
                    default:
                        var type = DecodeType(ref signatureReader, typeCode);
                        return new ParameterSignature(type, isByRef);
                }
            }
        }

        // cf. MetadataDecoder<>.DecodeTypeOrThrow.
        private TypeSignature DecodeType(ref BlobReader signatureReader, SignatureTypeCode typeCode)
        {
            switch (typeCode)
            {
                case SignatureTypeCode.TypeHandle:
                    {
                        int typeArgumentOffset = 0;
                        return DecodeType(signatureReader.ReadTypeHandle(), [], ref typeArgumentOffset);
                    }
                case SignatureTypeCode.Array:
                    {
                        var elementType = DecodeModifiersAndType(ref signatureReader);
                        int rank;
                        int sizes;
                        signatureReader.TryReadCompressedInteger(out rank);
                        signatureReader.TryReadCompressedInteger(out sizes);
                        if (sizes != 0)
                        {
                            throw UnhandledMetadata();
                        }
                        return new ArrayTypeSignature(elementType, rank);
                    }
                case SignatureTypeCode.SZArray:
                    {
                        var elementType = DecodeModifiersAndType(ref signatureReader);
                        return new ArrayTypeSignature(elementType, 1);
                    }
                case SignatureTypeCode.GenericTypeInstance:
                    return DecodeGenericTypeInstance(ref signatureReader);
                case SignatureTypeCode.Pointer:
                    {
                        var pointedAtType = DecodeModifiersAndType(ref signatureReader);
                        return new PointerTypeSignature(pointedAtType);
                    }
                case SignatureTypeCode.GenericTypeParameter:
                    return DecodeGenericTypeParameter(ref signatureReader, _allTypeParameters, _containingArity);
                case SignatureTypeCode.GenericMethodParameter:
                    return DecodeGenericTypeParameter(ref signatureReader, _methodTypeParameters, 0);
                default:
                    {
                        var signature = typeCode.ToSpecialType().GetTypeSignature();
                        if (signature == null)
                        {
                            throw UnhandledMetadata();
                        }
                        return signature;
                    }
            }
        }

        // Ignore modifiers and decode type.
        private TypeSignature DecodeModifiersAndType(ref BlobReader signatureReader)
        {
            while (true)
            {
                var typeCode = signatureReader.ReadSignatureTypeCode();
                switch (typeCode)
                {
                    case SignatureTypeCode.RequiredModifier:
                    case SignatureTypeCode.OptionalModifier:
                        // Skip modifiers.
                        break;
                    default:
                        return DecodeType(ref signatureReader, typeCode);
                }
            }
        }

        private TypeSignature DecodeGenericTypeParameter(
            ref BlobReader signatureReader,
            ImmutableArray<string> typeParameters,
            int containingArity)
        {
            int index = signatureReader.ReadCompressedInteger();
            if (index < containingArity)
            {
                // Unspecified type parameter.
                throw UnhandledMetadata();
            }
            var name = typeParameters[index - containingArity];
            return new QualifiedTypeSignature(null, name);
        }

        // cf. MetadataDecoder<>.DecodeGenericTypeInstanceOrThrow.
        private TypeSignature DecodeGenericTypeInstance(ref BlobReader signatureReader)
        {
            var typeCode = signatureReader.ReadSignatureTypeCode();
            var typeHandle = signatureReader.ReadTypeHandle();
            var typeArguments = DecodeGenericTypeArguments(ref signatureReader);
            int typeArgumentOffset = 0;
            var type = DecodeType(typeHandle, typeArguments, ref typeArgumentOffset);
            if (typeArgumentOffset != typeArguments.Length)
            {
                // Generic type reference names must include arity
                // to avoid loading referenced assemblies.
                throw UnhandledMetadata();
            }
            return type;
        }

        private ImmutableArray<TypeSignature> DecodeGenericTypeArguments(ref BlobReader signatureReader)
        {
            int typeArgCount;
            signatureReader.TryReadCompressedInteger(out typeArgCount);
            var builder = ImmutableArray.CreateBuilder<TypeSignature>(typeArgCount);
            for (int i = 0; i < typeArgCount; i++)
            {
                var typeArg = DecodeModifiersAndType(ref signatureReader);
                builder.Add(typeArg);
            }
            return builder.ToImmutable();
        }

        // cf. MetadataDecoder<>.GetSymbolForTypeHandleOrThrow.
        private TypeSignature DecodeType(
            EntityHandle handle,
            ImmutableArray<TypeSignature> typeArguments,
            ref int typeArgumentOffset)
        {
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return DecodeTypeDefinition((TypeDefinitionHandle)handle, typeArguments, ref typeArgumentOffset);
                case HandleKind.TypeReference:
                    return DecodeTypeReference((TypeReferenceHandle)handle, typeArguments, ref typeArgumentOffset);
                default:
                    throw new BadImageFormatException();
            }
        }

        // cf. MetadataDecoder<>.GetTypeOfTypeDef.
        private TypeSignature DecodeTypeDefinition(
            TypeDefinitionHandle handle,
            ImmutableArray<TypeSignature> typeArguments,
            ref int typeArgumentOffset)
        {
            var typeDef = _reader.GetTypeDefinition(handle);
            TypeSignature qualifier;
            var declaringTypeHandle = typeDef.GetDeclaringType();
            if (declaringTypeHandle.IsNil)
            {
                // Include namespace.
                qualifier = GetNamespace(typeDef.Namespace);
            }
            else
            {
                // Include declaring type.
                qualifier = DecodeTypeDefinition(declaringTypeHandle, typeArguments, ref typeArgumentOffset);
            }
            return CreateTypeSignature(qualifier, _reader.GetString(typeDef.Name), typeArguments, ref typeArgumentOffset);
        }

        // cf. MetadataDecoder<>.GetTypeOfTypeRef.
        private TypeSignature DecodeTypeReference(
            TypeReferenceHandle handle,
            ImmutableArray<TypeSignature> typeArguments,
            ref int typeArgumentOffset)
        {
            var typeRef = _reader.GetTypeReference(handle);
            TypeSignature qualifier;
            var scope = typeRef.ResolutionScope;
            switch (scope.Kind)
            {
                case HandleKind.AssemblyReference:
                case HandleKind.ModuleReference:
                    // Include namespace.
                    qualifier = GetNamespace(typeRef.Namespace);
                    break;
                case HandleKind.TypeReference:
                    // Include declaring type.
                    qualifier = DecodeTypeReference((TypeReferenceHandle)scope, typeArguments, ref typeArgumentOffset);
                    break;
                default:
                    throw new BadImageFormatException();
            }
            return CreateTypeSignature(qualifier, _reader.GetString(typeRef.Name), typeArguments, ref typeArgumentOffset);
        }

        private QualifiedTypeSignature GetNamespace(StringHandle namespaceHandle)
        {
            var namespaceName = _reader.GetString(namespaceHandle);
            if (string.IsNullOrEmpty(namespaceName))
            {
                return null;
            }
            QualifiedTypeSignature signature = null;
            var parts = namespaceName.Split('.');
            foreach (var part in parts)
            {
                signature = new QualifiedTypeSignature(signature, part);
            }
            return signature;
        }

        private static TypeSignature CreateTypeSignature(
            TypeSignature qualifier,
            string typeName,
            ImmutableArray<TypeSignature> typeArguments,
            ref int typeArgumentOffset)
        {
            int arity;
            typeName = RemoveAritySeparatorIfAny(typeName, out arity);
            var qualifiedName = new QualifiedTypeSignature(qualifier, typeName);
            if (arity == 0)
            {
                return qualifiedName;
            }
            typeArguments = ImmutableArray.Create(typeArguments, typeArgumentOffset, arity);
            typeArgumentOffset += arity;
            return new GenericTypeSignature(qualifiedName, typeArguments);
        }

        private static string RemoveAritySeparatorIfAny(string typeName, out int arity)
        {
            arity = 0;
            int index = typeName.LastIndexOf('`');
            if (index < 0)
            {
                return typeName;
            }
            int n;
            if (int.TryParse(typeName.Substring(index + 1), out n))
            {
                arity = n;
            }
            return typeName.Substring(0, index);
        }

        private static Exception UnhandledMetadata()
        {
            return new NotSupportedException();
        }
    }
}
