using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// TODO
    /// </summary>
    public sealed class TypeSymbolEqualityComparer : IEqualityComparer<ISymbol>
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
        public bool Equals(ISymbol x, ISymbol y)
        {
            if (x is null)
            {
                return y is null;
            }
            if (x is ITypeSymbolComparable tx && y is ITypeSymbol ty)
            {
                return tx.Equals(ty, _compareKind);
            }
            else
            {
                return x.Equals(y);
            }
        }

        public int GetHashCode(ISymbol obj)
        {
            return obj.GetHashCode();
        }
    }

    internal sealed class WrappedTypeComparer<T> : IEqualityComparer<T>
    {
        TypeSymbolEqualityComparer _typeComparer;

        Func<T, T, TypeSymbolEqualityComparer, bool> _comparerFunc;

        internal WrappedTypeComparer(TypeSymbolEqualityComparer typeComparer, Func<T, T, TypeSymbolEqualityComparer, bool> comparisonFuncOpt = null)
        {
            _typeComparer = typeComparer;
            _comparerFunc = comparisonFuncOpt;
        }

        public bool Equals(T x, T y)
        {
            // TODO: could we use the default comparer here to simplify code?
            if (x is null)
            {
                return y is null;
            }
            else if (_comparerFunc is object)
            {
                return _comparerFunc(x, y, _typeComparer);
            }
            else if (x is ITypeSymbol tx && y is ITypeSymbol ty)
            {
                return _typeComparer.Equals(tx, ty);
            }
            else
            {
                return x.Equals(y);
            }
        }

        public int GetHashCode(T obj)
        {
            return (obj is null) ? 0 : obj.GetHashCode(); //TODO: we should route this through types too
        }
    }
}
