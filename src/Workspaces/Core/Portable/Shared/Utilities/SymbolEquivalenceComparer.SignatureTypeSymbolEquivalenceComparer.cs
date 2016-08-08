// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SymbolEquivalenceComparer
    {
        internal class SignatureTypeSymbolEquivalenceComparer : IEqualityComparer<ITypeSymbol>
        {
            private readonly SymbolEquivalenceComparer _symbolEquivalenceComparer;

            public SignatureTypeSymbolEquivalenceComparer(SymbolEquivalenceComparer symbolEquivalenceComparer)
            {
                _symbolEquivalenceComparer = symbolEquivalenceComparer;
            }

            public bool Equals(ITypeSymbol x, ITypeSymbol y)
            {
                return this.Equals(x, y, null);
            }

            public bool Equals(ITypeSymbol x, ITypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                return _symbolEquivalenceComparer.GetEquivalenceVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true).AreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);
            }

            public int GetHashCode(ITypeSymbol x)
            {
                return _symbolEquivalenceComparer.GetGetHashCodeVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true).GetHashCode(x, currentHash: 0);
            }
        }
    }
}
