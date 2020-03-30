// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using ProjectToDocumentMap = Dictionary<Project, MultiDictionary<Document, (SymbolAndProjectId symbolAndProjectId, IReferenceFinder finder)>>;

    internal partial class FindReferencesSearchEngine
    {
        private readonly Solution _solution;
        private readonly IImmutableSet<Document> _documents;
        private readonly ImmutableArray<IReferenceFinder> _finders;
        private readonly IStreamingProgressTracker _progressTracker;
        private readonly IStreamingFindReferencesProgress _progress;
        private readonly CancellationToken _cancellationToken;
        private readonly ProjectDependencyGraph _dependencyGraph;
        private readonly FindReferencesSearchOptions _options;

        public FindReferencesSearchEngine(
            Solution solution,
            IImmutableSet<Document> documents,
            ImmutableArray<IReferenceFinder> finders,
            IStreamingFindReferencesProgress progress,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            _documents = documents;
            _solution = solution;
            _finders = finders;
            _progress = progress;
            _cancellationToken = cancellationToken;
            _dependencyGraph = solution.GetProjectDependencyGraph();
            _options = options;

            _progressTracker = progress.ProgressTracker;
        }

        public async Task FindReferencesAsync(SymbolAndProjectId symbolAndProjectId)
        {
            await _progress.OnStartedAsync().ConfigureAwait(false);
            try
            {
                await using var _ = await _progressTracker.AddSingleItemAsync().ConfigureAwait(false);

                var symbols = await DetermineAllSymbolsAsync(symbolAndProjectId).ConfigureAwait(false);

                var projectMap = await CreateProjectMapAsync(symbols).ConfigureAwait(false);
                var projectToDocumentMap = await CreateProjectToDocumentMapAsync(projectMap).ConfigureAwait(false);
                ValidateProjectToDocumentMap(projectToDocumentMap);

                await ProcessAsync(projectToDocumentMap).ConfigureAwait(false);
            }
            finally
            {
                await _progress.OnCompletedAsync().ConfigureAwait(false);
            }
        }

        private async Task ProcessAsync(ProjectToDocumentMap projectToDocumentMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessAsync, _cancellationToken))
            {
                // quick exit
                if (projectToDocumentMap.Count == 0)
                {
                    return;
                }

                // Get the connected components of the dependency graph and process each individually.
                // That way once a component is done we can throw away all the memory associated with
                // it.
                // For each connected component, we'll process the individual projects from bottom to
                // top.  i.e. we'll first process the projects with no dependencies.  Then the projects
                // that depend on those projects, and so on.  This way we always have created the 
                // dependent compilations when they're needed by later projects.  If we went the other
                // way (i.e. processed the projects with lots of project dependencies first), then we'd
                // have to create all their dependent compilations in order to get their compilation.
                // This would be very expensive and would take a lot of time before we got our first
                // result.
                var connectedProjects = _dependencyGraph.GetDependencySets(_cancellationToken);

                // Add a progress item for each (document, symbol, finder) set that we will execute.
                // We'll mark the item as completed in "ProcessDocumentAsync".
                var totalFindCount = projectToDocumentMap.Sum(
                    kvp1 => kvp1.Value.Sum(kvp2 => kvp2.Value.Count));
                await _progressTracker.AddItemsAsync(totalFindCount).ConfigureAwait(false);

                // Now, go through each connected project set and process it independently.
                foreach (var connectedProjectSet in connectedProjects)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    await ProcessProjectsAsync(
                        connectedProjectSet, projectToDocumentMap).ConfigureAwait(false);
                }
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateProjectToDocumentMap(
            ProjectToDocumentMap projectToDocumentMap)
        {
            var set = new HashSet<(SymbolAndProjectId symbolAndProjectId, IReferenceFinder finder)>();

            foreach (var documentMap in projectToDocumentMap.Values)
            {
                foreach (var documentToFinderList in documentMap)
                {
                    set.Clear();

                    foreach (var finder in documentToFinderList.Value)
                    {
                        Debug.Assert(set.Add(finder));
                    }
                }
            }
        }

        private Task HandleLocationAsync(SymbolAndProjectId symbolAndProjectId, ReferenceLocation location)
            => _progress.OnReferenceFoundAsync(symbolAndProjectId, location);
    }
}
