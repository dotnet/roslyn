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
            int position,
            bool isAccessedByConditionalAccess,
            CancellationToken cancellationToken)
        {
            var containingType = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (containingType != null)
            {
                foreach (var property in container.GetAccessibleMembersInThisAndBaseTypes<IPropertySymbol>(containingType))
                {
                    if (property.IsIndexer)
                    {
                        var indexerCompletion = SymbolCompletionItem.CreateWithSymbolId(
                            displayText: "this",
                            displayTextSuffix: "[]",
                            filterText: "this",
                            sortText: "this",
                            symbols: indexerList,
                            rules: CompletionItemRules.Default,
                            contextPosition: position);
                        return ImmutableArray.Create(indexerCompletion);
                    }
                }
            }

            return ImmutableArray<CompletionItem>.Empty;
        }

        internal override async Task<CompletionChange> GetChangeAsync(
            Document document,
            CompletionItem item,
            TextSpan completionListSpan,
            char? commitKey,
            bool disallowAddingImports,
            CancellationToken cancellationToken)
        {
            return await ReplaceDotAndTokenAfterWithTextAsync(
                document, item, text: "[]", removeConditionalAccess: false, positionOffset: -1, cancellationToken).ConfigureAwait(false);
        }
    }
}
