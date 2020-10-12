﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SymbolEquivalenceComparer
    {
        internal class ParameterSymbolEqualityComparer : IEqualityComparer<IParameterSymbol>
        {
            private readonly SymbolEquivalenceComparer _symbolEqualityComparer;
            private readonly bool _distinguishRefFromOut;

            public ParameterSymbolEqualityComparer(
                SymbolEquivalenceComparer symbolEqualityComparer,
                bool distinguishRefFromOut)
            {
                _symbolEqualityComparer = symbolEqualityComparer;
                _distinguishRefFromOut = distinguishRefFromOut;
            }

            public bool Equals(
                IParameterSymbol x,
                IParameterSymbol y,
                Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies,
                bool compareParameterName,
                bool isCaseSensitive)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                var nameComparisonCheck = true;
                if (compareParameterName)
                {
                    nameComparisonCheck = isCaseSensitive ?
                        x.Name == y.Name
                        : string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                }

                // See the comment in the outer type.  If we're comparing two parameters for
                // equality, then we want to consider method type parameters by index only.

                return
                    AreRefKindsEquivalent(x.RefKind, y.RefKind, _distinguishRefFromOut) &&
                    nameComparisonCheck &&
                    _symbolEqualityComparer.GetEquivalenceVisitor().AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                    _symbolEqualityComparer.SignatureTypeEquivalenceComparer.Equals(x.Type, y.Type, equivalentTypesWithDifferingAssemblies);
            }

            public bool Equals(IParameterSymbol x, IParameterSymbol y)
                => this.Equals(x, y, null, false, false);

            public bool Equals(IParameterSymbol x, IParameterSymbol y, bool compareParameterName, bool isCaseSensitive)
                => this.Equals(x, y, null, compareParameterName, isCaseSensitive);

            public int GetHashCode(IParameterSymbol x)
            {
                if (x == null)
                {
                    return 0;
                }

                return
                    Hash.Combine(x.IsRefOrOut(),
                    _symbolEqualityComparer.SignatureTypeEquivalenceComparer.GetHashCode(x.Type));
            }
        }

        public static bool AreRefKindsEquivalent(RefKind rk1, RefKind rk2, bool distinguishRefFromOut)
        {
            return distinguishRefFromOut
                ? rk1 == rk2
                : (rk1 == RefKind.None) == (rk2 == RefKind.None);
        }
    }
}
