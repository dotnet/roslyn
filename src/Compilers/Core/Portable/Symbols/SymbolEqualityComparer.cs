// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Allows for the comparison of two <see cref="ISymbol"/> instance based on certain criteria TODO
    /// </summary>
    public sealed class SymbolEqualityComparer : IEqualityComparer<ISymbol>
    {
        /// <summary>
        /// Compares two <see cref="ISymbol"/> instances based on the default comparison rules, equivalent to calling <see cref="IEquatable{ISymbol}.Equals(ISymbol)"/>
        /// </summary>
        public static readonly SymbolEqualityComparer Default = new SymbolEqualityComparer(TypeCompareKind.AllNullableIgnoreOptions);

        /// <summary>
        /// Compares  two <see cref="ISymbol"/> instances, considering their nullability
        /// </summary>
        public static readonly SymbolEqualityComparer IncludeNullability = new SymbolEqualityComparer(TypeCompareKind.ConsiderEverything2); //TODO: should this be explicitly *not* compare everything

        // Internal only comparisons:
        internal static readonly SymbolEqualityComparer ConsiderEverything = new SymbolEqualityComparer(TypeCompareKind.ConsiderEverything);

        internal static readonly SymbolEqualityComparer IgnoringTupleNamesAndNullability = new SymbolEqualityComparer(TypeCompareKind.IgnoreTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        /// <summary>
        /// A comparer that treats dynamic and object as "the same" types, and also ignores tuple element names differences.
        /// </summary>
        internal static readonly SymbolEqualityComparer IgnoringDynamicTupleNamesAndNullability = new SymbolEqualityComparer(TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        internal static readonly SymbolEqualityComparer IgnoringNullable = new SymbolEqualityComparer(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        internal static readonly SymbolEqualityComparer ObliviousNullableModifierMatchesAny = new SymbolEqualityComparer(TypeCompareKind.ObliviousNullableModifierMatchesAny);

        internal static readonly SymbolEqualityComparer AllIgnoreOptionsPlusNullableWithUnknownMatchesAny = new SymbolEqualityComparer(TypeCompareKind.AllIgnoreOptions & ~(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

        internal static readonly SymbolEqualityComparer CLRSignature = new SymbolEqualityComparer(TypeCompareKind.CLRSignatureCompareOptions);

        private readonly TypeCompareKind _compareKind;

        private SymbolEqualityComparer(TypeCompareKind compareKind)
        {
            _compareKind = compareKind;
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
            else if (x is ITypeComparable tx)
            {
                return tx.Equals(y, _compareKind);
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
