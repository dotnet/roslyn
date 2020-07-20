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
        public Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return SymbolFinder.FindLinkedSymbolsAsync(symbol, solution, cancellationToken);
        }

        public Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ISymbol symbol, Project project, IImmutableSet<Document> documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<Document>();
        }

        public Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
            => SpecializedTasks.EmptyImmutableArray<Project>();

        public Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ISymbol symbol, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<FinderLocation>();
        }
    }
}
