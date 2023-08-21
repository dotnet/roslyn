// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class UnnamedSymbolCompletionProvider
    {
        private readonly ImmutableDictionary<string, string> IndexerProperties =
            ImmutableDictionary<string, string>.Empty.Add(KindName, IndexerKindName);

        private void AddIndexers(CompletionContext context, ImmutableArray<ISymbol> indexers)
        {
            if (indexers.Length == 0)
                return;

            context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                displayText: "this",
                displayTextSuffix: "[]",
                filterText: "this",
                sortText: "this",
                symbols: indexers,
                rules: CompletionItemRules.Default,
                contextPosition: context.Position,
                properties: IndexerProperties,
                isComplexTextEdit: true));
        }

        // Remove the dot, but leave the ? if one is there.  Place the caret one space back so it is between the braces.
        private static Task<CompletionChange> GetIndexerChangeAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => ReplaceTextAfterOperatorAsync(document, item, text: "[]", keepQuestion: true, positionOffset: -1, cancellationToken);

        private static Task<CompletionDescription> GetIndexerDescriptionAsync(Document document, CompletionItem item, SymbolDescriptionOptions options, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, options, cancellationToken);
    }
}
