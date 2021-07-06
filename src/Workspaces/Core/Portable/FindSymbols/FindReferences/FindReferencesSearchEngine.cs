// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
    using ProjectToDocumentMap = Dictionary<Project, Dictionary<Document, HashSet<(SymbolGroup group, ISymbol symbol, IReferenceFinder finder)>>>;
    using DocumentMap = Dictionary<Document, HashSet<(SymbolGroup group, ISymbol symbol, IReferenceFinder finder)>>;
    using ProjectMap = Dictionary<Project, HashSet<(SymbolGroup group, ISymbol symbol, IReferenceFinder finder)>>;

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

                var searchSymbol = MapToAppropriateSymbol(symbol);
                var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(searchSymbol, _solution, cancellationToken).ConfigureAwait(false);
                if (sourceSymbol != null)
                    searchSymbol = sourceSymbol;

                // Determine the set of projects we actually have to walk to find results in.  If the caller provided a
                // set of documents to search, we only bother with those.
                var projects = _documents != null ? _documents.Select(d => d.Project) : _solution.Projects;
                var projectsToSearch = await DependentProjectsFinder.GetDependentProjectsAsync(_solution, searchSymbol, projects.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);

                // Keep track of the initial symbol group corresponding to search-symbol.  Any references to this group
                // will always be reported.
                var exactGroup = new HashSet<SymbolGroup>();
                await foreach (var group in DetermineExactSymbolGroupsAsync(searchSymbol, cancellationToken).ConfigureAwait(false))
                    exactGroup.Add(group);

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
                var upGroup = new HashSet<SymbolGroup>();
                await foreach (var group in DetermineUpSymbolGroupsAsync(searchSymbol, cancellationToken).ConfigureAwait(false))
                    upGroup.Add(group);

                using var _2 = ArrayBuilder<Task<(SymbolGroup group, ISymbol symbol, IReferenceFinder finder, ImmutableArray<Document> documents)>>.GetInstance(out var tasks);
                foreach (var project in projects)
                {
                    foreach (var group in exactGroup)
                    {
                        foreach (var groupSymbol in group.Symbols)
                        {
                            foreach (var finder in _finders)
                            {
                                tasks.Add(Task.Factory.StartNew(async () =>
                                {
                                    var documents = await finder.DetermineDocumentsToSearchAsync(groupSymbol, project, _documents, _options, cancellationToken).ConfigureAwait(false);
                                    return (group, groupSymbol, finder, documents);
                                }, cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
                            }
                        }
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                var projectToDocumentMap = new ProjectToDocumentMap();
                foreach (var task in tasks)
                {
                    var (group, groupSymbol, finder, documents) = await task.ConfigureAwait(false);
                    foreach (var document in documents)
                    {
                        projectToDocumentMap.GetOrAdd(document.Project, s_createDocumentMap)
                                            .MultiAdd(document, (group, symbol, finder));
                    }
                }

                //var projectMap = await CreateProjectMapAsync(symbols, cancellationToken).ConfigureAwait(false);
                //var projectToDocumentMap = await CreateProjectToDocumentMapAsync(projectMap, cancellationToken).ConfigureAwait(false);
                //ValidateProjectToDocumentMap(projectToDocumentMap);

                await ProcessAsync(exactGroup, upGroup, projectToDocumentMap, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await _progress.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessAsync(
            HashSet<SymbolGroup> exactGroup,
            HashSet<SymbolGroup> upGroup,
            ProjectToDocumentMap projectToDocumentMap,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessAsync, cancellationToken))
            {
                // quick exit
                if (projectToDocumentMap.Count == 0)
                    return;

                // Add a progress item for each (document, symbol, finder) set that we will execute.
                // We'll mark the item as completed in "ProcessDocumentAsync".
                var totalFindCount = projectToDocumentMap.Sum(kvp1 => kvp1.Value.Sum(kvp2 => kvp2.Value.Count));
                await _progressTracker.AddItemsAsync(totalFindCount, cancellationToken).ConfigureAwait(false);

                using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

                var isMatchAsync = GetIsMatchFunction(exactGroup, upGroup, cancellationToken);

                foreach (var (project, documentMap) in projectToDocumentMap)
                    tasks.Add(Task.Factory.StartNew(() => ProcessProjectAsync(project, documentMap, isMatchAsync, cancellationToken), cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        private Func<ISymbol, ValueTask<bool>> GetIsMatchFunction(
            HashSet<SymbolGroup> exactGroup,
            HashSet<SymbolGroup> upGroup,
            CancellationToken cancellationToken)
        {
            var symbolToResultMap = new Dictionary<ISymbol, AsyncLazy<bool>>();
            return async s =>
            {
                AsyncLazy<bool>? result;
                lock (symbolToResultMap)
                {
                    if (!symbolToResultMap.TryGetValue(s, out result))
                    {
                        result = new AsyncLazy<bool>(c => IsMatchAsync(exactGroup, upGroup, s, c), cacheResult: true);
                        symbolToResultMap.Add(s, result);
                    }
                }

                return await result.GetValueAsync(cancellationToken).ConfigureAwait(false);
            };
        }

        private async Task<bool> IsMatchAsync(
            HashSet<SymbolGroup> exactGroup,
            HashSet<SymbolGroup> upGroup,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            // First, if the symbol is in the exact or up groups, then we have a match.
            if (await InGroupAsync(exactGroup, symbol, cancellationToken).ConfigureAwait(false) ||
                await InGroupAsync(upGroup, symbol, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            // now if this in an inheritance scenario, see if this symbol is in the proper up/down inheritance
            // relation with a starting symbol.

            // Walk up this symbol and see if we hit the exact group.  If so, this is always a match.  However, also see
            // if we hit the up group.  If we do, it's only a match if UnidirectionalHierarchyCascade is false.

            await foreach (var upSymbol in DetermineUpSymbolsAsync(symbol, cancellationToken).ConfigureAwait(false))
            {
                if (await InGroupAsync(exactGroup, upSymbol, cancellationToken).ConfigureAwait(false))
                    return true;

                if (!_options.UnidirectionalHierarchyCascade && await InGroupAsync(upGroup, upSymbol, cancellationToken).ConfigureAwait(false))
                    return true;
            }

            return false;

            //var groups = await DetermineUpSymbolGroupsAsync(symbol, cancellationToken)

            //var project = _solution.GetOriginatingProject(symbol);
            //if (project != null)
            //{
            //    var cascaded = await InheritanceCascadeAsync(symbol, _solution, ImmutableHashSet.Create(project), direction, cancellationToken).ConfigureAwait(false);
            //    foreach (var (cascadedSymbol, _) in cascaded)
            //    {
            //        if (await IsMatchAsync(initialSymbolGroup, cascadedSymbol, direction, cancellationToken).ConfigureAwait(false))
            //            return true;
            //    }
            //}

            //return false;
        }

        private async ValueTask<bool> InGroupAsync(HashSet<SymbolGroup> groupSet, ISymbol symbol, CancellationToken cancellationToken)
        {
            foreach (var group in groupSet)
            {
                foreach (var groupSymbol in group.Symbols)
                {
                    if (await SymbolFinder.OriginalSymbolsMatchAsync(_solution, groupSymbol, symbol, cancellationToken).ConfigureAwait(false))
                        return true;
                }
            }

            return false;
        }

        internal async IAsyncEnumerable<ISymbol> DetermineUpSymbolsAsync(
            ISymbol symbol, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_options.Cascade &&
                symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event &&
                symbol.ContainingType.TypeKind is not TypeKind.Interface)
            {
                // We have a normal method.  Find any interface methods up the inheritance hierarchy that it implicitly
                // or explicitly implements and cascade to those.
                foreach (var match in await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(symbol, _solution, projects: null, includeDerivedTypes: false, cancellationToken).ConfigureAwait(false))
                    yield return match;

                if (symbol.GetOverriddenMember() is { } overriddenMember)
                    yield return overriddenMember;
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
            var set = new HashSet<(SymbolGroup group, ISymbol symbol, IReferenceFinder finder)>();

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

        private ValueTask HandleLocationAsync(SymbolGroup group, ISymbol symbol, ReferenceLocation location, CancellationToken cancellationToken)
            => _progress.OnReferenceFoundAsync(group, symbol, location, cancellationToken);
    }
}
