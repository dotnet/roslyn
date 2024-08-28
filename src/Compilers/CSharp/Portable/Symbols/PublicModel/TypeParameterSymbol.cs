// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class TypeParameterSymbol : TypeSymbol, ITypeParameterSymbol
    {
        private readonly Symbols.TypeParameterSymbol _underlying;

        public TypeParameterSymbol(Symbols.TypeParameterSymbol underlying, CodeAnalysis.NullableAnnotation nullableAnnotation)
            : base(nullableAnnotation)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        protected override ITypeSymbol WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != _underlying.DefaultNullableAnnotation);
            Debug.Assert(nullableAnnotation != this.NullableAnnotation);
            return new TypeParameterSymbol(_underlying, nullableAnnotation);
        }

        internal override Symbols.TypeSymbol UnderlyingTypeSymbol => _underlying;
        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
        internal override Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol => _underlying;
        internal Symbols.TypeParameterSymbol UnderlyingTypeParameterSymbol => _underlying;

        CodeAnalysis.NullableAnnotation ITypeParameterSymbol.ReferenceTypeConstraintNullableAnnotation =>
            _underlying.ReferenceTypeConstraintIsNullable switch
            {
                false when !_underlying.HasReferenceTypeConstraint => CodeAnalysis.NullableAnnotation.None,
                false => CodeAnalysis.NullableAnnotation.NotAnnotated,
                true => CodeAnalysis.NullableAnnotation.Annotated,
                null => CodeAnalysis.NullableAnnotation.None,
            };

        TypeParameterKind ITypeParameterSymbol.TypeParameterKind
        {
            get
            {
                return _underlying.TypeParameterKind;
            }
        }

        IMethodSymbol ITypeParameterSymbol.DeclaringMethod
        {
            get { return _underlying.DeclaringMethod.GetPublicSymbol(); }
        }

        INamedTypeSymbol ITypeParameterSymbol.DeclaringType
        {
            get { return _underlying.DeclaringType.GetPublicSymbol(); }
        }

        ImmutableArray<ITypeSymbol> ITypeParameterSymbol.ConstraintTypes
        {
            get
            {
                return _underlying.ConstraintTypesNoUseSiteDiagnostics.GetPublicSymbols();
            }
        }

        ImmutableArray<CodeAnalysis.NullableAnnotation> ITypeParameterSymbol.ConstraintNullableAnnotations =>
            _underlying.ConstraintTypesNoUseSiteDiagnostics.ToPublicAnnotations();

        ITypeParameterSymbol ITypeParameterSymbol.OriginalDefinition
        {
            get { return _underlying.OriginalDefinition.GetPublicSymbol(); }
        }

        ITypeParameterSymbol ITypeParameterSymbol.ReducedFrom
        {
            get { return _underlying.ReducedFrom.GetPublicSymbol(); }
        }

        int ITypeParameterSymbol.Ordinal => _underlying.Ordinal;

        VarianceKind ITypeParameterSymbol.Variance => _underlying.Variance;

        bool ITypeParameterSymbol.HasReferenceTypeConstraint => _underlying.HasReferenceTypeConstraint;

        bool ITypeParameterSymbol.HasValueTypeConstraint => _underlying.HasValueTypeConstraint;

        bool ITypeParameterSymbol.AllowsRefLikeType => _underlying.AllowsRefLikeType;

        bool ITypeParameterSymbol.HasUnmanagedTypeConstraint => _underlying.HasUnmanagedTypeConstraint;

        bool ITypeParameterSymbol.HasNotNullConstraint => _underlying.HasNotNullConstraint;

        bool ITypeParameterSymbol.HasConstructorConstraint => _underlying.HasConstructorConstraint;

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitTypeParameter(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitTypeParameter(this);
        }

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTypeParameter(this, argument);
        }

        #endregion
    }
}
