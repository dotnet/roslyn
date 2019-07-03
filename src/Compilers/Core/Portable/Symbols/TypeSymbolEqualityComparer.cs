using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// TODO
    /// </summary>
    public sealed class TypeSymbolEqualityComparer : IEqualityComparer<ITypeSymbol>
    {
        /// <summary>
        /// Compares two types based on the default comparison rules, equivalent to calling <see cref="IEquatable{ISymbol}.Equals(ISymbol)"/>
        /// </summary>
        public static TypeSymbolEqualityComparer Default = new TypeSymbolEqualityComparer(TypeCompareKind.AllNullableIgnoreOptions);

        /// <summary>
        /// Compares two types considering everything available to the compiler
        /// </summary>
        public static TypeSymbolEqualityComparer CompareEverything = new TypeSymbolEqualityComparer(TypeCompareKind.ConsiderEverything2);

        private readonly TypeCompareKind _compareKind;

        private TypeSymbolEqualityComparer(TypeCompareKind compareKind)
        {
            _compareKind = compareKind;
        }

        /// <summary>
        /// Determines if two <see cref="ITypeSymbol" /> instances are equal according to the rules of this comparer
        /// </summary>
        /// <param name="x">The first type to compare</param>
        /// <param name="y">The second type to compare</param>
        /// <returns>True if the types are equivalent</returns>
        public bool Equals(ITypeSymbol x, ITypeSymbol y)
        {
            if (x is ITypeSymbolComparable tx && y is ITypeSymbol ty)
            {
                return tx.Equals(ty, _compareKind);
            }
            else
            {
                return x.Equals(y);
            }
        }

        public int GetHashCode(ITypeSymbol obj)
        {
            return obj.GetHashCode();
        }
    }
}
