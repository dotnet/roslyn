using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        private sealed class DynamicTypeSymbolWithNullableAnnotation : TypeSymbolWithNullableAnnotation, IDynamicTypeSymbol
        {
            public DynamicTypeSymbolWithNullableAnnotation(ITypeSymbol wrappedSymbol, NullableAnnotation nullability) : base(wrappedSymbol, nullability)
            {
            }

            private new IDynamicTypeSymbol WrappedSymbol => (IDynamicTypeSymbol)base.WrappedSymbol;

            public override void Accept(SymbolVisitor visitor)
            {
                visitor.VisitDynamicType(this);
            }

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                return visitor.VisitDynamicType(this);
            }
        }
    }
}
