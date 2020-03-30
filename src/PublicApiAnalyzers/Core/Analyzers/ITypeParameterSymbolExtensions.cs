// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
