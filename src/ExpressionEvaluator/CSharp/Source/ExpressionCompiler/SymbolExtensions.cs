// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class SymbolExtensions
    {
        internal static ImmutableArray<TypeParameterSymbol> GetAllTypeParameters(this MethodSymbol method)
        {
            var builder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            method.ContainingType.GetAllTypeParameters(builder);
            builder.AddRange(method.TypeParameters);
            return builder.ToImmutableAndFree();
        }

        internal static ReadOnlyCollection<byte> GetCustomTypeInfoPayload(this MethodSymbol method)
        {
            return CSharpCompilation.DynamicTransformsEncoder.Encode(method.ReturnType, method.ReturnTypeCustomModifiers.Length, RefKind.None).ToBytes();
        }

        internal static ReadOnlyCollection<byte> ToBytes(this ImmutableArray<bool> dynamicFlags)
        {
            Debug.Assert(!dynamicFlags.IsDefaultOrEmpty);

            var builder = ArrayBuilder<bool>.GetInstance(dynamicFlags.Length);
            builder.AddRange(dynamicFlags);
            var bytes = DynamicFlagsCustomTypeInfo.ToBytes(builder);
            builder.Free();
            return CustomTypeInfo.Encode(bytes, null);
        }

    }
}
