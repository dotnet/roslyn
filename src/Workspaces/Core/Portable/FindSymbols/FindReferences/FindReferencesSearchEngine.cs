// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private readonly Solution _solution;
        private readonly IImmutableSet<Document>? _documents;
        private readonly ImmutableArray<IReferenceFinder> _finders;
        private readonly IStreamingProgressTracker _progressTracker;
        private readonly IStreamingFindReferencesProgress _progress;
        private readonly FindReferencesSearchOptions _options;

        /// <summary>
        /// Scheduler to run our tasks on.  If we're in <see cref="FindReferencesSearchOptions.Explicit"/> mode, we'll
        /// run all our tasks concurrently.  Otherwise, we will run them serially using <see cref="s_exclusiveScheduler"/>
        /// </summary>
        private readonly TaskScheduler _scheduler;
        private static readonly TaskScheduler s_exclusiveScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;

        private readonly ConcurrentDictionary<ISymbol, SymbolGroup> _symbolToGroup = new();

        public FindReferencesSearchEngine(
            Solution solution,
            IImmutableSet<Document>? documents,
            ImmutableArray<IReferenceFinder> finders,
            IStreamingFindReferencesProgress progress,
            FindReferencesSearchOptions options)
        {
            _documents = documents;
            _solution = solution;
            _finders = finders;
            _progress = progress;
            _options = options;

            _progressTracker = progress.ProgressTracker;

            // If we're an explicit invocation, just defer to the threadpool to execute all our work in parallel to get
            // things done as quickly as possible.  If we're running implicitly, then use a
            // ConcurrentExclusiveSchedulerPair's exclusive scheduler as that's the most built-in way in the TPL to get
            // will run things serially.
            _scheduler = _options.Explicit ? TaskScheduler.Default : s_exclusiveScheduler;
        }

        public async Task FindReferencesAsync(ISymbol symbol, CancellationToken cancellationToken)
        {
            await _progress.OnStartedAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var _ = await _progressTracker.AddSingleItemAsync(cancellationToken).ConfigureAwait(false);

                // Create the initial set of symbols to search for.  As we walk the appropriate projects in the solution
                // we'll expand this set as we dicover new symbols to search for in each project.
                var symbolSet = await SymbolSet.CreateAsync(this, symbol, cancellationToken).ConfigureAwait(false);

                // Determine the set of projects we actually have to walk to find results in.  If the caller provided a
                // set of documents to search, we only bother with those.
                var projectsToSearch = await GetProjectsToSearchAsync(symbolSet.GetAllSymbols(), cancellationToken).ConfigureAwait(false);

                // We need to process projects in order when updating our symbol set.  Say we have three projects (A, B
                // and C), we cannot necessarily find inherited symbols in C until we have searched B.  Importantly,
                // while we're processing each project linearly to update the symbol set we're searching for, we still
                // then process the projects in parallel once we know the set of symbols we're searching for in that
                // project.
                var dependencyGraph = _solution.GetProjectDependencyGraph();
                await _progressTracker.AddItemsAsync(projectsToSearch.Count, cancellationToken).ConfigureAwait(false);

                using var _1 = ArrayBuilder<Task>.GetInstance(out var projectTasks);

                foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
                {
                    var currentProject = _solution.GetRequiredProject(projectId);
                    if (!projectsToSearch.Contains(currentProject))
                        continue;

                    // As we walk each project, attempt to grow the search set appropriately up and down the 
                    // inheritance hierarchy.  Note: this has to happen serially which is why we do it in this
                    // loop and not inside the concurrent project processing that happens below.
                    await symbolSet.InheritanceCascadeAsync(currentProject, cancellationToken).ConfigureAwait(false);

                    // Grab a copy of all the symbols we need to search for in this project.  Make sure we've notified
                    // all clients that these are the symbols we're searching for.
                    var allSymbols = symbolSet.GetAllSymbols();
                    await ReportGroupsAsync(allSymbols, cancellationToken).ConfigureAwait(false);

                    projectTasks.Add(Task.Factory.StartNew(
                        () => ProcessProjectAsync(currentProject, allSymbols, cancellationToken),
                        cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
                }

                // Now, wait for all projects to complete.
                await Task.WhenAll(projectTasks).ConfigureAwait(false);
            }
            finally
            {
                await _progress.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ReportGroupsAsync(ImmutableArray<ISymbol> allSymbols, CancellationToken cancellationToken)
        {
            foreach (var symbol in allSymbols)
            {
                if (!_symbolToGroup.ContainsKey(symbol))
                {
                    var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(symbol, _solution, cancellationToken).ConfigureAwait(false);
                    var group = new SymbolGroup(linkedSymbols);

                    foreach (var groupSymbol in group.Symbols)
                        Contract.ThrowIfFalse(_symbolToGroup.TryAdd(groupSymbol, group));

                    await _progress.OnDefinitionFoundAsync(group, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<HashSet<Project>> GetProjectsToSearchAsync(
            ImmutableArray<ISymbol> symbols, CancellationToken cancellationToken)
        {
            var projects = _documents != null
                ? _documents.Select(d => d.Project).ToImmutableHashSet()
                : _solution.Projects.ToImmutableHashSet();

            var result = new HashSet<Project>();

            foreach (var symbol in symbols)
                result.AddRange(await DependentProjectsFinder.GetDependentProjectsAsync(_solution, symbol, projects, cancellationToken).ConfigureAwait(false));

            return result;
        }

        private async Task ProcessProjectAsync(Project project, ImmutableArray<ISymbol> allSymbols, CancellationToken cancellationToken)
        {
            try
            {
                using var _ = PooledHashSet<Document>.GetInstance(out var allDocuments);
                foreach (var symbol in allSymbols)
                {
                    foreach (var finder in _finders)
                    {
                        var documents = await finder.DetermineDocumentsToSearchAsync(
                            symbol, project, _documents, _options, cancellationToken).ConfigureAwait(false);
                        allDocuments.AddRange(documents);
                    }
                }

                foreach (var document in allDocuments)
                    await ProcessDocumentsAsync(document, allSymbols, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await _progressTracker.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessDocumentsAsync(
            Document document,
            ImmutableArray<ISymbol> documentQueue,
            CancellationToken cancellationToken)
        {
            await _progress.OnFindInDocumentStartedAsync(document, cancellationToken).ConfigureAwait(false);

            SemanticModel? model = null;
            try
            {
                model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // start cache for this semantic model
                FindReferenceCache.Start(model);

                foreach (var symbol in documentQueue)
                    await ProcessDocumentAsync(document, model, symbol, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                FindReferenceCache.Stop(model);

                await _progress.OnFindInDocumentCompletedAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        private static readonly Func<Document, ISymbol, string> s_logDocument = (d, s) =>
            (d.Name != null && s.Name != null) ? string.Format("{0} - {1}", d.Name, s.Name) : string.Empty;

        private async Task ProcessDocumentAsync(
            Document document,
            SemanticModel semanticModel,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessDocumentAsync, s_logDocument, document, symbol, cancellationToken))
            {
                var group = _symbolToGroup[symbol];
                foreach (var finder in _finders)
                {
                    var references = await finder.FindReferencesInDocumentAsync(
                        symbol, document, semanticModel, _options, cancellationToken).ConfigureAwait(false);
                    foreach (var (_, location) in references)
                        await _progress.OnReferenceFoundAsync(group, symbol, location, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
