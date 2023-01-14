// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Determines what, if any, global alias names could potentially map this symbol in this project.
        /// Note that this result is allowed to return global aliases that don't actually map to this symbol.
        /// For example, given symbol <c>A.X</c> and <c>global alias G = B.X</c>, <c>G</c> might be returned
        /// in a search for <c>A.X</c> because they both end in <c>X</c>.
        /// </summary>
        Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken);

        /// <summary>
        /// Called by the find references search engine when a new symbol definition is found.
        /// Implementations can then choose to request more symbols be searched for.  For example, an
        /// implementation could choose for the find references search engine to cascade to
        /// constructors when searching for standard types.
        /// 
        /// Implementations of this method must be thread-safe.
        /// </summary>
        ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            ISymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken);

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
            ISymbol symbol, HashSet<string>? globalAliases,
            Project project, IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Called by the find references search engine to determine the set of reference locations
        /// in the provided document.  Only documents returned by DetermineDocumentsToSearch will be
        /// passed to this method. 
        /// 
        /// Implementations of this method must be thread-safe.
        /// </summary>
        ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ISymbol symbol,
            FindReferencesDocumentState state,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken);
    }
}
