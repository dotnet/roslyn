// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class LinkedFileReferenceFinder : IReferenceFinder
    {
        public async Task<ImmutableArray<(ISymbol symbol, FindReferencesCascadeDirection cascadeDirection)>> DetermineCascadedSymbolsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project>? projects,
            FindReferencesSearchOptions options, FindReferencesCascadeDirection cascadeDirection,
            CancellationToken cancellationToken)
        {
            if (!options.Cascade)
                return ImmutableArray<(ISymbol symbol, FindReferencesCascadeDirection cascadeDirection)>.Empty;

            var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            return linkedSymbols.SelectAsArray(s => (s, cascadeDirection));

        }

        public Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ISymbol symbol, Project project, IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<Document>();
        }

        public Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(ISymbol symbol, Solution solution, IImmutableSet<Project>? projects = null, CancellationToken cancellationToken = default)
            => SpecializedTasks.EmptyImmutableArray<Project>();

        public ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ISymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return new ValueTask<ImmutableArray<FinderLocation>>(ImmutableArray<FinderLocation>.Empty);
        }
    }
}
