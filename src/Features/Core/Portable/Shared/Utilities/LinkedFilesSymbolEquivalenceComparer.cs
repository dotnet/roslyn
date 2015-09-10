// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// For completion and quickinfo in linked files, we compare symbols from different documents
    /// to determine if they are similar enough for us to suppress the platform dependence
    /// warning icon. We consider symbols equivalent if they have the same name and kind.
    /// </summary>
    internal sealed class LinkedFilesSymbolEquivalenceComparer : IEqualityComparer<ISymbol>
    {
        public static readonly LinkedFilesSymbolEquivalenceComparer Instance = new LinkedFilesSymbolEquivalenceComparer();

        bool IEqualityComparer<ISymbol>.Equals(ISymbol x, ISymbol y)
        {
            return x.Name == y.Name;
        }

        int IEqualityComparer<ISymbol>.GetHashCode(ISymbol symbol)
        {
            return symbol.Name.GetHashCode();
        }
    }
}
