// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal partial class SymbolEquivalenceComparer
{
    internal class SignatureTypeSymbolEquivalenceComparer(SymbolEquivalenceComparer symbolEquivalenceComparer) : IEqualityComparer<ITypeSymbol?>
    {
        public bool Equals(ITypeSymbol? x, ITypeSymbol? y)
            => this.Equals(x, y, null);

        public bool Equals(ITypeSymbol? x, ITypeSymbol? y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            var visitor = symbolEquivalenceComparer.GetEquivalenceVisitor(
                compareMethodTypeParametersByIndex: true,
                symbolEquivalenceComparer._objectAndDynamicCompareEqually,
                symbolEquivalenceComparer._arrayAndReadOnlySpanCompareEqually);
            return visitor.AreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);
        }

        public int GetHashCode(ITypeSymbol? x)
            => symbolEquivalenceComparer.GetGetHashCodeVisitor(compareMethodTypeParametersByIndex: true, symbolEquivalenceComparer._objectAndDynamicCompareEqually).GetHashCode(x, currentHash: 0);
    }
}
