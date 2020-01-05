// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    /// <summary>
    /// Extensibility interface to allow extending the IFindReferencesService service.  Implementations
    /// must be thread-safe as the methods on this interface may be called on multiple threads
    /// simultaneously.  Implementations should also respect the provided cancellation token and
    /// should try to cancel themselves quickly when requested.
    /// </summary>
    internal interface IReferenceFinder
    {
        /// <summary>
        /// Called by the find references search engine when a new symbol definition is found.
        /// Implementations can then choose to request more symbols be searched for.  For example, an
        /// implementation could choose for the find references search engine to cascade to
        /// constructors when searching for standard types.
        /// 
        /// Implementations of this method must be thread-safe.
        /// </summary>
        Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId symbolAndProject, Solution solution, IImmutableSet<Project> projects,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Called by the find references search engine to determine which projects should be
        /// searched for a given symbol.  The returned projects will then be searched in parallel. If
        /// the implementation does not care about the provided symbol then null can be returned
        /// from this method.
        /// 
        /// Implementations should endeavor to keep the list of returned projects as small as
        /// possible to keep search time down to a minimum.  Returning the entire list of projects in
        /// a solution is not recommended (unless, of course, there is reasonable reason to believe
        /// there are references in every project).
        /// 
        /// Implementations of this method must be thread-safe.
        /// </summary>
        Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken);

        /// <summary>
        /// Called by the find references search engine to determine which documents in the supplied
        /// project need to be searched for references.  Only projects returned by
        /// DetermineProjectsToSearch will be passed to this method.
        /// 
        /// Implementations should endeavor to keep the list of returned documents as small as
        /// possible to keep search time down to a minimum.  Returning the entire list of documents
        /// in a project is not recommended (unless, of course, there is reasonable reason to
        /// believe there are references in every document).
        /// 
        /// Implementations of this method must be thread-safe.
        /// </summary>
        Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ISymbol symbol, Project project, IImmutableSet<Document> documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Called by the find references search engine to determine the set of reference locations
        /// in the provided document.  Only documents returned by DetermineDocumentsToSearch will be
        /// passed to this method. 
        /// 
        /// Implementations of this method must be thread-safe.
        /// </summary>
        Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            SymbolAndProjectId symbolAndProjectId, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);
    }
}
