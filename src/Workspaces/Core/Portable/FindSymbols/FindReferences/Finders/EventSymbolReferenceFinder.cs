// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal class EventSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IEventSymbol>
{
    protected override bool CanFind(IEventSymbol symbol)
        => true;

    protected sealed override ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        IEventSymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var backingFields = symbol.ContainingType.GetMembers()
                                                 .OfType<IFieldSymbol>()
                                                 .Where(f => symbol.Equals(f.AssociatedSymbol))
                                                 .ToImmutableArray<ISymbol>();

        var associatedNamedTypes = symbol.ContainingType.GetTypeMembers()
                                                        .WhereAsArray(n => symbol.Equals(n.AssociatedSymbol))
                                                        .CastArray<ISymbol>();

        return new(backingFields.Concat(associatedNamedTypes));
    }

    protected sealed override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
        IEventSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);
        var documentsWithGlobalAttributes = await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, cancellationToken).ConfigureAwait(false);
        return documentsWithName.Concat(documentsWithGlobalAttributes);
    }

    protected sealed override ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
        IEventSymbol symbol,
        FindReferencesDocumentState state,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        return FindReferencesInDocumentUsingSymbolNameAsync(symbol, state, cancellationToken);
    }
}
