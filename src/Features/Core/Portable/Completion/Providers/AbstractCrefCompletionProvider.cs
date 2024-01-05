// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractCrefCompletionProvider : LSPCompletionProvider
    {
        protected const string HideAdvancedMembers = nameof(HideAdvancedMembers);

        internal override async Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);

            // What EditorBrowsable settings were we previously passed in (if it mattered)?
            if (item.TryGetProperty(HideAdvancedMembers, out var hideAdvancedMembersString) &&
                bool.TryParse(hideAdvancedMembersString, out var hideAdvancedMembers))
            {
                options = options with { HideAdvancedMembers = hideAdvancedMembers };
            }

            var (token, semanticModel, symbols) = await GetSymbolsAsync(document, position, options, cancellationToken).ConfigureAwait(false);
            if (symbols.Length == 0)
            {
                return CompletionDescription.Empty;
            }

            Contract.ThrowIfNull(semanticModel);

            var name = SymbolCompletionItem.GetSymbolName(item);
            var kind = SymbolCompletionItem.GetKind(item);
            var bestSymbols = symbols.WhereAsArray(s => s.Kind == kind && s.Name == name);
            return await SymbolCompletionItem.GetDescriptionAsync(item, bestSymbols, document, semanticModel, displayOptions, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<(SyntaxToken, SemanticModel?, ImmutableArray<ISymbol>)> GetSymbolsAsync(
            Document document, int position, CompletionOptions options, CancellationToken cancellationToken);
    }
}
