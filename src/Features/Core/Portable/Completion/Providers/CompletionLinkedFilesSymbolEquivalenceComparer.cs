// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractSymbolCompletionProvider<TSyntaxContext>
    {
        private sealed class CompletionLinkedFilesSymbolEquivalenceComparer : IEqualityComparer<(ISymbol symbol, bool preselect)>
        {
            public static readonly CompletionLinkedFilesSymbolEquivalenceComparer Instance = new();

            public bool Equals((ISymbol symbol, bool preselect) x, (ISymbol symbol, bool preselect) y)
                => LinkedFilesSymbolEquivalenceComparer.Instance.Equals(x.symbol, y.symbol);

            public int GetHashCode((ISymbol symbol, bool preselect) obj)
                => LinkedFilesSymbolEquivalenceComparer.Instance.GetHashCode(obj.symbol);
        }
    }
}
