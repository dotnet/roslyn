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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        protected override ImmutableArray<CompletionItem> GetCompletionItemsForTypeSymbol(SemanticModel semanticModel,
            ITypeSymbol container,
            ExpressionSyntax expression,
            int position,
            bool isAccessedByConditionalAccess,
            CancellationToken cancellationToken)
        {
            // We only want to suggest indexer accessible from the cursor position, so we need the containing type at the cursor position,
            // because the within parameter of GetAccessibleMembersInThisAndBaseTypes() must be an IAssemblySymbol or an INamedTypeSymbol.
            var containingTypeAtCursorPosition = semanticModel.GetEnclosingSymbol(position, cancellationToken)?.GetContainingTypeOrThis();
            // We may not be able to identify a containing type, in which case we are conservative and suggest only public indexers
            var indexers = containingTypeAtCursorPosition is null
                ? from t in container.GetBaseTypesAndThis()
                  from i in t.GetIndexers()
                  where i.HasPublicResultantVisibility()
                  select i
                : from p in container.GetAccessibleMembersInThisAndBaseTypes<IPropertySymbol>(containingTypeAtCursorPosition)
                  where p.IsIndexer
                  select p;
            var indexerList = indexers.ToImmutableArray();
            if (indexerList.Any())
            {
                var indexerCompletion = SymbolCompletionItem.CreateWithSymbolId(
                    displayText: "this",
                    displayTextSuffix: "[]",
                    filterText: "this",
                    sortText: SortText(),
                    symbols: indexerList,
                    rules: CompletionItemRules.Default,
                    contextPosition: position);
                return ImmutableArray.Create(indexerCompletion);
            }

            return ImmutableArray<CompletionItem>.Empty;
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            return
                await ReplaceDotAndTokenAfterWithTextAsync(document, item, text: "[]", removeConditionalAccess: false, positionOffset: -1, cancellationToken).ConfigureAwait(false) ??
                await base.GetChangeAsync(document, item, completionListSpan, commitKey, disallowAddingImports, cancellationToken).ConfigureAwait(false);
        }
    }
}
