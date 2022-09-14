// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractSymbolCompletionProvider<TSyntaxContext>
    {
        private sealed class CompletionLinkedFilesSymbolEquivalenceComparer : IEqualityComparer<SymbolAndSelectionInfo>
        {
            public static readonly CompletionLinkedFilesSymbolEquivalenceComparer Instance = new();

            public bool Equals(SymbolAndSelectionInfo x, SymbolAndSelectionInfo y)
                => LinkedFilesSymbolEquivalenceComparer.Instance.Equals(x.Symbol, y.Symbol);

            public int GetHashCode(SymbolAndSelectionInfo obj)
                => LinkedFilesSymbolEquivalenceComparer.Instance.GetHashCode(obj.Symbol);
        }

        protected readonly record struct SymbolAndSelectionInfo(ISymbol Symbol, bool Preselect) { }
    }
}
