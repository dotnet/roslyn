// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using ProjectToDocumentMap = Dictionary<Project, Dictionary<Document, HashSet<ISymbol>>>;

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

                var symbolSet = await SymbolSet.CreateAsync(this, symbol, cancellationToken).ConfigureAwait(false);

                // Determine the set of projects we actually have to walk to find results in.  If the caller provided a
                // set of documents to search, we only bother with those.
                var projectsToSearch = await GetProjectsToSearchAsync(symbolSet.GetAllSymbols(), cancellationToken).ConfigureAwait(false);
                if (projectsToSearch.Count == 0)
                    return;

                var dependencyGraph = _solution.GetProjectDependencyGraph();
                await _progressTracker.AddItemsAsync(projectsToSearch.Count, cancellationToken).ConfigureAwait(false);

                using var _1 = ArrayBuilder<Task>.GetInstance(out var projectTasks);

                foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
                {
                    var currentProject = _solution.GetRequiredProject(projectId);
                    if (!projectsToSearch.Contains(currentProject))
                        continue;

                    // As we walk each project, attempt to grow the search set appropriately up and down the 
                    // inheritance hierarchy.
                    await symbolSet.InheritanceCascadeAsync(currentProject, cancellationToken).ConfigureAwait(false);

                    var allSymbols = symbolSet.GetAllSymbols();
                    await ReportGroupsAsync(allSymbols, cancellationToken).ConfigureAwait(false);

                    projectTasks.Add(Task.Factory.StartNew(async () =>
                    {
                        await ProcessProjectAsync(currentProject, allSymbols, cancellationToken).ConfigureAwait(false);
                    }, cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
                }

                await Task.WhenAll(projectTasks).ConfigureAwait(false);

                //var symbolOrigination = DependentProjectsFinder.GetSymbolOrigination(_solution, searchSymbol, cancellationToken);
                //var initialProject = symbolOrigination.sourceProject ?? projectsToSearch.First();



                //var downSymbols = await DetermineDownSymbolsAsync(searchSymbol, cancellationToken).ConfigureAwait(false);

                // Determine the set of symbols higher up in the search-symbol's inheritance hierarchy.  References to
                // these groups will always be reported.  However any reference to a subtype of these (that is not a
                // subtype of the exact group) will only be reported if UnidirectionalHierarchyCascade is false.  In
                // other words, if you have the following hierarchy:
                //
                //      A
                //     / \
                //    B   C
                //   / \
                //  D   E
                //
                // And 'B' is the search symbol.  Then 'A' will be in the 'up group'.  References to 'A' or 'B' will
                // always be reported.  Subtypes of 'A' will be reported depending on the value of
                // UnidirectionalHierarchyCascade.  If UnidirectionalHierarchyCascade is false then all subtypes will be
                // reported (i.e. D, E, and C).  If UnidirectionalHierarchyCascade is true, then only 'B' and 'D' will
                // be reported (C will not be).

                //using var _2 = ArrayBuilder<Task<(ISymbol symbol, ImmutableArray<Document> documents)>>.GetInstance(out var tasks);
                //foreach (var project in projects)
                //{
                //    foreach (var exactSymbol in exactSymbols)
                //    {
                //        foreach (var finder in _finders)
                //        {
                //            tasks.Add(Task.Factory.StartNew(async () =>
                //            {
                //                var documents = await finder.DetermineDocumentsToSearchAsync(exactSymbol, project, _documents, _options, cancellationToken).ConfigureAwait(false);
                //                return (exactSymbol, documents);
                //            }, cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
                //        }
                //    }
                //}

                //await Task.WhenAll(tasks).ConfigureAwait(false);

                //var projectToDocumentMap = new ProjectToDocumentMap();
                //foreach (var task in tasks)
                //{
                //    var (groupSymbol, documents) = await task.ConfigureAwait(false);
                //    foreach (var document in documents)
                //    {
                //        projectToDocumentMap.GetOrAdd(document.Project, s_createDocumentMap)
                //                            .MultiAdd(document, groupSymbol);
                //    }
                //}

                ////var projectMap = await CreateProjectMapAsync(symbols, cancellationToken).ConfigureAwait(false);
                ////var projectToDocumentMap = await CreateProjectToDocumentMapAsync(projectMap, cancellationToken).ConfigureAwait(false);
                ////ValidateProjectToDocumentMap(projectToDocumentMap);

                //await ProcessAsync(exactGroup, upGroup, projectToDocumentMap, cancellationToken).ConfigureAwait(false);
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
                    var group = await GetSymbolGroupAsync(symbol, cancellationToken).ConfigureAwait(false);
                    foreach (var groupSymbol in group.Symbols)
                        Contract.ThrowIfFalse(_symbolToGroup.TryAdd(groupSymbol, group));

                    await _progress.OnDefinitionFoundAsync(group, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        private async Task<SymbolGroup> GetSymbolGroupAsync(ISymbol currentSymbol, CancellationToken cancellationToken)
        {
            var symbols = await SymbolFinder.FindLinkedSymbolsAsync(currentSymbol, _solution, cancellationToken).ConfigureAwait(false);
            var group = new SymbolGroup(symbols);
            return group;
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
                    await ProcessDocumentQueueAsync(document, allSymbols, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await _progressTracker.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        //internal static async Task<ImmutableArray<(ISymbol symbol, FindReferencesCascadeDirection cascadeDirection)>> InheritanceCascadeAsync(
        //    ISymbol symbol,
        //    Solution solution,
        //    ImmutableHashSet<Project>? projects,
        //    FindReferencesCascadeDirection cascadeDirection,
        //    CancellationToken cancellationToken)
        //{
        //    if (symbol.IsImplementableMember())
        //    {
        //        // We have an interface method.  Walk down the inheritance hierarchy and find all implementations of
        //        // that method and cascade to them.
        //        var result = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Down)
        //            ? await SymbolFinder.FindMemberImplementationsArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
        //            : ImmutableArray<ISymbol>.Empty;
        //        return result.SelectAsArray(s => (s, FindReferencesCascadeDirection.Down));
        //    }
        //    else
        //    {
        //        // We have a normal method.  Find any interface methods up the inheritance hierarchy that it implicitly
        //        // or explicitly implements and cascade to those.
        //        var interfaceMembersImplemented = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Up)
        //            ? await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
        //            : ImmutableArray<ISymbol>.Empty;

        //        // Finally, methods can cascade through virtual/override inheritance.  NOTE(cyrusn):
        //        // We only need to go up or down one level.  Then, when we're finding references on
        //        // those members, we'll end up traversing the entire hierarchy.
        //        var overrides = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Down)
        //            ? await SymbolFinder.FindOverridesArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
        //            : ImmutableArray<ISymbol>.Empty;

        //        var overriddenMember = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Up)
        //            ? symbol.GetOverriddenMember()
        //            : null;

        //        var interfaceMembersImplementedWithDirection = interfaceMembersImplemented.SelectAsArray(s => (s, FindReferencesCascadeDirection.Up));
        //        var overridesWithDirection = overrides.SelectAsArray(s => (s, FindReferencesCascadeDirection.Down));
        //        var overriddenMemberWithDirection = (overriddenMember!, FindReferencesCascadeDirection.Up);

        //        return overriddenMember == null
        //            ? interfaceMembersImplementedWithDirection.Concat(overridesWithDirection)
        //            : interfaceMembersImplementedWithDirection.Concat(overridesWithDirection).Concat(overriddenMemberWithDirection);
        //    }
        //}

        //internal static async Task<ImmutableArray<(ISymbol symbol, FindReferencesCascadeDirection cascadeDirection)>> InheritanceCascadeAsync(
        //    ISymbol symbol,
        //    Solution solution,
        //    ImmutableHashSet<Project>? projects,
        //    FindReferencesCascadeDirection cascadeDirection,
        //    CancellationToken cancellationToken)
        //{
        //    if (symbol.IsImplementableMember())
        //    {
        //        // We have an interface method.  Walk down the inheritance hierarchy and find all implementations of
        //        // that method and cascade to them.
        //        var result = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Down)
        //            ? await SymbolFinder.FindMemberImplementationsArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
        //            : ImmutableArray<ISymbol>.Empty;
        //        return result.SelectAsArray(s => (s, FindReferencesCascadeDirection.Down));
        //    }
        //    else
        //    {
        //        // We have a normal method.  Find any interface methods up the inheritance hierarchy that it implicitly
        //        // or explicitly implements and cascade to those.
        //        var interfaceMembersImplemented = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Up)
        //            ? await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
        //            : ImmutableArray<ISymbol>.Empty;

        //        // Finally, methods can cascade through virtual/override inheritance.  NOTE(cyrusn):
        //        // We only need to go up or down one level.  Then, when we're finding references on
        //        // those members, we'll end up traversing the entire hierarchy.
        //        var overrides = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Down)
        //            ? await SymbolFinder.FindOverridesArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
        //            : ImmutableArray<ISymbol>.Empty;

        //        var overriddenMember = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Up)
        //            ? symbol.GetOverriddenMember()
        //            : null;

        //        var interfaceMembersImplementedWithDirection = interfaceMembersImplemented.SelectAsArray(s => (s, FindReferencesCascadeDirection.Up));
        //        var overridesWithDirection = overrides.SelectAsArray(s => (s, FindReferencesCascadeDirection.Down));
        //        var overriddenMemberWithDirection = (overriddenMember!, FindReferencesCascadeDirection.Up);

        //        return overriddenMember == null
        //            ? interfaceMembersImplementedWithDirection.Concat(overridesWithDirection)
        //            : interfaceMembersImplementedWithDirection.Concat(overridesWithDirection).Concat(overriddenMemberWithDirection);
        //    }
        //}

        [Conditional("DEBUG")]
        private static void ValidateProjectToDocumentMap(
            ProjectToDocumentMap projectToDocumentMap)
        {
            var set = new HashSet<ISymbol>();

            foreach (var documentMap in projectToDocumentMap.Values)
            {
                foreach (var documentToFinderList in documentMap)
                {
                    set.Clear();

                    foreach (var tuple in documentToFinderList.Value)
                        Debug.Assert(set.Add(tuple));
                }
            }
        }
    }
}
