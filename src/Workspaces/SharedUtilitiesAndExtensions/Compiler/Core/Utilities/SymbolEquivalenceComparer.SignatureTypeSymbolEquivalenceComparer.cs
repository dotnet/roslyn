// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SymbolEquivalenceComparer
    {
        internal class SignatureTypeSymbolEquivalenceComparer : IEqualityComparer<ITypeSymbol>
        {
            private readonly SymbolEquivalenceComparer _symbolEquivalenceComparer;

            public SignatureTypeSymbolEquivalenceComparer(SymbolEquivalenceComparer symbolEquivalenceComparer)
                => _symbolEquivalenceComparer = symbolEquivalenceComparer;

            public bool Equals(ITypeSymbol x, ITypeSymbol y)
                => this.Equals(x, y, null);

            public bool Equals(ITypeSymbol x, ITypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
                => _symbolEquivalenceComparer.GetEquivalenceVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true).AreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);

            public int GetHashCode(ITypeSymbol x)
                => _symbolEquivalenceComparer.GetGetHashCodeVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true).GetHashCode(x, currentHash: 0);
        }
    }
}
