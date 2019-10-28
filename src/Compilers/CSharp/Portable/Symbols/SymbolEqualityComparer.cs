// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SymbolEqualityComparer : EqualityComparer<Symbol>
    {
        internal static readonly EqualityComparer<Symbol> ConsiderEverything = new SymbolEqualityComparer(TypeCompareKind.ConsiderEverything);

        internal static readonly EqualityComparer<Symbol> IgnoringTupleNamesAndNullability = new SymbolEqualityComparer(TypeCompareKind.IgnoreTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        internal static EqualityComparer<Symbol> IncludeNullability => ConsiderEverything;

        /// <summary>
        /// A comparer that treats dynamic and object as "the same" types, and also ignores tuple element names differences.
        /// </summary>
        internal static readonly EqualityComparer<Symbol> IgnoringDynamicTupleNamesAndNullability = new SymbolEqualityComparer(TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        internal static readonly EqualityComparer<Symbol> IgnoringNullable = new SymbolEqualityComparer(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        internal static readonly EqualityComparer<Symbol> ObliviousNullableModifierMatchesAny = new SymbolEqualityComparer(TypeCompareKind.ObliviousNullableModifierMatchesAny);

        internal static readonly EqualityComparer<Symbol> AllIgnoreOptionsPlusNullableWithUnknownMatchesAny =
                                                                  new SymbolEqualityComparer(TypeCompareKind.AllIgnoreOptions & ~(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

        internal static readonly EqualityComparer<Symbol> CLRSignature = new SymbolEqualityComparer(TypeCompareKind.CLRSignatureCompareOptions);

        private readonly TypeCompareKind _comparison;

        private SymbolEqualityComparer(TypeCompareKind comparison)
        {
            _comparison = comparison;
        }

        public override int GetHashCode(Symbol obj)
        {
            return obj is null ? 0 : obj.GetHashCode();
        }

        public override bool Equals(Symbol x, Symbol y)
        {
            return x is null ? y is null : x.Equals(y, _comparison);
        }
    }
}
