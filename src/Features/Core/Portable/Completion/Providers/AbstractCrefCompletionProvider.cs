// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    abstract class AbstractCrefCompletionProvider : CommonCompletionProvider
    {
        protected const string HideAdvancedMembers = nameof(HideAdvancedMembers);

        protected override async Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);

            // What EditorBrowsable settings were we previously passed in (if it mattered)?
            var hideAdvancedMembers = false;
            if (item.Properties.TryGetValue(HideAdvancedMembers, out var hideAdvancedMembersString))
            {
                bool.TryParse(hideAdvancedMembersString, out hideAdvancedMembers);
            }

            var options = document.Project.Solution.Workspace.Options
                .WithChangedOption(new OptionKey(CompletionOptions.HideAdvancedMembers, document.Project.Language), hideAdvancedMembers);

            var (token, semanticModel, symbols) = await GetSymbolsAsync(document, position, options, cancellationToken).ConfigureAwait(false);
            var name = SymbolCompletionItem.GetSymbolName(item);
            var kind = SymbolCompletionItem.GetKind(item);
            var bestSymbols = symbols.WhereAsArray(s => s is { Kind: kind, Name: name });
            return await SymbolCompletionItem.GetDescriptionAsync(item, bestSymbols, document, semanticModel, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<(SyntaxToken, SemanticModel, ImmutableArray<ISymbol>)> GetSymbolsAsync(
            Document document, int position, OptionSet options, CancellationToken cancellationToken);
    }
}
