using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                return visitor.VisitTypeParameter(this);
            }

            #region ITypeParameterSymbol Implementation Forwards

            public int Ordinal => WrappedSymbol.Ordinal;

            public VarianceKind Variance => WrappedSymbol.Variance;

            public TypeParameterKind TypeParameterKind => WrappedSymbol.TypeParameterKind;

            public IMethodSymbol DeclaringMethod => WrappedSymbol.DeclaringMethod;

            public INamedTypeSymbol DeclaringType => WrappedSymbol.DeclaringType;

            public bool HasReferenceTypeConstraint => WrappedSymbol.HasReferenceTypeConstraint;

            public NullableAnnotation ReferenceTypeConstraintNullableAnnotation => WrappedSymbol.ReferenceTypeConstraintNullableAnnotation;

            public bool HasValueTypeConstraint => WrappedSymbol.HasValueTypeConstraint;

            public bool HasUnmanagedTypeConstraint => WrappedSymbol.HasUnmanagedTypeConstraint;

            public bool HasNotNullConstraint => WrappedSymbol.HasNotNullConstraint;

            public bool HasConstructorConstraint => WrappedSymbol.HasConstructorConstraint;

            public ImmutableArray<ITypeSymbol> ConstraintTypes => WrappedSymbol.ConstraintTypes;

            public ImmutableArray<NullableAnnotation> ConstraintNullableAnnotations => WrappedSymbol.ConstraintNullableAnnotations;

            public ITypeParameterSymbol ReducedFrom => WrappedSymbol.ReducedFrom;


            #endregion
        }
    }
}
