// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
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
                { "System.Boolean System.Boolean.Parse(System.String)", Parameters(skip, Nullable(false)) },
                { "System.Void System.Buffer.BlockCopy(System.Array, System.Int32, System.Array, System.Int32, System.Int32)", Parameters(skip, Nullable(false), skip, Nullable(false), skip, skip) },
                { "System.Int32 System.Buffer.ByteLength(System.Array)", Parameters(skip, Nullable(false)) },
                { "System.Byte System.Buffer.GetByte(System.Array, System.Int32)", Parameters(skip, Nullable(false), skip) },
                { "System.Void System.Buffer.SetByte(System.Array, System.Int32, System.Byte)", Parameters(skip, Nullable(false), skip, skip) },
                { "System.Byte System.Byte.Parse(System.String)", Parameters(skip, Nullable(false)) },
                { "System.Byte System.Byte.Parse(System.String, System.IFormatProvider)", Parameters(skip, Nullable(false), skip) },
                { "System.Byte System.Byte.Parse(System.String, System.Globalization.NumberStyles)", Parameters(skip, Nullable(false), Nullable(false)) },
                { "System.Byte System.Byte.Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", Parameters(skip, Nullable(false), Nullable(false), skip) },
                { "System.String System.String.Concat(System.String, System.String)", Parameters(Nullable(false), Nullable(true), Nullable(true)) },
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

        /// <summary>
        /// All types in a member should be annotated, except for struct or enum types, which can be skipped.
        /// </summary>
        private static readonly ImmutableArray<bool> skip = ImmutableArray<bool>.Empty;

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

        private static void Add(MethodDefinitionHandle methodHandle, MetadataReader metadata, StringBuilder builder, PEModuleSymbol moduleSymbol)
        {
            var metadataDecoder = new MetadataDecoder(moduleSymbol);
            SignatureHeader signatureHeader;
            BadImageFormatException mrEx;
            ParamInfo<TypeSymbol>[] methodParams = metadataDecoder.GetSignatureForMethod(methodHandle, out signatureHeader, out mrEx, setParamHandles: false);

            MethodDefinition method = metadata.GetMethodDefinition(methodHandle);

            TypeSymbol type1 = methodParams[0].Type;
            Add(type1, builder);

            builder.Append(' ');

            TypeDefinition type = metadata.GetTypeDefinition(method.GetDeclaringType());
            Add(type, metadata, builder);
            builder.Append('.');

            string name = metadata.GetString(method.Name);
            builder.Append(name);
            builder.Append('(');

            for (int i = 1; i < methodParams.Length; i++)
            {
                Add(methodParams[i].Type, builder);
                if (i < methodParams.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(')');

            // PROTOTYPE(NullableReferenceTypes): Many cases are not yet handled
            // generic type args
            // ref kind
            // 'this'
            // static vs. instance
        }

        private static void Add(TypeSymbol type1, StringBuilder builder) =>
                        builder.Append(type1.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

        private static void Add(TypeDefinition type, MetadataReader metadata, StringBuilder builder)
        {
            string ns = metadata.GetString(type.Namespace);
            builder.Append(ns);
            builder.Append('.');

            string name = metadata.GetString(type.Name);
            builder.Append(name);
        }
    }
}
