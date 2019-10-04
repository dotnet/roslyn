// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        private sealed class TypeParameterSymbolWithNullableAnnotation : TypeSymbolWithNullableAnnotation, ITypeParameterSymbol
        {
            public TypeParameterSymbolWithNullableAnnotation(ITypeSymbol wrappedSymbol, NullableAnnotation nullability) : base(wrappedSymbol, nullability)
            {
            }


            private new ITypeParameterSymbol WrappedSymbol => (ITypeParameterSymbol)base.WrappedSymbol;

            ITypeParameterSymbol ITypeParameterSymbol.OriginalDefinition => WrappedSymbol.OriginalDefinition;

            public override void Accept(SymbolVisitor visitor)
            {
                visitor.VisitTypeParameter(this);
            }

            [return: MaybeNull]
            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
                return visitor.VisitTypeParameter(this);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            }

            #region ITypeParameterSymbol Implementation Forwards

            public int Ordinal => WrappedSymbol.Ordinal;

            public VarianceKind Variance => WrappedSymbol.Variance;

            public TypeParameterKind TypeParameterKind => WrappedSymbol.TypeParameterKind;

            public IMethodSymbol? DeclaringMethod => WrappedSymbol.DeclaringMethod;

            public INamedTypeSymbol? DeclaringType => WrappedSymbol.DeclaringType;

            public bool HasReferenceTypeConstraint => WrappedSymbol.HasReferenceTypeConstraint;

            public NullableAnnotation ReferenceTypeConstraintNullableAnnotation => WrappedSymbol.ReferenceTypeConstraintNullableAnnotation;

            public bool HasValueTypeConstraint => WrappedSymbol.HasValueTypeConstraint;

            public bool HasUnmanagedTypeConstraint => WrappedSymbol.HasUnmanagedTypeConstraint;

            public bool HasNotNullConstraint => WrappedSymbol.HasNotNullConstraint;

            public bool HasConstructorConstraint => WrappedSymbol.HasConstructorConstraint;

            public ImmutableArray<ITypeSymbol> ConstraintTypes => WrappedSymbol.ConstraintTypes;

            public ImmutableArray<NullableAnnotation> ConstraintNullableAnnotations => WrappedSymbol.ConstraintNullableAnnotations;

            public ITypeParameterSymbol? ReducedFrom => WrappedSymbol.ReducedFrom;


            #endregion
        }
    }
}
