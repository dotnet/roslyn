// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// For completion and quickinfo in linked files, we compare symbols from different documents
    /// to determine if they are basically the same (which allows us to suppress the platform
    /// dependence warning icon). A <see cref="SymbolEquivalenceComparer"/> handles this comparison
    /// correctly for most symbols, but it compares locals, labels, and range variables by 
    /// comparing their <see cref="Location"/>s. This fails for linked files because 
    /// they have different trees. This class performs the special handling for these kinds of
    /// symbols and passes through all other requests to a <see cref="SymbolEquivalenceComparer"/>.
    /// </summary>
    internal sealed class LinkedFilesSymbolEquivalenceComparer : IEqualityComparer<ISymbol>
    {
        public static readonly LinkedFilesSymbolEquivalenceComparer IgnoreAssembliesInstance = new LinkedFilesSymbolEquivalenceComparer(SymbolEquivalenceComparer.IgnoreAssembliesInstance);
        public static readonly LinkedFilesSymbolEquivalenceComparer Instance = new LinkedFilesSymbolEquivalenceComparer(SymbolEquivalenceComparer.Instance);

        private readonly SymbolEquivalenceComparer _symbolEquivalenceComparer;

        public LinkedFilesSymbolEquivalenceComparer(SymbolEquivalenceComparer symbolEquivalenceComparer)
        {
            _symbolEquivalenceComparer = symbolEquivalenceComparer;
        }

        bool IEqualityComparer<ISymbol>.Equals(ISymbol x, ISymbol y)
        {
            return x.Kind == y.Kind && (x.IsKind(SymbolKind.Local) || x.IsKind(SymbolKind.Label) || x.IsKind(SymbolKind.RangeVariable))
                 ? x.Name == y.Name
                 : _symbolEquivalenceComparer.Equals(x, y);
        }

        int IEqualityComparer<ISymbol>.GetHashCode(ISymbol symbol)
        {
            return symbol.IsKind(SymbolKind.Local) || symbol.IsKind(SymbolKind.Label) || symbol.IsKind(SymbolKind.RangeVariable)
                ? symbol.Name.GetHashCode()
                : _symbolEquivalenceComparer.GetHashCode(symbol);
        }
    }
}
