// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
            var dynamicFlags = CSharpCompilation.DynamicTransformsEncoder.Encode(method.ReturnType.TypeSymbol, method.ReturnType.CustomModifiers.Length, RefKind.None);
            var dynamicFlagsInfo = DynamicFlagsCustomTypeInfo.Create(dynamicFlags);
            return dynamicFlagsInfo.GetCustomTypeInfoPayload();
        }
    }
}
