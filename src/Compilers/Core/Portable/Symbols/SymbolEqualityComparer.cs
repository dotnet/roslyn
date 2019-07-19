// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Allows for the comparison of two <see cref="ISymbol"/> instances
    /// </summary>
    public sealed class SymbolEqualityComparer : IEqualityComparer<ISymbol>
    {
        /// <summary>
        /// Compares two <see cref="ISymbol"/> instances based on the default comparison rules, equivalent to calling <see cref="IEquatable{ISymbol}.Equals(ISymbol)"/>
        /// </summary>
        public readonly static SymbolEqualityComparer Default = new SymbolEqualityComparer(TypeCompareKind.AllNullableIgnoreOptions);

        /// <summary>
        /// Compares  two <see cref="ISymbol"/> instances, considering their nullability
        /// </summary>
        public readonly static SymbolEqualityComparer IncludeNullability = new SymbolEqualityComparer(TypeCompareKind.ConsiderEverything2); //TODO: should this be explicitly *not* compare everything

        // Internal only comparisons:
        internal readonly static SymbolEqualityComparer ConsiderEverything = new SymbolEqualityComparer(TypeCompareKind.ConsiderEverything);

        internal TypeCompareKind CompareKind { get; }

        private SymbolEqualityComparer(TypeCompareKind compareKind)
        {
            CompareKind = compareKind;
        }

        /// <summary>
        /// Determines if two <see cref="ISymbol" /> instances are equal according to the rules of this comparer
        /// </summary>
        /// <param name="x">The first symbol to compare</param>
        /// <param name="y">The second symbol to compare</param>
        /// <returns>True if the symbols are equivalent</returns>
        public bool Equals(ISymbol x, ISymbol y)
        {
            if (x is null)
            {
                return y is null;
            }
            else if (ReferenceEquals(x, y))
            {
                return true;
            }
            else if (x is ISymbolInternal sx)
            {
                return sx.Equals(y, CompareKind);
            }
            else
            {
                return x.Equals((object)y);
            }
        }

        public int GetHashCode(ISymbol obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
}
