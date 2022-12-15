// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        public async Task FindReferencesInDocumentsAsync(
            ISymbol originalSymbol, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            // Caller needs to pass unidirectional cascading to make this search efficient.  If we only have
            // unidirectional cascading, then we only need to check the potential matches we find in the file against
            // the starting symbol.
            Debug.Assert(_options.UnidirectionalHierarchyCascade);
            var unifiedSymbols = new MetadataUnifyingSymbolHashSet();
            unifiedSymbols.Add(originalSymbol);

            var hasInheritanceRelationshipCache = new ConcurrentDictionary<(ISymbol searchSymbol, ISymbol candidateSymbol), AsyncLazy<bool>>();

            await _progress.OnStartedAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var disposable = await _progressTracker.AddSingleItemAsync(cancellationToken).ConfigureAwait(false);
                await using var _ = disposable.ConfigureAwait(false);

                // Create the initial set of symbols to search for.  This includes linked and cascaded symbols. It does
                // not walk up/down the inheritance hierarchy.
                var symbolSet = await SymbolSet.DetermineInitialSearchSymbolsAsync(this, unifiedSymbols, cancellationToken).ConfigureAwait(false);

                // Report the initial set of symbols to the caller.
                var allSymbols = symbolSet.ToImmutableArray();
                await ReportGroupsAsync(allSymbols, cancellationToken).ConfigureAwait(false);

                // Determine the set of projects we actually have to walk to find results in.  This is only the set of
                // projects that all these documents are in.
                var projectsToSearch = documents.Select(d => d.Project).ToImmutableHashSet();

                // Process projects in dependency graph order so that any compilations built by one are available for later projects.
                var dependencyGraph = _solution.GetProjectDependencyGraph();
                await _progressTracker.AddItemsAsync(projectsToSearch.Count, cancellationToken).ConfigureAwait(false);

                foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
                {
                    var currentProject = _solution.GetRequiredProject(projectId);
                    if (projectsToSearch.Contains(currentProject))
                        await PerformSearchInProjectAsync(allSymbols, currentProject).ConfigureAwait(false);
                }
            }
            finally
            {
                await _progress.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
            }

            async Task PerformSearchInProjectAsync(
                ImmutableArray<ISymbol> symbols, Project project)
            {
                using var _1 = PooledDictionary<ISymbol, PooledHashSet<string>>.GetInstance(out var symbolToGlobalAliases);
                try
                {
                    // Compute global aliases up front for the project so it can be used below for all the symbols we're
                    // searching for.
                    await AddGlobalAliasesAsync(
                        project, symbols, symbolToGlobalAliases, cancellationToken).ConfigureAwait(false);

                    var documentsInProject = documents.Where(d => d.Project == project);
                    foreach (var document in documentsInProject)
                    {
                        await PerformSearchInDocumentAsync(
                            symbols, document, symbolToGlobalAliases).ConfigureAwait(false);
                    }
                }
                finally
                {
                    FreeGlobalAliases(symbolToGlobalAliases);
                }
            }

            async Task PerformSearchInDocumentAsync(
                ImmutableArray<ISymbol> symbols,
                Document document,
                PooledDictionary<ISymbol, PooledHashSet<string>> symbolToGlobalAliases)
            {
                // We're doing to do all of our processing of this document at once.  This will necessitate all the
                // appropriate finders checking this document for hits.  We're likely going to need to checks in this
                // file, which will require syntax and semantics.  So just grab those once here and hold onto them for
                // the lifetime of this call.
                var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var cache = FindReferenceCache.GetCache(model);

                foreach (var symbol in symbols)
                {
                    var globalAliases = TryGet(symbolToGlobalAliases, symbol);
                    var state = new FindReferencesDocumentState(
                        document, model, root, cache, globalAliases);

                    await PerformSearchInDocumentWorkerAsync(symbol, document, state).ConfigureAwait(false);
                }
            }

            async Task PerformSearchInDocumentWorkerAsync(
                ISymbol symbol, Document document, FindReferencesDocumentState state)
            {
                {
                    // This is safe to just blindly read. We can only ever get here after the call to ReportGroupsAsync
                    // happened.  So there must be a group for this symbol in our map.
                    var group = _symbolToGroup[symbol];

                    // Always perform a normal search, looking directly for references to that symbol.
                    foreach (var finder in _finders)
                    {
                        var references = await finder.FindReferencesInDocumentAsync(
                            symbol, state, _options, cancellationToken).ConfigureAwait(false);
                        foreach (var (_, location) in references)
                            await _progress.OnReferenceFoundAsync(group, symbol, location, cancellationToken).ConfigureAwait(false);
                    }
                }

                // We may be looking for symbols that can be virtual and have other methods related to them (like an
                // override, or implemented interface method).  When searching the source, we may end up looking at a
                // token that hits one of these other symbols.  When we do, we have to then see if that symbol is
                // actually an inheritance match for the symbol we're looking at.

                if (InvolvesInheritance(symbol))
                {
                    var tokens = await AbstractReferenceFinder.FindMatchingIdentifierTokensAsync(
                        state, symbol.Name, cancellationToken).ConfigureAwait(false);

                    foreach (var token in tokens)
                    {
                        var parent = state.SyntaxFacts.TryGetBindableParent(token) ?? token.GetRequiredParent();
                        var symbolInfo = state.Cache.GetSymbolInfo(parent, cancellationToken);

                        var (matched, candidate, candidateReason) = await HasInheritanceRelationshipAsync(symbol, symbolInfo).ConfigureAwait(false);
                        if (matched)
                        {
                            await ReportGroupsAsync(ImmutableArray.Create(candidate), cancellationToken).ConfigureAwait(false);

                            // This is safe to just blindly read. We can only ever get here after the call to ReportGroupsAsync
                            // happened.  So there must be a group for this symbol in our map.
                            var candidateGroup = _symbolToGroup[candidate];

                            var location = AbstractReferenceFinder.CreateFinderLocation(state, token, candidateReason, cancellationToken);
                            await _progress.OnReferenceFoundAsync(candidateGroup, candidate, location.Location, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            async Task<(bool matched, ISymbol candidate, CandidateReason candidateReason)> HasInheritanceRelationshipAsync(
                ISymbol symbol, SymbolInfo symbolInfo)
            {
                if (await HasInheritanceRelationshipSingleAsync(symbol, symbolInfo.Symbol).ConfigureAwait(false))
                    return (matched: true, symbolInfo.Symbol, CandidateReason.None);

                foreach (var candidate in symbolInfo.CandidateSymbols)
                {
                    if (await HasInheritanceRelationshipSingleAsync(symbol, candidate).ConfigureAwait(false))
                        return (matched: true, candidate, symbolInfo.CandidateReason);
                }

                return default;
            }

            async ValueTask<bool> HasInheritanceRelationshipSingleAsync(ISymbol symbol, ISymbol? candidate)
            {
                if (candidate is null)
                    return false;

                var relationship = hasInheritanceRelationshipCache.GetOrAdd(
                    (symbol.GetOriginalUnreducedDefinition(), candidate.GetOriginalUnreducedDefinition()),
                    t => AsyncLazy.Create(cancellationToken => ComputeInheritanceRelationshipAsync(this, t.searchSymbol, t.candidateSymbol, cancellationToken), cacheResult: true));
                return await relationship.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }

            static async Task<bool> ComputeInheritanceRelationshipAsync(
                FindReferencesSearchEngine engine, ISymbol searchSymbol, ISymbol candidate, CancellationToken cancellationToken)
            {
                // Counter-intuitive, but if these are matching symbols, they do *not* have an inheritance relationship.
                // We do *not* want to report these as they would have been found in the original call to the finders in
                // PerformSearchInTextSpanAsync.
                if (await SymbolFinder.OriginalSymbolsMatchAsync(engine._solution, searchSymbol, candidate, cancellationToken).ConfigureAwait(false))
                    return false;

                // walk upwards from each symbol, seeing if we hit the other symbol.
                var searchSymbolSet = await SymbolSet.CreateAsync(engine, new() { searchSymbol }, cancellationToken).ConfigureAwait(false);
                foreach (var symbolUp in searchSymbolSet.GetAllSymbols())
                {
                    if (await SymbolFinder.OriginalSymbolsMatchAsync(engine._solution, symbolUp, candidate, cancellationToken).ConfigureAwait(false))
                        return true;
                }

                var candidateSymbolSet = await SymbolSet.CreateAsync(engine, new() { candidate }, cancellationToken).ConfigureAwait(false);
                foreach (var candidateUp in candidateSymbolSet.GetAllSymbols())
                {
                    if (await SymbolFinder.OriginalSymbolsMatchAsync(engine._solution, searchSymbol, candidateUp, cancellationToken).ConfigureAwait(false))
                        return true;
                }

                return false;
            }
        }
    }
}
