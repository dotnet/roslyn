﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractCrefCompletionProvider : LSPCompletionProvider
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
            var bestSymbols = symbols.WhereAsArray(s => s.Kind == kind && s.Name == name);
            return await SymbolCompletionItem.GetDescriptionAsync(item, bestSymbols, document, semanticModel, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<(SyntaxToken, SemanticModel, ImmutableArray<ISymbol>)> GetSymbolsAsync(
            Document document, int position, OptionSet options, CancellationToken cancellationToken);
    }
}
