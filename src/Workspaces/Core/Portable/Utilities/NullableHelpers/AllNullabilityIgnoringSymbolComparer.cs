using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A comparer that compares symbols but ignores nullability, both top-level and nested.
    /// </summary>
    /// <remarks>This is not implemented correctly, which is being tracked by https://github.com/dotnet/roslyn/issues/36044</remarks>
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
                // TODO: also ignore nested nullability. Right now there is no compiler API to do this, but it's being tracked in https://github.com/dotnet/roslyn/issues/35933.
                // The fixing of this code will be tracked by https://github.com/dotnet/roslyn/issues/36044 and tests can then be unskipped there.
                return xTypeSymbol.WithoutNullability().Equals(yTypeSymbol.WithoutNullability());
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
