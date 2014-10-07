// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private readonly Solution solution;
        private readonly IImmutableSet<Document> documents;
        private readonly ImmutableArray<IReferenceFinder> finders;
        private readonly IFindReferencesProgress progress;
        private readonly CancellationToken cancellationToken;
        private readonly ProjectDependencyGraph dependencyGraph;

        /// <summary>
        /// Mapping from a document to the list of reference locations found in it.  Kept around so
        /// we only notify the callback once when a location is found for a reference (in case
        /// multiple finders find the same reference location for a symbol).
        /// </summary>
        private readonly ConcurrentDictionary<Document, ConcurrentSet<ReferenceLocation>> documentToLocationMap = new ConcurrentDictionary<Document, ConcurrentSet<ReferenceLocation>>();
        private static readonly Func<Document, ConcurrentSet<ReferenceLocation>> createDocumentLocations = _ => new ConcurrentSet<ReferenceLocation>();

        /// <summary>
        /// The resultant collection of all references found per symbol.
        /// </summary>
        private readonly ConcurrentDictionary<ISymbol, ConcurrentSet<ReferenceLocation>> foundReferences = new ConcurrentDictionary<ISymbol, ConcurrentSet<ReferenceLocation>>();
        private static readonly Func<ISymbol, ConcurrentSet<ReferenceLocation>> createSymbolLocations = _ => new ConcurrentSet<ReferenceLocation>();

        public FindReferencesSearchEngine(
            Solution solution,
            IImmutableSet<Document> documents,
            ImmutableArray<IReferenceFinder> finders,
            IFindReferencesProgress progress,
            CancellationToken cancellationToken)
        {
            this.documents = documents;
            this.solution = solution;
            this.finders = finders;
            this.progress = progress;
            this.cancellationToken = cancellationToken;
            this.dependencyGraph = solution.GetProjectDependencyGraph();
        }

        public async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(ISymbol symbol)
        {
            progress.OnStarted();
            progress.ReportProgress(0, 1);
            try
            {
                var symbols = await DetermineAllSymbolsAsync(symbol).ConfigureAwait(false);

                var projectMap = await CreateProjectMapAsync(symbols).ConfigureAwait(false);
                var documentMap = await CreateDocumentMapAsync(projectMap).ConfigureAwait(false);
                await ProcessAsync(documentMap).ConfigureAwait(false);
            }
            finally
            {
                progress.ReportProgress(1, 1);
                progress.OnCompleted();
            }

            return this.foundReferences.Select(kvp => new ReferencedSymbol(kvp.Key, kvp.Value.ToImmutableArray())).ToImmutableArray();
        }

        private async Task ProcessAsync(
            ConcurrentDictionary<Document, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>> documentMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessAsync, this.cancellationToken))
            {
                // quick exit
                if (documentMap.Count == 0)
                {
                    return;
                }

                var wrapper = new ProgressWrapper(this.progress, documentMap.Count);

                // Get the connected components of the dependency graph and process each individually.
                // That way once a component is done we can throw away all the memory associated with
                // it.
                var connectedProjects = this.dependencyGraph.GetDependencySets(cancellationToken);
                var projectMap = CreateProjectMap(documentMap);

                foreach (var projectSet in connectedProjects)
                {
                    this.cancellationToken.ThrowIfCancellationRequested();

                    await ProcessProjectsAsync(projectSet, projectMap, wrapper).ConfigureAwait(false);
                }
            }
        }

        private static readonly Func<Project, Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>> documentMapGetter =
            _ => new Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>();

        private static readonly Func<Document, List<ValueTuple<ISymbol, IReferenceFinder>>> queueGetter =
            _ => new List<ValueTuple<ISymbol, IReferenceFinder>>();

        private static Dictionary<Project, Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>> CreateProjectMap(
            ConcurrentDictionary<Document, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>> map)
        {
            Contract.Requires(map.Count > 0);

            var projectMap = new Dictionary<Project, Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>>();
            foreach (var kv in map)
            {
                var documentMap = projectMap.GetOrAdd(kv.Key.Project, documentMapGetter);
                var queue = documentMap.GetOrAdd(kv.Key, queueGetter);

                queue.AddRange(kv.Value);
            }

            ValidateProjectMap(projectMap);
            return projectMap;
        }

        [Conditional("DEBUG")]
        private static void ValidateProjectMap(Dictionary<Project, Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>> projectMap)
        {
            var set = new HashSet<ValueTuple<ISymbol, IReferenceFinder>>();

            foreach (var map in projectMap.Values)
            {
                foreach (var finderList in map.Values)
                {
                    set.Clear();

                    foreach (var finder in finderList)
                    {
                        Contract.Requires(set.Add(finder));
                    }
                }
            }
        }

        private void HandleLocation(ISymbol symbol, ReferenceLocation location)
        {
            this.foundReferences.GetOrAdd(symbol, createSymbolLocations).Add(location);
            this.progress.OnReferenceFound(symbol, location);
        }
    }
}