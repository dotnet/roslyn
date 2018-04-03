// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    // PROTOTYPE(NullableReferenceTypes): external annotations should be removed or fully designed/productized 
    internal class ExtraAnnotations
    {
        // APIs that are useful to annotate:
        //   1) don't accept null input
        //   2) can return null
        //   3) never return null
        internal static readonly Dictionary<string, ImmutableArray<ImmutableArray<bool>>> Annotations =
            new Dictionary<string, ImmutableArray<ImmutableArray<bool>>>
            {
                { "System.Boolean System.Boolean.Parse(System.String)", Parameters(skip, Nullable(false)) }
            };

        internal static string MakeKey(PEModuleSymbol moduleSymbol, Handle handle)
        {
            var metadata = moduleSymbol.Module.MetadataReader;
            var builder = PooledStringBuilder.GetInstance();

            if (handle.Kind == HandleKind.MethodDefinition)
            {
                Add((MethodDefinitionHandle)handle, metadata, builder, moduleSymbol);
            }

            return builder.ToStringAndFree();
        }

        private static readonly ImmutableArray<bool> skip = default;

        static ImmutableArray<ImmutableArray<bool>> Parameters(params ImmutableArray<bool>[] values)
        {
            return values.ToImmutableArray();
        }

        static ImmutableArray<bool> Nullable(params bool[] values)
        {
            return values.ToImmutableArray();
        }

        internal static ImmutableArray<bool> GetExtraAnnotations(string key, int ordinal)
        {
            if (!Annotations.TryGetValue(key, out var flags))
            {
                return default;
            }

            return flags[ordinal];
        }

        private static void Add(MethodDefinitionHandle methodHandle, MetadataReader metadata, PooledStringBuilder builder, PEModuleSymbol moduleSymbol)
        {
            var metadataDecoder = new MetadataDecoder(moduleSymbol);
            SignatureHeader signatureHeader;
            BadImageFormatException mrEx;
            ParamInfo<TypeSymbol>[] methodParams = metadataDecoder.GetSignatureForMethod(methodHandle, out signatureHeader, out mrEx, setParamHandles: false);

            MethodDefinition method = metadata.GetMethodDefinition(methodHandle);

            TypeSymbol type1 = methodParams[0].Type;
            Add(type1, builder);

            builder.Builder.Append(' ');

            TypeDefinition type = metadata.GetTypeDefinition(method.GetDeclaringType());
            Add(type, metadata, builder);
            builder.Builder.Append('.');

            string name = metadata.GetString(method.Name);
            builder.Builder.Append(name);
            builder.Builder.Append('(');

            for (int i = 1; i < methodParams.Length; i++)
            {
                Add(methodParams[i].Type, builder);
                if (i < methodParams.Length - 1)
                {
                    builder.Builder.Append(", ");
                }
            }

            builder.Builder.Append(')');

            // PROTOTYPE(NullableReferenceTypes): Many cases are not yet handled
            // generic type args
            // ref kind
            // 'this'
        }

        private static void Add(TypeSymbol type1, PooledStringBuilder builder) =>
                        builder.Builder.Append(type1.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

        private static void Add(TypeDefinition type, MetadataReader metadata, PooledStringBuilder builder)
        {
            string ns = metadata.GetString(type.Namespace);
            builder.Builder.Append(ns);
            builder.Builder.Append('.');

            string name = metadata.GetString(type.Name);
            builder.Builder.Append(name);
        }
    }
}
