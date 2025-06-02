// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal sealed partial class SymbolEquivalenceComparer
{
    internal sealed class SignatureTypeSymbolEquivalenceComparer(SymbolEquivalenceComparer symbolEquivalenceComparer) : IEqualityComparer<ITypeSymbol?>
    {
        public bool Equals(ITypeSymbol? x, ITypeSymbol? y)
            => this.Equals(x, y, null);

        public bool Equals(ITypeSymbol? x, ITypeSymbol? y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
            => symbolEquivalenceComparer.GetEquivalenceVisitor(compareMethodTypeParametersByIndex: true).AreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);

        public int GetHashCode(ITypeSymbol? x)
            => symbolEquivalenceComparer.GetGetHashCodeVisitor(compareMethodTypeParametersByIndex: true).GetHashCode(x, currentHash: 0);
    }
}
