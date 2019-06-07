using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        private sealed class ArrayTypeSymbolWithNullableAnnotation : TypeSymbolWithNullableAnnotation, IArrayTypeSymbol
        {
            public ArrayTypeSymbolWithNullableAnnotation(ITypeSymbol wrappedSymbol, NullableAnnotation nullability) : base(wrappedSymbol, nullability)
            {
            }

            private new IArrayTypeSymbol WrappedSymbol => (IArrayTypeSymbol)base.WrappedSymbol;

            public override void Accept(SymbolVisitor visitor)
            {
                visitor.VisitArrayType(this);
            }

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                return visitor.VisitArrayType(this);
            }

            #region IArrayTypeSymbol Implementation Forwards

            public int Rank => WrappedSymbol.Rank;

            public bool IsSZArray => WrappedSymbol.IsSZArray;

            public ImmutableArray<int> LowerBounds => WrappedSymbol.LowerBounds;

            public ImmutableArray<int> Sizes => WrappedSymbol.Sizes;

            public ITypeSymbol ElementType => WrappedSymbol.ElementType;

            public NullableAnnotation ElementNullableAnnotation => WrappedSymbol.ElementNullableAnnotation;

            public ImmutableArray<CustomModifier> CustomModifiers => WrappedSymbol.CustomModifiers;

            public bool Equals(IArrayTypeSymbol other)
            {
                return WrappedSymbol.Equals(other);
            }

            #endregion
        }
    }
}
