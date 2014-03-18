// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SymbolEquivalenceComparer
    {
        internal class ParameterSymbolEqualityComparer : IEqualityComparer<IParameterSymbol>
        {
            private readonly SymbolEquivalenceComparer symbolEqualityComparer;

            public ParameterSymbolEqualityComparer(
                SymbolEquivalenceComparer symbolEqualityComparer)
            {
                this.symbolEqualityComparer = symbolEqualityComparer;
            }

            public bool Equals(IParameterSymbol x, IParameterSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                // See the comment in the outer type.  If we're comparing two parameters for
                // equality, then we want to consider method type parameters by index only.
                //
                // NOTE(cyrusn): Do we actually want to test the name?  Shouldn't method signatures
                // compare the same regardless of the name used?
                return
                    x.RefKind == y.RefKind &&
                    symbolEqualityComparer.SignatureTypeEquivalenceComparer.Equals(x.Type, y.Type, equivalentTypesWithDifferingAssemblies);
            }

            public bool Equals(IParameterSymbol x, IParameterSymbol y)
            {
                return this.Equals(x, y, null);
            }

            public int GetHashCode(IParameterSymbol x)
            {
                if (x == null)
                {
                    return 0;
                }

                return
                    Hash.Combine(x.IsRefOrOut(),
                    symbolEqualityComparer.SignatureTypeEquivalenceComparer.GetHashCode(x.Type));
            }
        }
    }
}