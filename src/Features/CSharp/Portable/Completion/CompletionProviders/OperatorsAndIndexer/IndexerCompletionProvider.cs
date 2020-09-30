// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(IndexerCompletionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(ConversionCompletionProvider))]
    internal class IndexerCompletionProvider : OperatorIndexerCompletionProviderBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IndexerCompletionProvider()
        {
        }

        protected override int SortingGroupIndex => 1;

        protected override ImmutableArray<CompletionItem> GetCompletionItemsForTypeSymbol(ITypeSymbol container, bool isAccessedByConditionalAccess, SemanticModel semanticModel, int position)
        {
            var allMembers = container.GetMembers();
            var indexers = allMembers.OfType<IPropertySymbol>().Where(p => p.IsIndexer).ToImmutableList();
            if (indexers.Any())
            {
                var indexerCompletion = SymbolCompletionItem.CreateWithSymbolId(
                    displayText: "this",
                    displayTextSuffix: "[]",
                    filterText: "this",
                    sortText: SortText(),
                    symbols: indexers,
                    rules: CompletionItemRules.Default,
                    contextPosition: position);
                return ImmutableArray.Create(indexerCompletion);
            }

            return ImmutableArray<CompletionItem>.Empty;
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            return
                await ReplaceDotAndTokenAfterWithTextAsync(document, item, text: "[]", positionOffset: -1, cancellationToken).ConfigureAwait(false) ??
                await base.GetChangeAsync(document, item, completionListSpan, commitKey, disallowAddingImports, cancellationToken).ConfigureAwait(false);
        }
    }
}
