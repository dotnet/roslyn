// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

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

        // Mapping from symbols (unified across metadata/retargeting) and the set of symbols that was produced for 
        // them in the case of linked files across projects.  This allows references to be found to any of the unified
        // symbols, while the user only gets a single reported group back that corresponds to that entire set.
        //
        // This is a normal dictionary that is not locked.  It is only ever read and written to serially from within the
        // high level project-walking code in this method.
        var symbolToGroup = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);

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

        // Safe to call as we're in the entry-point method, and nothing is running concurrently with this call.
        var allSymbolsAndGroups = await ReportGroupsSeriallyAsync(
            [.. symbolSet], symbolToGroup, cancellationToken).ConfigureAwait(false);

        // Process projects in dependency graph order so that any compilations built by one are available for later
        // projects. We only have to examine the projects containing the documents requested though.
        var dependencyGraph = _solution.GetProjectDependencyGraph();
        var projectsToSearch = documents.Select(d => d.Project).Where(p => p.SupportsCompilation).ToImmutableHashSet();

        foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
        {
            var currentProject = _solution.GetRequiredProject(projectId);
            if (!projectsToSearch.Contains(currentProject))
                continue;

            // Safe to call as we're in the entry-point method, and it's only serially looping over the projects when
            // calling into this.
            await PerformSearchInProjectSeriallyAsync(
                currentProject, allSymbolsAndGroups, symbolToGroup).ConfigureAwait(false);
        }

        return;

        async ValueTask PerformSearchInProjectSeriallyAsync(
            Project project, ImmutableArray<(ISymbol symbol, SymbolGroup group)> symbols, Dictionary<ISymbol, SymbolGroup> symbolToGroup)
        {
            using var _ = PooledDictionary<ISymbol, PooledHashSet<string>>.GetInstance(out var symbolToGlobalAliases);
            try
            {
                // Compute global aliases up front for the project so it can be used below for all the symbols we're
                // searching for.
                await AddGlobalAliasesAsync(project, symbols, symbolToGlobalAliases, cancellationToken).ConfigureAwait(false);

                foreach (var document in documents)
                {
                    if (document.Project != project)
                        continue;

                    // Safe to call as we're only in a serial context ourselves.
                    await PerformSearchInDocumentSeriallyAsync(
                        document, symbols, symbolToGroup, symbolToGlobalAliases).ConfigureAwait(false);
                }
            }
            finally
            {
                FreeGlobalAliases(symbolToGlobalAliases);
            }
        }

        async ValueTask PerformSearchInDocumentSeriallyAsync(
            Document document,
            ImmutableArray<(ISymbol symbol, SymbolGroup group)> symbols,
            Dictionary<ISymbol, SymbolGroup> symbolToGroup,
            PooledDictionary<ISymbol, PooledHashSet<string>> symbolToGlobalAliases)
        {
            // We're doing to do all of our processing of this document at once.  This will necessitate all the
            // appropriate finders checking this document for hits.  We're likely going to need to perform syntax
            // and semantics checks in this file.  So just grab those once here and hold onto them for the lifetime
            // of this call.
            var cache = await FindReferenceCache.GetCacheAsync(document, cancellationToken).ConfigureAwait(false);

            foreach (var (symbol, group) in symbols)
            {
                var state = new FindReferencesDocumentState(
                    cache, TryGet(symbolToGlobalAliases, symbol));

                // Safe to call as we're only in a serial context ourselves.
                await PerformSearchInDocumentSeriallyWorkerAsync(
                    symbol, group, symbolToGroup, state).ConfigureAwait(false);
            }
        }

        async ValueTask PerformSearchInDocumentSeriallyWorkerAsync(
            ISymbol symbol, SymbolGroup group, Dictionary<ISymbol, SymbolGroup> symbolToGroup, FindReferencesDocumentState state)
        {
            // Always perform a normal search, looking for direct references to exactly that symbol.
            await DirectSymbolSearchAsync(symbol, group, state).ConfigureAwait(false);

            // Now, for symbols that could involve inheritance, look for references to the same named entity, and
            // see if it's a reference to a symbol that shares an inheritance relationship with that symbol.
            //
            // Safe to call as we're only in a serial context ourselves.
            await InheritanceSymbolSearchSeriallyAsync(symbol, symbolToGroup, state).ConfigureAwait(false);
        }

        async ValueTask DirectSymbolSearchAsync(ISymbol symbol, SymbolGroup group, FindReferencesDocumentState state)
        {
            await ProducerConsumer<FinderLocation>.RunAsync(
                ProducerConsumerOptions.SingleReaderWriterOptions,
                static (callback, args, cancellationToken) =>
                {
                    var (@this, symbol, group, state) = args;

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
                    var (@this, symbol, group, state) = args;
                    var converted = await ConvertLocationsAsync(@this, values, symbol, group, cancellationToken).ConfigureAwait(false);
                    await @this._progress.OnReferencesFoundAsync(converted, cancellationToken).ConfigureAwait(false);
                },
                args: (@this: this, symbol, group, state),
                cancellationToken).ConfigureAwait(false);
        }

        static async Task<ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)>> ConvertLocationsAsync(
            FindReferencesSearchEngine @this, IAsyncEnumerable<FinderLocation> locations, ISymbol symbol, SymbolGroup group, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)>.GetInstance(out var result);

            // Transform the individual finder-location objects to "group/symbol/location" tuples.
            await foreach (var location in locations)
                result.Add((group, symbol, location.Location));

            return result.ToImmutableAndClear();
        }

        async ValueTask InheritanceSymbolSearchSeriallyAsync(
            ISymbol symbol, Dictionary<ISymbol, SymbolGroup> symbolToGroup, FindReferencesDocumentState state)
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
                        // Safe to call this as we're only being called from within a serial context ourselves.
                        var candidateGroup = await ReportGroupSeriallyAsync(
                            candidate, symbolToGroup, cancellationToken).ConfigureAwait(false);

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
