// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    // PROTOTYPE(NullableReferenceTypes): external annotations should be removed or fully designed/productized
    internal static class ExtraAnnotations
    {
        // APIs that are useful to annotate:
        //   1) don't accept null input
        //   2) return a reference type
        private static readonly ImmutableDictionary<string, ImmutableArray<ImmutableArray<bool>>> Annotations =
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
            }.ToImmutableDictionary();

        internal static string MakeMethodKey(PEMethodSymbol method, ParamInfo<TypeSymbol>[] paramInfo)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();

            StringBuilder builder = pooledBuilder.Builder;
            Add(paramInfo[0].Type, builder);
            builder.Append(' ');

            Add(method.ContainingType, builder);
            builder.Append('.');

            builder.Append(method.Name);
            builder.Append('(');

            for (int i = 1; i < paramInfo.Length; i++)
            {
                Add(paramInfo[i].Type, builder);
                if (i < paramInfo.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(')');
            return pooledBuilder.ToStringAndFree();

            // PROTOTYPE(NullableReferenceTypes): Many cases are not yet handled
            // generic type args
            // ref kind
            // 'this'
            // static vs. instance
            // use assembly qualified name format (used in metadata) rather than symbol display?
        }

        /// <summary>
        /// All types in a member which can be annotated should be annotated. Value types and void can be skipped.
        /// </summary>
        private static readonly ImmutableArray<bool> skip = default;

        private static ImmutableArray<ImmutableArray<bool>> Parameters(params ImmutableArray<bool>[] values)
            => values.ToImmutableArray();

        private static ImmutableArray<bool> Nullable(params bool[] values)
        {
            Debug.Assert(values.Length > 0);
            return values.ToImmutableArray();
        }

        internal static ImmutableArray<ImmutableArray<bool>> GetExtraAnnotations(string key)
        {
            if (!Annotations.TryGetValue(key, out var flags))
            {
                return default;
            }

            return flags;
        }

        private static void Add(TypeSymbol type, StringBuilder builder)
            => builder.Append(
                type.ToDisplayString(
                    SymbolDisplayFormat.CSharpErrorMessageFormat
                        .RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)
                        // displaying tuple syntax causes to load the members of ValueTuple, which can cause a cycle, so we use long-hand format instead
                        .WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeNullableTypeModifier | SymbolDisplayCompilerInternalOptions.UseValueTuple)));
    }
}
