// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract partial class AbstractSymbolCompletionProvider<TSyntaxContext>
{
    // The equality of this type is only used when we try to figure out missing symbols
    // among linked files, therefore delegate to CompletionLinkedFilesSymbolEquivalenceComparer
    protected readonly record struct SymbolAndSelectionInfo(ISymbol Symbol, bool Preselect)
    {
        public bool Equals(SymbolAndSelectionInfo other)
            => LinkedFilesSymbolEquivalenceComparer.Instance.Equals(Symbol, other.Symbol);

        public override int GetHashCode()
            => LinkedFilesSymbolEquivalenceComparer.Instance.GetHashCode(Symbol);
    }
}
