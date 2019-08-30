using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        private sealed class PointerTypeSymbolWithNullableAnnotation : TypeSymbolWithNullableAnnotation, IPointerTypeSymbol
        {
            public PointerTypeSymbolWithNullableAnnotation(ITypeSymbol wrappedSymbol, NullableAnnotation nullability) : base(wrappedSymbol, nullability)
            {
            }

            private new IPointerTypeSymbol WrappedSymbol => (IPointerTypeSymbol)base.WrappedSymbol;

            public override void Accept(SymbolVisitor visitor)
            {
                visitor.VisitPointerType(this);
            }

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                return visitor.VisitPointerType(this);
            }

            #region IPointerTypeSymbol Implementation Forwards

            public ITypeSymbol PointedAtType => WrappedSymbol.PointedAtType;

            public ImmutableArray<CustomModifier> CustomModifiers => WrappedSymbol.CustomModifiers;

            #endregion
        }
    }
}
