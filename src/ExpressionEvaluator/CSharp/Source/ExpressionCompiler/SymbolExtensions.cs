// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
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
            bool[] dynamicFlags = CSharpCompilation.DynamicTransformsEncoder.Encode(method.ReturnType, method.ReturnTypeCustomModifiers.Length, RefKind.None).ToArray();
            var dynamicFlagsInfo = new DynamicFlagsCustomTypeInfo(dynamicFlags);
            return dynamicFlagsInfo.GetCustomTypeInfoPayload();
        }
    }
}
