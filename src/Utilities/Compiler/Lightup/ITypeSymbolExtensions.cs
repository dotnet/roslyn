// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class ITypeSymbolExtensions
    {
        private static readonly Func<ITypeSymbol, NullableAnnotation> s_nullableAnnotation
            = LightupHelpers.CreateSymbolPropertyAccessor<ITypeSymbol, NullableAnnotation>(typeof(ITypeSymbol), nameof(NullableAnnotation), fallbackResult: Lightup.NullableAnnotation.None);

        private static readonly Func<ITypeSymbol, NullableAnnotation, ITypeSymbol> s_withNullableAnnotation
            = LightupHelpers.CreateSymbolWithPropertyAccessor<ITypeSymbol, NullableAnnotation>(typeof(ITypeSymbol), nameof(NullableAnnotation), fallbackResult: Lightup.NullableAnnotation.None);

        private static readonly Func<ITypeSymbol, bool> s_isNativeIntegerType
            = LightupHelpers.CreateSymbolPropertyAccessor<ITypeSymbol, bool>(typeof(ITypeSymbol), nameof(IsNativeIntegerType), fallbackResult: false);

        public static NullableAnnotation NullableAnnotation(this ITypeSymbol typeSymbol)
            => s_nullableAnnotation(typeSymbol);

        public static ITypeSymbol WithNullableAnnotation(this ITypeSymbol typeSymbol, NullableAnnotation nullableAnnotation)
            => s_withNullableAnnotation(typeSymbol, nullableAnnotation);

        public static bool IsNativeIntegerType(this ITypeSymbol typeSymbol)
            => s_isNativeIntegerType(typeSymbol);
    }
}
