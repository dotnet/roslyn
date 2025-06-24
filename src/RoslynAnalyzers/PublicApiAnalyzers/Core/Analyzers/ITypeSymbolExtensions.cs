﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class ITypeSymbolExtensions
    {
        private static readonly Func<ITypeSymbol, NullableAnnotation> s_nullableAnnotation
            = LightupHelpers.CreateSymbolPropertyAccessor<ITypeSymbol, NullableAnnotation>(typeof(ITypeSymbol), nameof(NullableAnnotation), fallbackResult: Lightup.NullableAnnotation.None);

        public static NullableAnnotation NullableAnnotation(this ITypeSymbol typeSymbol)
            => s_nullableAnnotation(typeSymbol);
    }
}
