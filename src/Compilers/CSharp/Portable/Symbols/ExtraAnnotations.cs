// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.PooledObjects;
using static Microsoft.CodeAnalysis.CSharp.Symbols.FlowAnalysisAnnotations;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // https://github.com/dotnet/roslyn/issues/29821 external annotations should be removed or fully designed/productized
    //  If we choose to stick with an ad-hoc key (rather than annotations as source or as PE/ref assembly),
    //  we should consider the assembly qualified name format used in metadata (with backticks and such).
    internal static class ExtraAnnotations
    {
        // APIs that are useful to annotate:
        //   1) don't accept null input
        //   2) return a reference type
        // All types in a member which can be annotated should be annotated. Value types and void can be skipped (with a `default`)
        private static readonly ImmutableDictionary<string, ImmutableArray<ImmutableArray<byte>>> Annotations =
            new Dictionary<string, ImmutableArray<ImmutableArray<byte>>>
            {
                { "System.Boolean System.Boolean.Parse(System.String)", Array(default, Nullable(false)) },
                { "System.Void System.Buffer.BlockCopy(System.Array, System.Int32, System.Array, System.Int32, System.Int32)", Array(default, Nullable(false), default, Nullable(false), default, default) },
                { "System.Int32 System.Buffer.ByteLength(System.Array)", Array(default, Nullable(false)) },
                { "System.Byte System.Buffer.GetByte(System.Array, System.Int32)", Array(default, Nullable(false), default) },
                { "System.Void System.Buffer.SetByte(System.Array, System.Int32, System.Byte)", Array(default, Nullable(false), default, default) },
                { "System.Byte System.Byte.Parse(System.String)", Array(default, Nullable(false)) },
                { "System.Byte System.Byte.Parse(System.String, System.IFormatProvider)", Array(default, Nullable(false), default) },
                { "System.Byte System.Byte.Parse(System.String, System.Globalization.NumberStyles)", Array(default, Nullable(false), Nullable(false)) },
                { "System.Byte System.Byte.Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", Array(default, Nullable(false), Nullable(false), default) },
                { "System.String System.String.Concat(System.String, System.String)", Array(Nullable(false), Nullable(true), Nullable(true)) },
            }.ToImmutableDictionary();

        private static readonly ImmutableDictionary<string, ImmutableArray<FlowAnalysisAnnotations>> Attributes =
            new Dictionary<string, ImmutableArray<FlowAnalysisAnnotations>>
            {
                { "System.Boolean System.String.IsNullOrEmpty(System.String)", Array(default, NotNullWhenFalse) },
                { "System.Boolean System.String.IsNullOrWhiteSpace(System.String)", Array(default, NotNullWhenFalse) },
                { "System.Boolean System.String.Contains(System.String)", Array(default, EnsuresNotNull) },
                { "System.Void System.Diagnostics.Debug.Assert(System.Boolean)", Array(default, AssertsTrue) },
                { "System.Void System.Diagnostics.Debug.Assert(System.Boolean, System.String)", Array(default, AssertsTrue, default) },
                { "System.Void System.Diagnostics.Debug.Assert(System.Boolean, System.String, System.String)", Array(default, AssertsTrue, default, default) },
                { "System.Void System.Diagnostics.Debug.Assert(System.Boolean, System.String, System.String, System.Object[])", Array(default, AssertsTrue, default, default, default) },
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

            // https://github.com/dotnet/roslyn/issues/29821: Many cases are not yet handled
            // generic type args
            // ref kind
            // 'this'
            // static vs. instance
            // use assembly qualified name format (used in metadata) rather than symbol display?
        }

        internal static string MakeMethodKey(MethodSymbol method)
        {
            var containingType = method.ContainingSymbol as TypeSymbol;
            if (containingType is null)
            {
                return null;
            }

            var pooledBuilder = PooledStringBuilder.GetInstance();

            StringBuilder builder = pooledBuilder.Builder;
            Add(method.ReturnType.TypeSymbol, builder);
            builder.Append(' ');

            Add(containingType, builder);
            builder.Append('.');

            builder.Append(method.Name);
            builder.Append('(');

            var parameterTypes = method.ParameterTypes;
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                Add(parameterTypes[i].TypeSymbol, builder);
                if (i < parameterTypes.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(')');
            return pooledBuilder.ToStringAndFree();

            // https://github.com/dotnet/roslyn/issues/29821: Many cases are not yet handled
            // generic type args
            // ref kind
            // 'this'
            // static vs. instance
            // use assembly qualified name format (used in metadata) rather than symbol display?
        }

        private static ImmutableArray<ImmutableArray<byte>> Array(params ImmutableArray<byte>[] values)
            => values.ToImmutableArray();

        private static ImmutableArray<FlowAnalysisAnnotations> Array(params FlowAnalysisAnnotations[] values)
            => values.ToImmutableArray();

        private static ImmutableArray<byte> Nullable(bool isNullable)
        {
            return ImmutableArray.Create((byte)(isNullable ? NullableAnnotation.Annotated : NullableAnnotation.NotAnnotated));
        }

        internal static ImmutableArray<ImmutableArray<byte>> GetExtraAnnotations(string key)
        {
            if (key is null)
            {
                return default;
            }

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
                        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)
                        // displaying tuple syntax causes to load the members of ValueTuple, which can cause a cycle, so we use long-hand format instead
                        .WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UseValueTuple)));

        /// <summary>
        /// index 0 is used for return type
        /// other parameters follow
        /// If there are no annotations on the member (not just that parameter), then returns null. The purpose is to ensure
        /// that if some annotations are present on the member, then annotations win over the attributes on the member in all positions.
        /// That could mean removing an attribute.
        /// </summary>
        internal static FlowAnalysisAnnotations? TryGetExtraAttributes(string key, int parameterIndex)
        {
            if (key is null)
            {
                return null;
            }

            if (!Attributes.TryGetValue(key, out var extraAttributes))
            {
                return null;
            }

            return extraAttributes[parameterIndex + 1];
        }
    }
}
