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
            ISymbol originalSymbol, IImmutableSet<(Document document, TextSpan textSpan)> documentsAndSpans, CancellationToken cancellationToken)
        {
            // Caller needs to pass unidirectional cascading to make this search efficient.  If we only have
            // unidirectional cascading, then we only need to check the potential matches we find in the file against
            // the starting symbol.
            Debug.Assert(_options.UnidirectionalHierarchyCascade);
            var unifiedSymbols = new MetadataUnifyingSymbolHashSet();
            unifiedSymbols.Add(originalSymbol);

            var symbolToMatch = new ConcurrentDictionary<ISymbol, bool>(MetadataUnifyingEquivalenceComparer.Instance);

            await _progress.OnStartedAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var disposable = await _progressTracker.AddSingleItemAsync(cancellationToken).ConfigureAwait(false);
                await using var _ = disposable.ConfigureAwait(false);

                // Create the initial set of symbols to search for.  As we walk the appropriate projects in the solution
                // we'll expand this set as we discover new symbols to search for in each project.
                var symbolSet = await SymbolSet.CreateAsync(this, unifiedSymbols, cancellationToken).ConfigureAwait(false);

                // Report the initial set of symbols to the caller.
                var allSymbols = symbolSet.GetAllSymbols();
                await ReportGroupsAsync(allSymbols, cancellationToken).ConfigureAwait(false);

                // Any matches against the symbol passed in, or in its 'up' set is automatically a match.
                foreach (var relatedSymbol in allSymbols)
                    symbolToMatch[relatedSymbol] = true;

                // Determine the set of projects we actually have to walk to find results in.  This is only the set of
                // projects that all these documents are in.
                var projectsToSearch = documentsAndSpans.Select(t => t.document.Project).ToImmutableHashSet();

                // Process projects in dependency graph order so that any compilations built by one are available for later projects.
                var dependencyGraph = _solution.GetProjectDependencyGraph();
                await _progressTracker.AddItemsAsync(projectsToSearch.Count, cancellationToken).ConfigureAwait(false);

                foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
                {
                    var currentProject = _solution.GetRequiredProject(projectId);
                    if (!projectsToSearch.Contains(currentProject))
                        continue;

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
                    await AddGlobalAliasesAsync(
                        project, symbols, symbolToGlobalAliases, cancellationToken).ConfigureAwait(false);

                    var documentsAndSpansInProject = documentsAndSpans.Where(t => t.document.Project == project);
                    foreach (var group in documentsAndSpansInProject.GroupBy(t => t.document))
                    {
                        await PerformSearchInDocumentAsync(
                            symbols, group.Key, group.Select(t => t.textSpan), symbolToGlobalAliases).ConfigureAwait(false);
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
                IEnumerable<TextSpan> textSpans,
                PooledDictionary<ISymbol, PooledHashSet<string>> symbolToGlobalAliases)
            {
                // We're doing to do all of our processing of this document at once.  This will necessitate all the
                // appropriate finders checking this document for hits.  We know that in the initial pass to determine
                // documents, this document was already considered a strong match (e.g. we know it contains the name of
                // the symbol being searched for).  As such, we're almost certainly going to have to do semantic checks
                // to now see if the candidate actually matches the symbol.  This will require syntax and semantics.  So
                // just grab those once here and hold onto them for the lifetime of this call.
                var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var cache = FindReferenceCache.GetCache(model);

                foreach (var symbol in symbols)
                {
                    var globalAliases = TryGet(symbolToGlobalAliases, symbol);
                    var state = new FindReferencesDocumentState(
                        document, model, root, cache, globalAliases);
                    foreach (var textSpan in textSpans)
                        await PerformSearchInTextSpanAsync(symbol, document, textSpan, state).ConfigureAwait(false);
                }
            }

            async Task PerformSearchInTextSpanAsync(
                ISymbol symbol, Document document, TextSpan textSpan, FindReferencesDocumentState state)
            {
                // This is safe to just blindly read. We can only ever get here after the call to ReportGroupsAsync
                // happened.  So there must be a group for this symbol in our map.
                var group = _symbolToGroup[symbol];

                if (symbol is IMethodSymbol or IPropertySymbol or IEventSymbol)
                {
                    // This is a complex case.  We're looking for symbols that can be virtual and have other methods
                    // related to them (like an override, or implemented interface method).  When searching the source,
                    // we may end up looking at a token that hits one of these other symbols.
                }
                else
                {

                    // Normal symbol without any inheritance concerns.  Just search for it using the appropriate finder
                    // for it in this document.
                    foreach (var finder in _finders)
                    {
                        var references = await finder.FindReferencesInDocumentAsync(
                            symbol, state, _options, textSpan, cancellationToken).ConfigureAwait(false);
                        foreach (var (_, location) in references)
                            await _progress.OnReferenceFoundAsync(group, symbol, location, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
