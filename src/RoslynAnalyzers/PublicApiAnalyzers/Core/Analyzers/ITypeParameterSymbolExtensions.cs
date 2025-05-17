// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class ITypeParameterSymbolExtensions
    {
        private static readonly Func<ITypeParameterSymbol, bool> s_hasReferenceTypeConstraint
            = LightupHelpers.CreateSymbolPropertyAccessor<ITypeParameterSymbol, bool>(typeof(ITypeParameterSymbol), nameof(HasReferenceTypeConstraint), fallbackResult: false);

        private static readonly Func<ITypeParameterSymbol, NullableAnnotation> s_referenceTypeConstraintNullableAnnotation
            = LightupHelpers.CreateSymbolPropertyAccessor<ITypeParameterSymbol, NullableAnnotation>(typeof(ITypeParameterSymbol), nameof(ReferenceTypeConstraintNullableAnnotation), fallbackResult: Lightup.NullableAnnotation.None);

        public static bool HasReferenceTypeConstraint(this ITypeParameterSymbol typeParameterSymbol)
            => s_hasReferenceTypeConstraint(typeParameterSymbol);

        public static NullableAnnotation ReferenceTypeConstraintNullableAnnotation(this ITypeParameterSymbol typeParameterSymbol)
            => s_referenceTypeConstraintNullableAnnotation(typeParameterSymbol);
    }
}
