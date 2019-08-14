using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A comparer that compares symbols but ignores nullability, both top-level and nested.
    /// </summary>
    internal sealed class AllNullabilityIgnoringSymbolComparer : IEqualityComparer<ISymbol>
    {
        public static readonly IEqualityComparer<ISymbol> Instance = new AllNullabilityIgnoringSymbolComparer();

        private AllNullabilityIgnoringSymbolComparer()
        {
        }

        public bool Equals(ISymbol x, ISymbol y)
        {
            if (x is ITypeSymbol xTypeSymbol && y is ITypeSymbol yTypeSymbol)
            {
                return xTypeSymbol.WithoutNullability().Equals(yTypeSymbol.WithoutNullability(), SymbolEqualityComparer.Default);
            }

            return object.Equals(x, y);
        }

        public int GetHashCode(ISymbol symbol)
        {
            if (symbol is ITypeSymbol typeSymbol)
            {
                return typeSymbol.WithoutNullability().GetHashCode();
            }
            else
            {
                return symbol.GetHashCode();
            }
        }
    }
}
