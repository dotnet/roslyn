// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class ArrayTypeSymbol : TypeSymbol, IArrayTypeSymbol
    {
        private readonly Symbols.ArrayTypeSymbol _underlying;
        private ITypeSymbol _lazyElementType;

        public ArrayTypeSymbol(Symbols.ArrayTypeSymbol underlying, CodeAnalysis.NullableAnnotation nullableAnnotation)
            : base(nullableAnnotation)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        protected override ITypeSymbol WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != _underlying.DefaultNullableAnnotation);
            Debug.Assert(nullableAnnotation != this.NullableAnnotation);
            return new ArrayTypeSymbol(_underlying, nullableAnnotation);
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
        internal override Symbols.TypeSymbol UnderlyingTypeSymbol => _underlying;
        internal override Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol => _underlying;

        int IArrayTypeSymbol.Rank => _underlying.Rank;

        bool IArrayTypeSymbol.IsSZArray => _underlying.IsSZArray;

        ImmutableArray<int> IArrayTypeSymbol.LowerBounds => _underlying.LowerBounds;

        ImmutableArray<int> IArrayTypeSymbol.Sizes => _underlying.Sizes;

        ITypeSymbol IArrayTypeSymbol.ElementType
        {
            get
            {
                if (_lazyElementType is null)
                {
                    Interlocked.CompareExchange(ref _lazyElementType, _underlying.ElementTypeWithAnnotations.GetITypeSymbol(), null);
                }

                return _lazyElementType;
            }
        }

        CodeAnalysis.NullableAnnotation IArrayTypeSymbol.ElementNullableAnnotation
        {
            get
            {
                return _underlying.ElementTypeWithAnnotations.ToPublicAnnotation();
            }
        }

        ImmutableArray<CustomModifier> IArrayTypeSymbol.CustomModifiers => _underlying.ElementTypeWithAnnotations.CustomModifiers;

        bool IArrayTypeSymbol.Equals(IArrayTypeSymbol other)
        {
            return this.Equals(other as ArrayTypeSymbol, CodeAnalysis.SymbolEqualityComparer.Default);
        }

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitArrayType(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitArrayType(this);
        }

        #endregion
    }
}
