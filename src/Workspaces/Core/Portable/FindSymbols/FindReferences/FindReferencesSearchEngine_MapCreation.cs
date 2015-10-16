// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private async Task<ConcurrentDictionary<Document, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>>> CreateDocumentMapAsync(
            ConcurrentDictionary<Project, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>> projectMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateDocumentMapAsync, _cancellationToken))
            {
                Func<Document, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>> createQueue = d => new ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>();

                var documentMap = new ConcurrentDictionary<Document, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>>();

#if PARALLEL
            Roslyn.Utilities.TaskExtensions.RethrowIncorrectAggregateExceptions(cancellationToken, () =>
                {
                    projectMap.AsParallel().WithCancellation(cancellationToken).ForAll(kvp =>
                    {
                        var project = kvp.Key;
                        var projectQueue = kvp.Value;

                        projectQueue.AsParallel().WithCancellation(cancellationToken).ForAll(symbolAndFinder =>
                        {
                            var symbol = symbolAndFinder.Item1;
                            var finder = symbolAndFinder.Item2;

                            var documents = finder.DetermineDocumentsToSearch(symbol, project, cancellationToken) ?? SpecializedCollections.EmptyEnumerable<Document>();
                            foreach (var document in documents.Distinct().WhereNotNull())
                            {
                                if (includeDocument(document))
                                {
                                    documentMap.GetOrAdd(document, createQueue).Enqueue(symbolAndFinder);
                                }
                            }
                        });
                    });
                });
#else
                foreach (var kvp in projectMap)
                {
                    var project = kvp.Key;
                    var projectQueue = kvp.Value;

                    foreach (var symbolAndFinder in projectQueue)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var symbol = symbolAndFinder.Item1;
                        var finder = symbolAndFinder.Item2;

                        var documents = await finder.DetermineDocumentsToSearchAsync(symbol, project, _documents, _cancellationToken).ConfigureAwait(false) ?? SpecializedCollections.EmptyEnumerable<Document>();
                        foreach (var document in documents.Distinct().WhereNotNull())
                        {
                            if (_documents == null || _documents.Contains(document))
                            {
                                documentMap.GetOrAdd(document, createQueue).Enqueue(symbolAndFinder);
                            }
                        }
                    }
                }
#endif

                Contract.ThrowIfTrue(documentMap.Any(kvp => kvp.Value.Count != kvp.Value.ToSet().Count));
                return documentMap;
            }
        }

        private async Task<ConcurrentDictionary<Project, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>>> CreateProjectMapAsync(
            ConcurrentSet<ISymbol> symbols)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateProjectMapAsync, _cancellationToken))
            {
                Func<Project, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>> createQueue = p => new ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>();

                var projectMap = new ConcurrentDictionary<Project, ConcurrentQueue<ValueTuple<ISymbol, IReferenceFinder>>>();

#if PARALLEL
            Roslyn.Utilities.TaskExtensions.RethrowIncorrectAggregateExceptions(cancellationToken, () =>
                {
                    symbols.AsParallel().WithCancellation(cancellationToken).ForAll(s =>
                    {
                        finders.AsParallel().WithCancellation(cancellationToken).ForAll(f =>
                        {
                            var projects = f.DetermineProjectsToSearch(s, solution, cancellationToken) ?? SpecializedCollections.EmptyEnumerable<Project>();
                            foreach (var project in projects.Distinct())
                            {
                                projectMap.GetOrAdd(project, createQueue).Enqueue(ValueTuple.Create(s, f));
                            }
                        });
                    });
                });
#else

                var scope = _documents != null ? _documents.Select(d => d.Project).ToImmutableHashSet() : null;
                foreach (var s in symbols)
                {
                    foreach (var f in _finders)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var projects = await f.DetermineProjectsToSearchAsync(s, _solution, scope, _cancellationToken).ConfigureAwait(false) ?? SpecializedCollections.EmptyEnumerable<Project>();
                        foreach (var project in projects.Distinct().WhereNotNull())
                        {
                            if (scope == null || scope.Contains(project))
                            {
                                projectMap.GetOrAdd(project, createQueue).Enqueue(ValueTuple.Create(s, f));
                            }
                        }
                    }
                }
#endif

                Contract.ThrowIfTrue(projectMap.Any(kvp => kvp.Value.Count != kvp.Value.ToSet().Count));
                return projectMap;
            }
        }

        private async Task<ConcurrentSet<ISymbol>> DetermineAllSymbolsAsync(ISymbol symbol)
        {
            using (Logger.LogBlock(FunctionId.FindReference_DetermineAllSymbolsAsync, _cancellationToken))
            {
                var result = new ConcurrentSet<ISymbol>(SymbolEquivalenceComparer.Instance);
                await DetermineAllSymbolsCoreAsync(symbol, result).ConfigureAwait(false);
                return result;
            }
        }

        private async Task DetermineAllSymbolsCoreAsync(ISymbol symbol, ConcurrentSet<ISymbol> result)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var searchSymbol = MapToAppropriateSymbol(symbol);

            // 2) Try to map this back to source symbol if this was a metadata symbol.
            searchSymbol = await SymbolFinder.FindSourceDefinitionAsync(searchSymbol, _solution, _cancellationToken).ConfigureAwait(false) ?? searchSymbol;

            if (searchSymbol != null && result.Add(searchSymbol))
            {
                _progress.OnDefinitionFound(searchSymbol);

                _foundReferences.GetOrAdd(searchSymbol, s_createSymbolLocations);

                // get project to search
                var projects = GetProjectScope();

                _cancellationToken.ThrowIfCancellationRequested();

                List<Task> finderTasks = new List<Task>();
                foreach (var f in _finders)
                {
                    finderTasks.Add(Task.Run(async () =>
                    {
                        var symbolTasks = new List<Task>();

                        var symbols = await f.DetermineCascadedSymbolsAsync(searchSymbol, _solution, projects, _cancellationToken).ConfigureAwait(false);
                        AddSymbolTasks(result, symbols, symbolTasks);

                        // Defer to the language to see if it wants to cascade here in some special way.
                        var symbolProject = _solution.GetProject(searchSymbol.ContainingAssembly);
                        var service = symbolProject?.LanguageServices.GetService<ILanguageServiceReferenceFinder>();
                        if (service != null)
                        {
                            symbols = await service.DetermineCascadedSymbolsAsync(searchSymbol, symbolProject, _cancellationToken).ConfigureAwait(false);
                            AddSymbolTasks(result, symbols, symbolTasks);
                        }

                        _cancellationToken.ThrowIfCancellationRequested();

                        await Task.WhenAll(symbolTasks).ConfigureAwait(false);
                    }, _cancellationToken));
                }

                await Task.WhenAll(finderTasks).ConfigureAwait(false);
            }
        }

        private void AddSymbolTasks(ConcurrentSet<ISymbol> result, IEnumerable<ISymbol> symbols, List<Task> symbolTasks)
        {
            if (symbols != null)
            {
                foreach (var child in symbols)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    symbolTasks.Add(Task.Run(async () => await DetermineAllSymbolsCoreAsync(child, result).ConfigureAwait(false), _cancellationToken));
                }
            }
        }

        private ImmutableHashSet<Project> GetProjectScope()
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

            return searchSymbol;
        }
    }
}
