// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal sealed class LinkedFilesItemEquivalenceComparer : IEqualityComparer<(ISymbol symbol, CompletionItemRules)>
    {
        public static readonly LinkedFilesItemEquivalenceComparer Instance = new LinkedFilesItemEquivalenceComparer();

        bool IEqualityComparer<(ISymbol symbol, CompletionItemRules)>.Equals((ISymbol symbol, CompletionItemRules) x, (ISymbol symbol, CompletionItemRules) y)
        {
            return x.symbol.Name == y.symbol.Name;
        }

        int IEqualityComparer<(ISymbol symbol, CompletionItemRules)>.GetHashCode((ISymbol symbol, CompletionItemRules) item)
        {
            return item.symbol.Name.GetHashCode();
        }
    }
}
