// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    using ProjectToDocumentMap = Dictionary<Project, Dictionary<Document, HashSet<(SymbolGroup group, ISymbol symbol, IReferenceFinder finder)>>>;

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

                var group = await DetermineSymbolGroupAsync(searchSymbol, cancellationToken).ConfigureAwait(false);

                // For the starting symbol, always cascade up and down the inheritance hierarchy.
                var symbols = await DetermineInitialSymbolsAsync(
                    symbol, FindReferencesCascadeDirection.UpAndDown, cancellationToken).ConfigureAwait(false);

                var projectMap = await CreateProjectMapAsync(symbols, cancellationToken).ConfigureAwait(false);
                var projectToDocumentMap = await CreateProjectToDocumentMapAsync(projectMap, cancellationToken).ConfigureAwait(false);
                ValidateProjectToDocumentMap(projectToDocumentMap);

                await ProcessAsync(group, projectToDocumentMap, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await _progress.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessAsync(
            SymbolGroup initialSymbolGroup,
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
                var totalFindCount = projectToDocumentMap.Sum(
                    kvp1 => kvp1.Value.Sum(kvp2 => kvp2.Value.Count));
                await _progressTracker.AddItemsAsync(totalFindCount, cancellationToken).ConfigureAwait(false);

                using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

                var isMatchAsync = GetIsMatchFunction(initialSymbolGroup, cancellationToken);

                foreach (var (project, documentMap) in projectToDocumentMap)
                    tasks.Add(Task.Factory.StartNew(() => ProcessProjectAsync(project, documentMap, isMatchAsync, cancellationToken), cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        private Func<ISymbol, ValueTask<bool>> GetIsMatchFunction(SymbolGroup initialSymbolGroup, CancellationToken cancellationToken)
        {
            var symbolToResultMap = new Dictionary<ISymbol, AsyncLazy<bool>>();
            return async s =>
            {
                AsyncLazy<bool>? result;
                lock (symbolToResultMap)
                {
                    if (!symbolToResultMap.TryGetValue(s, out result))
                    {
                        result = new AsyncLazy<bool>(c => IsMatchAsync(initialSymbolGroup, s, c), true);
                        symbolToResultMap.Add(s, result);
                    }
                }

                return await result.GetValueAsync(cancellationToken).ConfigureAwait(false);
            };
        }

        private async Task<bool> IsMatchAsync(SymbolGroup initialSymbolGroup, ISymbol symbol, CancellationToken cancellationToken)
        {
            return await IsMatchAsync(initialSymbolGroup, symbol, FindReferencesCascadeDirection.Up, cancellationToken).ConfigureAwait(false) ||
                   await IsMatchAsync(initialSymbolGroup, symbol, FindReferencesCascadeDirection.Down, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> IsMatchAsync(SymbolGroup initialSymbolGroup, ISymbol symbol, FindReferencesCascadeDirection direction, CancellationToken cancellationToken)
        {
            // First, directly check if the symbol matches any of the initial set.
            foreach (var initialSymbol in initialSymbolGroup.Symbols)
            {
                if (await SymbolFinder.OriginalSymbolsMatchAsync(_solution, initialSymbol, symbol, cancellationToken).ConfigureAwait(false))
                    return true;
            }

            // now if this in an inheritance scenario, see if this symbol is in the proper up/down inheritance
            // relation with a starting symbol.
            if (symbol.IsStatic)
                return false;

            if (symbol.Kind is not SymbolKind.Method and not SymbolKind.Property and not SymbolKind.Event)
                return false;

            var project = _solution.GetOriginatingProject(symbol);
            if (project != null)
            {
                var cascaded = await InheritanceCascadeAsync(symbol, _solution, ImmutableHashSet.Create(project), direction, cancellationToken).ConfigureAwait(false);
                foreach (var (cascadedSymbol, _) in cascaded)
                {
                    if (await IsMatchAsync(initialSymbolGroup, cascadedSymbol, direction, cancellationToken).ConfigureAwait(false))
                        return true;
                }
            }

            return false;
        }

        internal static async Task<ImmutableArray<(ISymbol symbol, FindReferencesCascadeDirection cascadeDirection)>> InheritanceCascadeAsync(
            ISymbol symbol,
            Solution solution,
            ImmutableHashSet<Project>? projects,
            FindReferencesCascadeDirection cascadeDirection,
            CancellationToken cancellationToken)
        {
            if (symbol.IsImplementableMember())
            {
                // We have an interface method.  Walk down the inheritance hierarchy and find all implementations of
                // that method and cascade to them.
                var result = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Down)
                    ? await SymbolFinder.FindMemberImplementationsArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
                    : ImmutableArray<ISymbol>.Empty;
                return result.SelectAsArray(s => (s, FindReferencesCascadeDirection.Down));
            }
            else
            {
                // We have a normal method.  Find any interface methods up the inheritance hierarchy that it implicitly
                // or explicitly implements and cascade to those.
                var interfaceMembersImplemented = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Up)
                    ? await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
                    : ImmutableArray<ISymbol>.Empty;

                // Finally, methods can cascade through virtual/override inheritance.  NOTE(cyrusn):
                // We only need to go up or down one level.  Then, when we're finding references on
                // those members, we'll end up traversing the entire hierarchy.
                var overrides = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Down)
                    ? await SymbolFinder.FindOverridesArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false)
                    : ImmutableArray<ISymbol>.Empty;

                var overriddenMember = cascadeDirection.HasFlag(FindReferencesCascadeDirection.Up)
                    ? symbol.GetOverriddenMember()
                    : null;

                var interfaceMembersImplementedWithDirection = interfaceMembersImplemented.SelectAsArray(s => (s, FindReferencesCascadeDirection.Up));
                var overridesWithDirection = overrides.SelectAsArray(s => (s, FindReferencesCascadeDirection.Down));
                var overriddenMemberWithDirection = (overriddenMember!, FindReferencesCascadeDirection.Up);

                return overriddenMember == null
                    ? interfaceMembersImplementedWithDirection.Concat(overridesWithDirection)
                    : interfaceMembersImplementedWithDirection.Concat(overridesWithDirection).Concat(overriddenMemberWithDirection);
            }
        }

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
