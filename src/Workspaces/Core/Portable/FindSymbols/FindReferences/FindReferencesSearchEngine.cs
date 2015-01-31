// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly Solution _solution;
        private readonly IImmutableSet<Document> _documents;
        private readonly ImmutableArray<IReferenceFinder> _finders;
        private readonly IFindReferencesProgress _progress;
        private readonly CancellationToken _cancellationToken;
        private readonly ProjectDependencyGraph _dependencyGraph;

        /// <summary>
        /// Mapping from a document to the list of reference locations found in it.  Kept around so
        /// we only notify the callback once when a location is found for a reference (in case
        /// multiple finders find the same reference location for a symbol).
        /// </summary>
        private readonly ConcurrentDictionary<Document, ConcurrentSet<ReferenceLocation>> _documentToLocationMap = new ConcurrentDictionary<Document, ConcurrentSet<ReferenceLocation>>();
        private static readonly Func<Document, ConcurrentSet<ReferenceLocation>> s_createDocumentLocations = _ => new ConcurrentSet<ReferenceLocation>();

        /// <summary>
        /// The resultant collection of all references found per symbol.
        /// </summary>
        private readonly ConcurrentDictionary<ISymbol, ConcurrentSet<ReferenceLocation>> _foundReferences = new ConcurrentDictionary<ISymbol, ConcurrentSet<ReferenceLocation>>();
        private static readonly Func<ISymbol, ConcurrentSet<ReferenceLocation>> s_createSymbolLocations = _ => new ConcurrentSet<ReferenceLocation>();

        public FindReferencesSearchEngine(
            Solution solution,
            IImmutableSet<Document> documents,
            ImmutableArray<IReferenceFinder> finders,
            IFindReferencesProgress progress,
            CancellationToken cancellationToken)
        {
            _documents = documents;
            _solution = solution;
            _finders = finders;
            _progress = progress;
            _cancellationToken = cancellationToken;
            _dependencyGraph = solution.GetProjectDependencyGraph();
        }

        public async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(ISymbol symbol)
        {
            _progress.OnStarted();
            _progress.ReportProgress(0, 1);
            try
            {
                var symbols = await DetermineAllSymbolsAsync(symbol).ConfigureAwait(false);

                var projectMap = await CreateProjectMapAsync(symbols).ConfigureAwait(false);
                var documentMap = await CreateDocumentMapAsync(projectMap).ConfigureAwait(false);
                await ProcessAsync(documentMap).ConfigureAwait(false);
            }
            finally
            {
                _progress.ReportProgress(1, 1);
                _progress.OnCompleted();
            }

            return _foundReferences.Select(kvp => new ReferencedSymbol(kvp.Key, kvp.Value.ToImmutableArray())).ToImmutableArray();
        }

        private async Task ProcessAsync(
            ConcurrentDictionary<Document, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>> documentMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessAsync, _cancellationToken))
            {
                // quick exit
                if (documentMap.Count == 0)
                {
                    return;
                }

                var wrapper = new ProgressWrapper(_progress, documentMap.Count);

                // Get the connected components of the dependency graph and process each individually.
                // That way once a component is done we can throw away all the memory associated with
                // it.
                var connectedProjects = _dependencyGraph.GetDependencySets(_cancellationToken);
                var projectMap = CreateProjectMap(documentMap);

                foreach (var projectSet in connectedProjects)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    await ProcessProjectsAsync(projectSet, projectMap, wrapper).ConfigureAwait(false);
                }
            }
        }

        private static readonly Func<Project, Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>> s_documentMapGetter =
            _ => new Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>();

        private static readonly Func<Document, List<ValueTuple<ISymbol, IReferenceFinder>>> s_queueGetter =
            _ => new List<ValueTuple<ISymbol, IReferenceFinder>>();

        private static Dictionary<Project, Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>> CreateProjectMap(
            ConcurrentDictionary<Document, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>> map)
        {
            Contract.Requires(map.Count > 0);

            var projectMap = new Dictionary<Project, Dictionary<Document, List<ValueTuple<ISymbol, IReferenceFinder>>>>();
            foreach (var kv in map)
            {
                var documentMap = projectMap.GetOrAdd(kv.Key.Project, s_documentMapGetter);
                var queue = documentMap.GetOrAdd(kv.Key, s_queueGetter);

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
            _foundReferences.GetOrAdd(symbol, s_createSymbolLocations).Add(location);
            _progress.OnReferenceFound(symbol, location);
        }
    }
}
