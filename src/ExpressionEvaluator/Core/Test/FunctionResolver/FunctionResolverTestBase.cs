// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    public abstract class FunctionResolverTestBase : CSharpTestBase
    {
        internal static void Resolve(Process process, Resolver resolver, RequestSignature signature, string[] expectedSignatures)
        {
            var request = new Request(null, signature);
            resolver.EnableResolution(process, request);
            VerifySignatures(request, expectedSignatures);
        }

        internal static void VerifySignatures(Request request, params string[] expectedSignatures)
        {
            var actualSignatures = request.GetResolvedAddresses().Select(a => GetMethodSignature(a.Module, a.Token));
            AssertEx.Equal(expectedSignatures, actualSignatures);
        }

        private static string GetMethodSignature(Module module, int token)
        {
            var reader = module.GetMetadataReader();
            return GetMethodSignature(reader, MetadataTokens.MethodDefinitionHandle(token));
        }

        private static string GetMethodSignature(MetadataReader reader, MethodDefinitionHandle handle)
        {
            var methodDef = reader.GetMethodDefinition(handle);
            var builder = new StringBuilder();
            var typeDef = reader.GetTypeDefinition(methodDef.GetDeclaringType());
            var allTypeParameters = typeDef.GetGenericParameters();
            AppendTypeName(builder, reader, typeDef);
            builder.Append('.');
            builder.Append(reader.GetString(methodDef.Name));
            var methodTypeParameters = methodDef.GetGenericParameters();
            AppendTypeParameters(builder, DecodeTypeParameters(reader, offset: 0, typeParameters: methodTypeParameters));
            var decoder = new MetadataDecoder(
                reader,
                GetTypeParameterNames(reader, allTypeParameters),
                0,
                GetTypeParameterNames(reader, methodTypeParameters));
            try
            {
                AppendParameters(builder, decoder.DecodeParameters(methodDef));
            }
            catch (NotSupportedException)
            {
                builder.Append("([notsupported])");
            }
            return builder.ToString();
        }

        private static ImmutableArray<string> GetTypeParameterNames(MetadataReader reader, GenericParameterHandleCollection handles)
        {
            return ImmutableArray.CreateRange(handles.Select(h => reader.GetString(reader.GetGenericParameter(h).Name)));
        }

        private static void AppendTypeName(StringBuilder builder, MetadataReader reader, TypeDefinition typeDef)
        {
            var declaringTypeHandle = typeDef.GetDeclaringType();
            int declaringTypeArity;
            if (declaringTypeHandle.IsNil)
            {
                declaringTypeArity = 0;
                var namespaceName = reader.GetString(typeDef.Namespace);
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    builder.Append(namespaceName);
                    builder.Append('.');
                }
            }
            else
            {
                var declaringType = reader.GetTypeDefinition(declaringTypeHandle);
                declaringTypeArity = declaringType.GetGenericParameters().Count;
                AppendTypeName(builder, reader, declaringType);
                builder.Append('.');
            }
            var typeName = reader.GetString(typeDef.Name);
            int index = typeName.IndexOf('`');
            if (index >= 0)
            {
                typeName = typeName.Substring(0, index);
            }
            builder.Append(typeName);
            AppendTypeParameters(builder, DecodeTypeParameters(reader, declaringTypeArity, typeDef.GetGenericParameters()));
        }

        private static void AppendTypeParameters(StringBuilder builder, ImmutableArray<string> typeParameters)
        {
            if (typeParameters.Length > 0)
            {
                builder.Append('<');
                AppendCommaSeparatedList(builder, typeParameters, (b, t) => b.Append(t));
                builder.Append('>');
            }
        }

        private static void AppendParameters(StringBuilder builder, ImmutableArray<ParameterSignature> parameters)
        {
            builder.Append('(');
            AppendCommaSeparatedList(builder, parameters, AppendParameter);
            builder.Append(')');
        }

        private static void AppendParameter(StringBuilder builder, ParameterSignature signature)
        {
            if (signature.IsByRef)
            {
                builder.Append("ref ");
            }
            AppendType(builder, signature.Type);
        }

        private static void AppendType(StringBuilder builder, TypeSignature signature)
        {
            switch (signature.Kind)
            {
                case TypeSignatureKind.GenericType:
                    {
                        var genericName = (GenericTypeSignature)signature;
                        AppendType(builder, genericName.QualifiedName);
                        AppendTypeArguments(builder, genericName.TypeArguments);
                    }
                    break;
                case TypeSignatureKind.QualifiedType:
                    {
                        var qualifiedName = (QualifiedTypeSignature)signature;
                        var qualifier = qualifiedName.Qualifier;
                        if (qualifier != null)
                        {
                            AppendType(builder, qualifier);
                            builder.Append('.');
                        }
                        builder.Append(qualifiedName.Name);
                    }
                    break;
                case TypeSignatureKind.ArrayType:
                    {
                        var arrayType = (ArrayTypeSignature)signature;
                        AppendType(builder, arrayType.ElementType);
                        builder.Append('[');
                        builder.Append(',', arrayType.Rank - 1);
                        builder.Append(']');
                    }
                    break;
                case TypeSignatureKind.PointerType:
                    AppendType(builder, ((PointerTypeSignature)signature).PointedAtType);
                    builder.Append('*');
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        private static void AppendTypeArguments(StringBuilder builder, ImmutableArray<TypeSignature> typeArguments)
        {
            if (typeArguments.Length > 0)
            {
                builder.Append('<');
                AppendCommaSeparatedList(builder, typeArguments, AppendType);
                builder.Append('>');
            }
        }

        private static void AppendCommaSeparatedList<T>(StringBuilder builder, ImmutableArray<T> items, Action<StringBuilder, T> appendItem)
        {
            bool any = false;
            foreach (var item in items)
            {
                if (any)
                {
                    builder.Append(", ");
                }
                appendItem(builder, item);
                any = true;
            }
        }

        private static ImmutableArray<string> DecodeTypeParameters(MetadataReader reader, int offset, GenericParameterHandleCollection typeParameters)
        {
            int arity = typeParameters.Count - offset;
            Debug.Assert(arity >= 0);
            if (arity == 0)
            {
                return ImmutableArray<string>.Empty;
            }
            var builder = ImmutableArray.CreateBuilder<string>(arity);
            for (int i = 0; i < arity; i++)
            {
                var handle = typeParameters[offset + i];
                var typeParameter = reader.GetGenericParameter(handle);
                builder.Add(reader.GetString(typeParameter.Name));
            }
            return builder.ToImmutable();
        }
    }
}
