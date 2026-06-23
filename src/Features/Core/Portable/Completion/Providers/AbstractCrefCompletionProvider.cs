// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractCrefCompletionProvider : LSPCompletionProvider
{
    internal override async Task<CompletionDescription> GetDescriptionWorkerAsync(
        Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
    {
        var position = SymbolCompletionItem.GetContextPosition(item);
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
