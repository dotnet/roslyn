// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Allows for the comparison of two <see cref="ISymbol"/> instances
    /// </summary>
    public sealed class SymbolEqualityComparer : IEqualityComparer<ISymbol?>
    {
        /// <summary>
        /// Compares two <see cref="ISymbol"/> instances based on the default comparison rules, equivalent to calling <see cref="IEquatable{ISymbol}.Equals(ISymbol)"/>.
        /// </summary>
        /// <remarks>
        /// Comparing <c>string</c> and <c>string?</c> will return equal. Use <see cref="IncludeNullability"/> if you don't want them to be considered equal.
        /// </remarks>
        public static readonly SymbolEqualityComparer Default = new SymbolEqualityComparer(TypeCompareKind.AllNullableIgnoreOptions);

        /// <summary>
        /// Compares  two <see cref="ISymbol"/> instances, considering that a reference type and the same nullable reference type are not equal.
        /// </summary>
        /// <remarks>
        /// Comparing <c>string</c> and <c>string?</c> will not return equal. Use <see cref="Default"/> if you want them to be considered equal.
        /// </remarks>
        public static readonly SymbolEqualityComparer IncludeNullability = new SymbolEqualityComparer(TypeCompareKind.ConsiderEverything2); //TODO: should this be explicitly *not* compare everything

        // Internal only comparisons:
        internal static readonly SymbolEqualityComparer ConsiderEverything = new SymbolEqualityComparer(TypeCompareKind.ConsiderEverything);
        internal static readonly SymbolEqualityComparer IgnoreAll = new SymbolEqualityComparer(TypeCompareKind.AllIgnoreOptions);
        internal static readonly SymbolEqualityComparer CLRSignature = new SymbolEqualityComparer(TypeCompareKind.CLRSignatureCompareOptions);

        internal TypeCompareKind CompareKind { get; }

        internal SymbolEqualityComparer(TypeCompareKind compareKind)
        {
            CompareKind = compareKind;
        }

        /// <summary>
        /// Determines if two <see cref="ISymbol" /> instances are equal according to the rules of this comparer
        /// </summary>
        /// <param name="x">The first symbol to compare</param>
        /// <param name="y">The second symbol to compare</param>
        /// <returns>True if the symbols are equivalent</returns>
        public bool Equals(ISymbol? x, ISymbol? y)
        {
            if (x is null)
            {
                return y is null;
            }

            return x.Equals(y, this);
        }

        public int GetHashCode(ISymbol? obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
}
