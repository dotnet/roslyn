// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        private async Task<ProjectMap> CreateProjectMapAsync(ConcurrentSet<SymbolGroup> symbolGroups, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateProjectMapAsync, cancellationToken))
            {
                var projectMap = new ProjectMap();

                var scope = _documents?.Select(d => d.Project).ToImmutableHashSet();
                foreach (var symbolGroup in symbolGroups)
                {
                    foreach (var symbol in symbolGroup.Symbols)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var projects = await DependentProjectsFinder.GetDependentProjectsAsync(
                            _solution, symbol, scope, cancellationToken).ConfigureAwait(false);

                        foreach (var project in projects.Distinct().WhereNotNull())
                        {
                            if (scope == null || scope.Contains(project))
                                projectMap.MultiAdd(project, symbol);
                        }
                    }
                }

                Contract.ThrowIfTrue(projectMap.Any(kvp => kvp.Value.Count != kvp.Value.ToSet().Count));
                return projectMap;
            }
        }

        private async Task<ConcurrentSet<SymbolGroup>> DetermineAllSymbolsAsync(
            ISymbol symbol, FindReferencesCascadeDirection cascadeDirection, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_DetermineAllSymbolsAsync, cancellationToken))
            {
                var result = new ConcurrentSet<SymbolGroup>();
                await DetermineAllSymbolsCoreAsync(symbol, cascadeDirection, result, cancellationToken).ConfigureAwait(false);
                return result;
            }
        }

        private async Task DetermineAllSymbolsCoreAsync(
            ISymbol symbol, FindReferencesCascadeDirection cascadeDirection,
            ConcurrentSet<SymbolGroup> result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchSymbol = MapToAppropriateSymbol(symbol);

            // 2) Try to map this back to source symbol if this was a metadata symbol.
            var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(searchSymbol, _solution, cancellationToken).ConfigureAwait(false);
            if (sourceSymbol != null)
                searchSymbol = sourceSymbol;

            Contract.ThrowIfNull(searchSymbol);

            var group = await DetermineSymbolGroupAsync(searchSymbol, cancellationToken).ConfigureAwait(false);
            if (result.Add(group))
            {
                await _progress.OnDefinitionFoundAsync(group, cancellationToken).ConfigureAwait(false);

                // get project to search
                var projects = GetProjectScope();

                cancellationToken.ThrowIfCancellationRequested();

                using var _ = ArrayBuilder<Task>.GetInstance(out var finderTasks);
                foreach (var f in _finders)
                {
                    finderTasks.Add(Task.Factory.StartNew(async () =>
                    {
                        using var _ = ArrayBuilder<Task>.GetInstance(out var symbolTasks);

                        var symbols = await f.DetermineCascadedSymbolsAsync(
                            searchSymbol, _solution, projects, _options, cascadeDirection, cancellationToken).ConfigureAwait(false);
                        AddSymbolTasks(result, symbols, symbolTasks, cancellationToken);

                        // Defer to the language to see if it wants to cascade here in some special way.
                        var symbolProject = _solution.GetProject(searchSymbol.ContainingAssembly);
                        if (symbolProject?.LanguageServices.GetService<ILanguageServiceReferenceFinder>() is { } service)
                        {
                            symbols = await service.DetermineCascadedSymbolsAsync(
                                searchSymbol, symbolProject, cascadeDirection, cancellationToken).ConfigureAwait(false);
                            AddSymbolTasks(result, symbols, symbolTasks, cancellationToken);
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        await Task.WhenAll(symbolTasks).ConfigureAwait(false);
                    }, cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
                }

                await Task.WhenAll(finderTasks).ConfigureAwait(false);
            }
        }

        private async Task<SymbolGroup> DetermineSymbolGroupAsync(ISymbol searchSymbol, CancellationToken cancellationToken)
        {
            if (!_options.Cascade)
                return new SymbolGroup(ImmutableArray.Create(searchSymbol));

            return new SymbolGroup(
                await SymbolFinder.FindLinkedSymbolsAsync(searchSymbol, _solution, cancellationToken).ConfigureAwait(false));
        }

        private void AddSymbolTasks(
            ConcurrentSet<SymbolGroup> result,
            ImmutableArray<(ISymbol symbol, FindReferencesCascadeDirection cascadeDirection)> symbols,
            ArrayBuilder<Task> symbolTasks,
            CancellationToken cancellationToken)
        {
            if (!symbols.IsDefault)
            {
                foreach (var (symbol, cascadeDirection) in symbols)
                {
                    Contract.ThrowIfNull(symbol);

                    // If we're cascading unidirectionally, then keep going in the direction this symbol was found in.
                    // Otherwise, if we're not unidirectional, then continue to cascade in both directions with this
                    // symbol.
                    var finalDirection = _options.UnidirectionalHierarchyCascade
                        ? cascadeDirection
                        : FindReferencesCascadeDirection.UpAndDown;
                    symbolTasks.Add(Task.Factory.StartNew(
                        () => DetermineAllSymbolsCoreAsync(symbol, finalDirection, result, cancellationToken), cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap());
                }
            }
        }

        private ImmutableHashSet<Project>? GetProjectScope()
        {
            if (_documents == null)
            {
                return null;
            }

            var builder = ImmutableHashSet.CreateBuilder<Project>();
            foreach (var document in _documents)
            {
                builder.Add(document.Project);

                foreach (var reference in document.Project.ProjectReferences)
                {
                    var referenceProject = document.Project.Solution.GetProject(reference.ProjectId);
                    if (referenceProject != null)
                    {
                        builder.Add(referenceProject);
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static ISymbol MapToAppropriateSymbol(ISymbol symbol)
        {
            // Never search for an alias.  Always search for it's target.  Note: if the caller was
            // actually searching for an alias, they can always get that information out in the end
            // by checking the ReferenceLocations that are returned.
            var searchSymbol = symbol;

            if (searchSymbol is IAliasSymbol)
            {
                searchSymbol = ((IAliasSymbol)searchSymbol).Target;
            }

            searchSymbol = searchSymbol.GetOriginalUnreducedDefinition();

            // If they're searching for a delegate constructor, then just search for the delegate
            // itself.  They're practically interchangeable for consumers.
            if (searchSymbol.IsConstructor() && searchSymbol.ContainingType.TypeKind == TypeKind.Delegate)
            {
                searchSymbol = symbol.ContainingType;
            }

            Contract.ThrowIfNull(searchSymbol);
            return searchSymbol;
        }
    }
}
