using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Internal interface that allows a symbol to declare that it supports comparisons involving type symbols
    /// </summary>
    /// <remarks>
    /// Becuase TypeSymbol equality can differ based on e.g. nullability, any symbols that contain TypeSymbols can also differ in the same way
    /// This interface allows the symbol to accept a comparison kind that should be used when comparing its contained types
    /// </remarks>
    internal interface ITypeComparable
    {
        internal bool Equals(ISymbol other, TypeCompareKind compareKind);
    }
}
