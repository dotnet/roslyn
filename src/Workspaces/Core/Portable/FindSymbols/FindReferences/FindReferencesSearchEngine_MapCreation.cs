// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using DocumentMap = Dictionary<Document, HashSet<ISymbol>>;
    using ProjectMap = Dictionary<Project, HashSet<ISymbol>>;
    using ProjectToDocumentMap = Dictionary<Project, Dictionary<Document, HashSet<ISymbol>>>;

    internal partial class FindReferencesSearchEngine
    {
        private static readonly Func<Project, DocumentMap> s_createDocumentMap = _ => new DocumentMap();

        private async Task<ProjectToDocumentMap> CreateProjectToDocumentMapAsync(ProjectMap projectMap, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateDocumentMapAsync, cancellationToken))
            {
                using var _ = ArrayBuilder<Task<(ImmutableArray<Document>, ISymbol)>>.GetInstance(out var tasks);

                foreach (var (project, projectQueue) in projectMap)
                {
                    foreach (var symbol in projectQueue)
                    {
                        tasks.Add(Task.Factory.StartNew(() =>
                            DetermineDocumentsToSearchAsync(project, symbol, cancellationToken), cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
                    }
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);

                var finalMap = new ProjectToDocumentMap();
                foreach (var (documents, symbol) in results)
                {
                    foreach (var document in documents)
                    {
                        finalMap.GetOrAdd(document.Project, s_createDocumentMap)
                                .MultiAdd(document, symbol);
                    }
                }

#if DEBUG
                foreach (var (project, documentMap) in finalMap)
                {
                    Contract.ThrowIfTrue(documentMap.Any(kvp1 => kvp1.Value.Count != kvp1.Value.ToSet().Count));
                }
#endif

                return finalMap;
            }
        }

        private async Task<(ImmutableArray<Document>, ISymbol)> DetermineDocumentsToSearchAsync(
            Project project, ISymbol symbol, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            foreach (var finder in _finders)
            {
                var documents = await finder.DetermineDocumentsToSearchAsync(
                    symbol, project, _documents, _options, cancellationToken).ConfigureAwait(false);

                foreach (var document in documents)
                {
                    if (_documents == null || _documents.Contains(document))
                        result.Add(document);
                }
            }

            result.RemoveDuplicates();
            return (result.ToImmutable(), symbol);
        }

        //private async Task<ProjectMap> CreateProjectMapAsync(ConcurrentSet<SymbolGroup> symbolGroups, CancellationToken cancellationToken)
        //{
        //    using (Logger.LogBlock(FunctionId.FindReference_CreateProjectMapAsync, cancellationToken))
        //    {
        //        var projectMap = new ProjectMap();

        //        var scope = _documents?.Select(d => d.Project).ToImmutableHashSet();
        //        foreach (var symbolGroup in symbolGroups)
        //        {
        //            foreach (var symbol in symbolGroup.Symbols)
        //            {
        //                foreach (var finder in _finders)
        //                {
        //                    cancellationToken.ThrowIfCancellationRequested();

        //                    var projects = await finder.DetermineProjectsToSearchAsync(symbol, _solution, scope, cancellationToken).ConfigureAwait(false);
        //                    foreach (var project in projects.Distinct().WhereNotNull())
        //                    {
        //                        if (scope == null || scope.Contains(project))
        //                            projectMap.MultiAdd(project, (symbolGroup, symbol, finder));
        //                    }
        //                }
        //            }
        //        }

        //        Contract.ThrowIfTrue(projectMap.Any(kvp => kvp.Value.Count != kvp.Value.ToSet().Count));
        //        return projectMap;
        //    }
        //}

        //private async Task<ConcurrentSet<SymbolGroup>> DetermineInitialSymbolsAsync(
        //    ISymbol symbol, FindReferencesCascadeDirection cascadeDirection, CancellationToken cancellationToken)
        //{
        //    using (Logger.LogBlock(FunctionId.FindReference_DetermineAllSymbolsAsync, cancellationToken))
        //    {
        //        var result = new ConcurrentSet<SymbolGroup>();
        //        await DetermineInitialSymbolsCoreAsync(symbol, cascadeDirection, result, cancellationToken).ConfigureAwait(false);
        //        return result;
        //    }
        //}

        //private async Task DetermineInitialSymbolsCoreAsync(
        //    ISymbol symbol, FindReferencesCascadeDirection cascadeDirection,
        //    ConcurrentSet<SymbolGroup> result, CancellationToken cancellationToken)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();

        //    var searchSymbol = MapToAppropriateSymbol(symbol);

        //    // 2) Try to map this back to source symbol if this was a metadata symbol.
        //    var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(searchSymbol, _solution, cancellationToken).ConfigureAwait(false);
        //    if (sourceSymbol != null)
        //        searchSymbol = sourceSymbol;

        //    Contract.ThrowIfNull(searchSymbol);

        //    var group = await DetermineSymbolGroupAsync(searchSymbol, cancellationToken).ConfigureAwait(false);
        //    if (result.Add(group))
        //    {
        //        await _progress.OnDefinitionFoundAsync(group, cancellationToken).ConfigureAwait(false);

        //        // get project to search
        //        var symbolProject = _solution.GetProject(searchSymbol.ContainingAssembly, cancellationToken);

        //        cancellationToken.ThrowIfCancellationRequested();

        //        if (symbolProject != null)
        //        {
        //            using var _1 = ArrayBuilder<Task>.GetInstance(out var finderTasks);
        //            using var _2 = ArrayBuilder<Task>.GetInstance(out var symbolTasks);
        //            foreach (var f in _finders)
        //            {
        //                finderTasks.Add(Task.Factory.StartNew(async () =>
        //                {
        //                    using var _ = ArrayBuilder<Task>.GetInstance(out var symbolTasks);

        //                    var symbols = await f.DetermineCascadedSymbolsAsync(
        //                        searchSymbol, symbolProject, _options, cascadeDirection, cancellationToken).ConfigureAwait(false);
        //                    AddSymbolTasks(result, symbols, symbolTasks, cancellationToken);

        //                    cancellationToken.ThrowIfCancellationRequested();

        //                    await Task.WhenAll(symbolTasks).ConfigureAwait(false);
        //                }, cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
        //            }

        //            // Defer to the language to see if it wants to cascade here in some special way.
        //            finderTasks.Add(Task.Factory.StartNew(async () =>
        //            {
        //                if (symbolProject.LanguageServices.GetService<ILanguageServiceReferenceFinder>() is { } service)
        //                {
        //                    var symbols = await service.DetermineCascadedSymbolsAsync(
        //                        searchSymbol, symbolProject, cascadeDirection, cancellationToken).ConfigureAwait(false);
        //                    AddSymbolTasks(result, symbols, symbolTasks, cancellationToken);
        //                }
        //            }, cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());

        //            await Task.WhenAll(finderTasks).ConfigureAwait(false);
        //            await Task.WhenAll(symbolTasks).ConfigureAwait(false);
        //        }
        //    }
        //}

        private async Task<SymbolGroup> GetSymbolGroupAsync(ISymbol currentSymbol, CancellationToken cancellationToken)
        {
            var symbols = await SymbolFinder.FindLinkedSymbolsAsync(currentSymbol, _solution, cancellationToken).ConfigureAwait(false);
            var group = new SymbolGroup(symbols);
            return group;
        }

        //private async IAsyncEnumerable<SymbolGroup> DetermineUpSymbolGroupsAsync(
        //    ISymbol symbol, [EnumeratorCancellation] CancellationToken cancellationToken)
        //{
        //    await foreach (var upSymbol in DetermineUpSymbolsAsync(symbol, cancellationToken).ConfigureAwait(false))
        //    {
        //        var group = await GetSymbolGroupAsync(upSymbol, cancellationToken).ConfigureAwait(false);
        //        yield return group;

        //        await foreach (var cascade in DetermineCascadedSymbolsAsync(upSymbol, cancellationToken).ConfigureAwait(false))
        //        {
        //            group = await GetSymbolGroupAsync(cascade, cancellationToken).ConfigureAwait(false);
        //            yield return group;
        //        }
        //    }
        //}

        //private async Task<HashSet<SymbolGroup>> DetermineUpGroupAsync(ISymbol searchSymbol, CancellationToken cancellationToken)
        //{
        //    // If we're not cascading, then we only find references to the exact original symbol, not any symbols up
        //    // its inheritance hierarchy.
        //    if (!_options.Cascade)
        //        return new HashSet<SymbolGroup>();

        //    var symbolOrigination = DependentProjectsFinder.GetSymbolOrigination(_solution, searchSymbol, cancellationToken);
        //    if (symbolOrigination.sourceProject == null)

        //    var upSymbols = await InheritanceCascadeAsync(symbol, _solution, )
        //    return new SymbolGroup(
        //        await SymbolFinder.FindLinkedSymbolsAsync(searchSymbol, _solution, cancellationToken).ConfigureAwait(false));
        //}

        //private void AddSymbolTasks(
        //    ConcurrentSet<SymbolGroup> result,
        //    ImmutableArray<(ISymbol symbol, FindReferencesCascadeDirection cascadeDirection)> symbols,
        //    ArrayBuilder<Task> symbolTasks,
        //    CancellationToken cancellationToken)
        //{
        //    if (!symbols.IsDefault)
        //    {
        //        foreach (var (symbol, cascadeDirection) in symbols)
        //        {
        //            Contract.ThrowIfNull(symbol);

        //            // If we're cascading unidirectionally, then keep going in the direction this symbol was found in.
        //            // Otherwise, if we're not unidirectional, then continue to cascade in both directions with this
        //            // symbol.
        //            var finalDirection = _options.UnidirectionalHierarchyCascade
        //                ? cascadeDirection
        //                : FindReferencesCascadeDirection.UpAndDown;
        //            lock (symbolTasks)
        //            {
        //                symbolTasks.Add(Task.Factory.StartNew(
        //                    () => DetermineInitialSymbolsCoreAsync(symbol, finalDirection, result, cancellationToken), cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
        //            }
        //        }
        //    }
        //}
    }
}
