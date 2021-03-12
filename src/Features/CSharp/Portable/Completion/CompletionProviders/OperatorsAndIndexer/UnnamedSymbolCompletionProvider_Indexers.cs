// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class UnnamedSymbolCompletionProvider
    {
        // private readonly int IndexerSortingGroupIndex = 1;
        private readonly ImmutableDictionary<string, string> IndexerProperties =
            ImmutableDictionary<string, string>.Empty.Add(KindName, IndexerKindName);

        private void AddIndexers(CompletionContext context, ImmutableArray<ISymbol> indexers)
        {
            if (indexers.Length == 0)
                return;

            var item = SymbolCompletionItem.CreateWithSymbolId(
                displayText: "this",
                displayTextSuffix: "[]",
                filterText: "this",
                sortText: "this",
                symbols: indexers,
                rules: CompletionItemRules.Default,
                contextPosition: context.Position,
                properties: IndexerProperties);
            context.AddItem(item);
        }

        private Task<CompletionChange> GetIndexerChangeAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return ReplaceDotAndTokenAfterWithTextAsync(document, item, text: "[]", removeConditionalAccess: false, positionOffset: -1, cancellationToken);
        }

        private Task<CompletionDescription> GetIndexerDescriptionAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);
        }
    }
}
