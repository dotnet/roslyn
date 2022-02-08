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

        /// <summary>
        /// Mapping from symbols (unified across metadata/retargeting) and the set of symbols that was produced for 
        /// them in the case of linked files across projects.  This allows references to be found to any of the unified
        /// symbols, while the user only gets a single reported group back that corresponds to that entire set.
        /// </summary>
        private readonly ConcurrentDictionary<ISymbol, SymbolGroup> _symbolToGroup = new(MetadataUnifyingEquivalenceComparer.Instance);

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
                var disposable = await _progressTracker.AddSingleItemAsync(cancellationToken).ConfigureAwait(false);
                await using var _ = disposable.ConfigureAwait(false);

                // Create the initial set of symbols to search for.  As we walk the appropriate projects in the solution
                // we'll expand this set as we dicover new symbols to search for in each project.
                var symbolSet = await SymbolSet.CreateAsync(this, symbol, cancellationToken).ConfigureAwait(false);

                // Report the initial set of symbols to the caller.
                var allSymbols = symbolSet.GetAllSymbols();
                await ReportGroupsAsync(allSymbols, cancellationToken).ConfigureAwait(false);

                // Determine the set of projects we actually have to walk to find results in.  If the caller provided a
                // set of documents to search, we only bother with those.
                var projectsToSearch = await GetProjectIdsToSearchAsync(allSymbols, cancellationToken).ConfigureAwait(false);

                // We need to process projects in order when updating our symbol set.  Say we have three projects (A, B
                // and C), we cannot necessarily find inherited symbols in C until we have searched B.  Importantly,
                // while we're processing each project linearly to update the symbol set we're searching for, we still
                // then process the projects in parallel once we know the set of symbols we're searching for in that
                // project.
                var dependencyGraph = _solution.GetProjectDependencyGraph();
                await _progressTracker.AddItemsAsync(projectsToSearch.Count, cancellationToken).ConfigureAwait(false);

                using var _1 = ArrayBuilder<Task>.GetInstance(out var tasks);

                foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
                {
                    if (!projectsToSearch.Contains(projectId))
                        continue;

                    var currentProject = _solution.GetRequiredProject(projectId);

                    // As we walk each project, attempt to grow the search set appropriately up and down the inheritance
                    // hierarchy and grab a copy of the symbols to be processed.  Note: this has to happen serially
                    // which is why we do it in this loop and not inside the concurrent project processing that happens
                    // below.
                    await symbolSet.InheritanceCascadeAsync(currentProject, cancellationToken).ConfigureAwait(false);
                    allSymbols = symbolSet.GetAllSymbols();

                    // Report any new symbols we've cascaded to to our caller.
                    await ReportGroupsAsync(allSymbols, cancellationToken).ConfigureAwait(false);

                    tasks.Add(CreateWorkAsync(() => ProcessProjectAsync(currentProject, allSymbols, cancellationToken), cancellationToken));
                }

                // Now, wait for all projects to complete.
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                await _progress.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public Task CreateWorkAsync(Func<Task> createWorkAsync, CancellationToken cancellationToken)
            => Task.Factory.StartNew(createWorkAsync, cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap();

        /// <summary>
        /// Notify the caller of the engine about the definitions we've found that we're looking for.  We'll only notify
        /// them once per symbol group, but we may have to notify about new symbols each time we expand our symbol set
        /// when we walk into a new project.
        /// </summary>
        private async Task ReportGroupsAsync(ImmutableArray<ISymbol> symbols, CancellationToken cancellationToken)
        {
            foreach (var symbol in symbols)
            {
                // See if this is the first time we're running across this symbol.  Note: no locks are needed
                // here betwen checking and then adding because this is only ever called serially from within
                // FindReferencesAsync above (though we still need a ConcurrentDictionary as reads of these 
                // symbols will happen later in ProcessDocumentAsync.  However, those reads will only happen
                // after the dependent symbol values were written in, so it will be safe to blindly read them
                // out.
                if (!_symbolToGroup.ContainsKey(symbol))
                {
                    var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(symbol, _solution, cancellationToken).ConfigureAwait(false);
                    var group = new SymbolGroup(linkedSymbols);

                    foreach (var groupSymbol in group.Symbols)
                        _symbolToGroup.TryAdd(groupSymbol, group);

                    await _progress.OnDefinitionFoundAsync(group, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<HashSet<ProjectId>> GetProjectIdsToSearchAsync(
            ImmutableArray<ISymbol> symbols, CancellationToken cancellationToken)
        {
            var projects = _documents != null
                ? _documents.Select(d => d.Project).ToImmutableHashSet()
                : _solution.Projects.ToImmutableHashSet();

            var result = new HashSet<ProjectId>();

            foreach (var symbol in symbols)
            {
                var dependentProjects = await DependentProjectsFinder.GetDependentProjectsAsync(
                    _solution, symbol, projects, cancellationToken).ConfigureAwait(false);
                foreach (var project in dependentProjects)
                    result.Add(project.Id);
            }

            return result;
        }

        private async Task ProcessProjectAsync(Project project, ImmutableArray<ISymbol> allSymbols, CancellationToken cancellationToken)
        {
            using var _1 = PooledDictionary<ISymbol, PooledHashSet<string>>.GetInstance(out var symbolToGlobalAliases);
            using var _2 = PooledDictionary<Document, PooledHashSet<ISymbol>>.GetInstance(out var documentToSymbols);
            try
            {
                foreach (var symbol in allSymbols)
                {
                    foreach (var finder in _finders)
                    {
                        var aliases = await finder.DetermineGlobalAliasesAsync(
                            symbol, project, cancellationToken).ConfigureAwait(false);
                        if (aliases.Length > 0)
                        {
                            var globalAliases = Get(symbolToGlobalAliases, symbol);
                            globalAliases.AddRange(aliases);
                        }
                    }
                }

                foreach (var symbol in allSymbols)
                {
                    var globalAliases = TryGet(symbolToGlobalAliases, symbol);

                    foreach (var finder in _finders)
                    {
                        var documents = await finder.DetermineDocumentsToSearchAsync(
                            symbol, globalAliases, project, _documents, _options, cancellationToken).ConfigureAwait(false);

                        foreach (var document in documents)
                        {
                            var docSymbols = Get(documentToSymbols, document);
                            docSymbols.Add(symbol);
                        }
                    }
                }

                using var _3 = ArrayBuilder<Task>.GetInstance(out var tasks);
                foreach (var (document, docSymbols) in documentToSymbols)
                {
                    tasks.Add(CreateWorkAsync(() => ProcessDocumentAsync(
                        document, docSymbols, symbolToGlobalAliases, cancellationToken), cancellationToken));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                foreach (var (_, symbols) in documentToSymbols)
                    symbols.Free();

                foreach (var (_, globalAliases) in symbolToGlobalAliases)
                    globalAliases.Free();

                await _progressTracker.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
            }

            static PooledHashSet<U> Get<T, U>(PooledDictionary<T, PooledHashSet<U>> dictionary, T key) where T : notnull
            {
                if (!dictionary.TryGetValue(key, out var set))
                {
                    set = PooledHashSet<U>.GetInstance();
                    dictionary.Add(key, set);
                }

                return set;
            }
        }

        private static PooledHashSet<U>? TryGet<T, U>(Dictionary<T, PooledHashSet<U>> dictionary, T key) where T : notnull
            => dictionary.TryGetValue(key, out var set) ? set : null;

        private async Task ProcessDocumentAsync(
            Document document, HashSet<ISymbol> symbols,
            Dictionary<ISymbol, PooledHashSet<string>> symbolToGlobalAliases,
            CancellationToken cancellationToken)
        {
            await _progress.OnFindInDocumentStartedAsync(document, cancellationToken).ConfigureAwait(false);

            SemanticModel? model = null;
            try
            {
                model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // start cache for this semantic model
                FindReferenceCache.Start(model);

                foreach (var symbol in symbols)
                {
                    var globalAliases = TryGet(symbolToGlobalAliases, symbol);
                    await ProcessDocumentAsync(document, model, symbol, globalAliases, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                FindReferenceCache.Stop(model);

                await _progress.OnFindInDocumentCompletedAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessDocumentAsync(
            Document document, SemanticModel semanticModel, ISymbol symbol,
            HashSet<string>? globalAliases, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessDocumentAsync, cancellationToken))
            {
                // This is safe to just blindly read. We can only ever get here after the call to ReportGroupsAsync
                // happened.  So tehre must be a group for this symbol in our map.
                var group = _symbolToGroup[symbol];
                foreach (var finder in _finders)
                {
                    var references = await finder.FindReferencesInDocumentAsync(
                        symbol, globalAliases, document, semanticModel, _options, cancellationToken).ConfigureAwait(false);
                    foreach (var (_, location) in references)
                        await _progress.OnReferenceFoundAsync(group, symbol, location, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
