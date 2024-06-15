// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal partial class FindReferencesSearchEngine
{
    public async Task FindReferencesInDocumentsAsync(
        ISymbol originalSymbol, IImmutableSet<Document> documents, CancellationToken cancellationToken)
    {
        // Caller needs to pass unidirectional cascading to make this search efficient.  If we only have
        // unidirectional cascading, then we only need to check the potential matches we find in the file against
        // the starting symbol.
        Debug.Assert(_options.UnidirectionalHierarchyCascade);

        var unifiedSymbols = new MetadataUnifyingSymbolHashSet
        {
            originalSymbol
        };

        // As we hit symbols, we may have to compute if they have an inheritance relationship to the symbols we're
        // searching for.  Cache those results so we don't have to continually perform them.
        //
        // Note: this is a dictionary as we do all our work serially (though asynchronously).  If we ever change to
        // doing things concurrently, this will need to be changed.
        var hasInheritanceRelationshipCache = new Dictionary<(ISymbol searchSymbol, ISymbol candidateSymbol), bool>();

        // Create and report the initial set of symbols to search for.  This includes linked and cascaded symbols. It does
        // not walk up/down the inheritance hierarchy.
        var symbolSet = await SymbolSet.DetermineInitialSearchSymbolsAsync(this, unifiedSymbols, cancellationToken).ConfigureAwait(false);
        var allSymbols = symbolSet.ToImmutableArray();
        await ReportGroupsAsync(allSymbols, cancellationToken).ConfigureAwait(false);

        // Process projects in dependency graph order so that any compilations built by one are available for later
        // projects. We only have to examine the projects containing the documents requested though.
        var dependencyGraph = _solution.GetProjectDependencyGraph();
        var projectsToSearch = documents.Select(d => d.Project).Where(p => p.SupportsCompilation).ToImmutableHashSet();

        foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
        {
            var currentProject = _solution.GetRequiredProject(projectId);
            if (projectsToSearch.Contains(currentProject))
                await PerformSearchInProjectAsync(allSymbols, currentProject).ConfigureAwait(false);
        }

        return;

        async ValueTask PerformSearchInProjectAsync(ImmutableArray<ISymbol> symbols, Project project)
        {
            using var _ = PooledDictionary<ISymbol, PooledHashSet<string>>.GetInstance(out var symbolToGlobalAliases);
            try
            {
                // Compute global aliases up front for the project so it can be used below for all the symbols we're
                // searching for.
                await AddGlobalAliasesAsync(project, symbols, symbolToGlobalAliases, cancellationToken).ConfigureAwait(false);

                foreach (var document in documents)
                {
                    if (document.Project == project)
                        await PerformSearchInDocumentAsync(symbols, document, symbolToGlobalAliases).ConfigureAwait(false);
                }
            }
            finally
            {
                FreeGlobalAliases(symbolToGlobalAliases);
            }
        }

        async ValueTask PerformSearchInDocumentAsync(
            ImmutableArray<ISymbol> symbols,
            Document document,
            PooledDictionary<ISymbol, PooledHashSet<string>> symbolToGlobalAliases)
        {
            // We're doing to do all of our processing of this document at once.  This will necessitate all the
            // appropriate finders checking this document for hits.  We're likely going to need to perform syntax
            // and semantics checks in this file.  So just grab those once here and hold onto them for the lifetime
            // of this call.
            var cache = await FindReferenceCache.GetCacheAsync(document, cancellationToken).ConfigureAwait(false);

            foreach (var symbol in symbols)
            {
                var state = new FindReferencesDocumentState(cache, TryGet(symbolToGlobalAliases, symbol));
                await PerformSearchInDocumentWorkerAsync(symbol, state).ConfigureAwait(false);
            }
        }

        async ValueTask PerformSearchInDocumentWorkerAsync(ISymbol symbol, FindReferencesDocumentState state)
        {
            // Always perform a normal search, looking for direct references to exactly that symbol.
            await DirectSymbolSearchAsync(symbol, state).ConfigureAwait(false);

            // Now, for symbols that could involve inheritance, look for references to the same named entity, and
            // see if it's a reference to a symbol that shares an inheritance relationship with that symbol.
            await InheritanceSymbolSearchAsync(symbol, state).ConfigureAwait(false);
        }

        async ValueTask DirectSymbolSearchAsync(ISymbol symbol, FindReferencesDocumentState state)
        {
            await ProducerConsumer<FinderLocation>.RunAsync(
                ProducerConsumerOptions.SingleReaderWriterOptions,
                static (callback, args, cancellationToken) =>
                {
                    var (@this, symbol, state) = args;

                    // We don't bother calling into the finders in parallel as there's only ever one that applies for a
                    // particular symbol kind.  All the rest bail out immediately after a quick type-check.  So there's
                    // no benefit in forking out to have only one of them end up actually doing work.
                    foreach (var finder in @this._finders)
                    {
                        finder.FindReferencesInDocument(
                            symbol, state,
                            static (finderLocation, callback) => callback(finderLocation),
                            callback, @this._options, cancellationToken);
                    }

                    return Task.CompletedTask;
                },
                consumeItems: static async (values, args, cancellationToken) =>
                {
                    var (@this, symbol, state) = args;
                    var converted = await ConvertLocationsAndReportGroupsAsync(@this, values, symbol, cancellationToken).ConfigureAwait(false);
                    await @this._progress.OnReferencesFoundAsync(converted, cancellationToken).ConfigureAwait(false);
                },
                args: (@this: this, symbol, state),
                cancellationToken).ConfigureAwait(false);
        }

        static async Task<ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)>> ConvertLocationsAndReportGroupsAsync(
            FindReferencesSearchEngine @this, IAsyncEnumerable<FinderLocation> locations, ISymbol symbol, CancellationToken cancellationToken)
        {
            SymbolGroup? group = null;

            using var _ = ArrayBuilder<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)>.GetInstance(out var result);

            // Transform the individual finder-location objects to "group/symbol/location" tuples.
            await foreach (var location in locations)
            {
                // The first time we see the location for a symbol, report its group.
                group ??= await @this.ReportGroupAsync(symbol, cancellationToken).ConfigureAwait(false);
                result.Add((group, symbol, location.Location));
            }

            return result.ToImmutableAndClear();
        }

        async ValueTask InheritanceSymbolSearchAsync(ISymbol symbol, FindReferencesDocumentState state)
        {
            if (InvolvesInheritance(symbol))
            {
                var tokens = AbstractReferenceFinder.FindMatchingIdentifierTokens(state, symbol.Name, cancellationToken);

                foreach (var token in tokens)
                {
                    var parent = state.SyntaxFacts.TryGetBindableParent(token) ?? token.GetRequiredParent();
                    var symbolInfo = state.Cache.GetSymbolInfo(parent, cancellationToken);

                    var (matched, candidate, candidateReason) = await HasInheritanceRelationshipAsync(symbol, symbolInfo).ConfigureAwait(false);
                    if (matched)
                    {
                        // Ensure we report this new symbol/group in case it's the first time we're seeing it.
                        var candidateGroup = await ReportGroupAsync(candidate, cancellationToken).ConfigureAwait(false);

                        var location = AbstractReferenceFinder.CreateReferenceLocation(state, token, candidateReason, cancellationToken);
                        await _progress.OnReferencesFoundAsync([(candidateGroup, candidate, location)], cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        async ValueTask<(bool matched, ISymbol candidate, CandidateReason candidateReason)> HasInheritanceRelationshipAsync(
            ISymbol symbol, SymbolInfo symbolInfo)
        {
            if (await HasInheritanceRelationshipSingleAsync(symbol, symbolInfo.Symbol).ConfigureAwait(false))
                return (matched: true, symbolInfo.Symbol!, CandidateReason.None);

            foreach (var candidate in symbolInfo.CandidateSymbols)
            {
                if (await HasInheritanceRelationshipSingleAsync(symbol, candidate).ConfigureAwait(false))
                    return (matched: true, candidate, symbolInfo.CandidateReason);
            }

            return default;
        }

        async ValueTask<bool> HasInheritanceRelationshipSingleAsync(ISymbol searchSymbol, [NotNullWhen(true)] ISymbol? candidate)
        {
            if (candidate is null)
                return false;

            var key = (searchSymbol: searchSymbol.GetOriginalUnreducedDefinition(), candidate: candidate.GetOriginalUnreducedDefinition());
            if (!hasInheritanceRelationshipCache.TryGetValue(key, out var relationship))
            {
                relationship = await ComputeInheritanceRelationshipAsync(key.searchSymbol, key.candidate).ConfigureAwait(false);
                hasInheritanceRelationshipCache[key] = relationship;
            }

            return relationship;
        }

        async Task<bool> ComputeInheritanceRelationshipAsync(
            ISymbol searchSymbol, ISymbol candidate)
        {
            // Counter-intuitive, but if these are matching symbols, they do *not* have an inheritance relationship.
            // We do *not* want to report these as they would have been found in the original call to the finders in
            // PerformSearchInTextSpanAsync.
            if (SymbolFinder.OriginalSymbolsMatch(_solution, searchSymbol, candidate))
                return false;

            // walk up the original symbol's inheritance hierarchy to see if we hit the candidate. Don't walk down
            // derived types here.  The point of this algorithm is to only walk upwards looking for matches.
            var searchSymbolUpSet = await SymbolSet.CreateAsync(
                this, [searchSymbol], includeImplementationsThroughDerivedTypes: false, cancellationToken).ConfigureAwait(false);
            foreach (var symbolUp in searchSymbolUpSet.GetAllSymbols())
            {
                if (SymbolFinder.OriginalSymbolsMatch(_solution, symbolUp, candidate))
                    return true;
            }

            // walk up the candidate's inheritance hierarchy to see if we hit the original symbol. Don't walk down
            // derived types here.  The point of this algorithm is to only walk upwards looking for matches.
            var candidateSymbolUpSet = await SymbolSet.CreateAsync(
                this, [candidate], includeImplementationsThroughDerivedTypes: false, cancellationToken).ConfigureAwait(false);
            foreach (var candidateUp in candidateSymbolUpSet.GetAllSymbols())
            {
                if (SymbolFinder.OriginalSymbolsMatch(_solution, searchSymbol, candidateUp))
                    return true;
            }

            return false;
        }
    }
}
