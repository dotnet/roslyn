// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using SymbolGroup = ImmutableArray<SymbolAndProjectId>;

    internal partial class FindReferencesSearchEngine
    {
        private static readonly SymbolAndProjectIdComparer s_symbolComparer = 
            new SymbolAndProjectIdComparer(MetadataUnifyingEquivalenceComparer.Instance);

        private async Task<ConcurrentDictionary<Document, ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>>> CreateDocumentMapAsync(
            ConcurrentDictionary<Project, ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>> projectMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateDocumentMapAsync, _cancellationToken))
            {
                Func<Document, ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>> createQueue = 
                    d => new ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>();

                var documentMap = new ConcurrentDictionary<Document, ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>>();

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

                    foreach (var symbolGroupAndFinder in projectQueue)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var symbolGroup = symbolGroupAndFinder.Item1;
                        var finder = symbolGroupAndFinder.Item2;

                        var documents = await finder.DetermineDocumentsToSearchAsync(symbolGroup, project, _documents, _cancellationToken).ConfigureAwait(false);
                        foreach (var document in documents.Distinct().WhereNotNull())
                        {
                            if (_documents == null || _documents.Contains(document))
                            {
                                documentMap.GetOrAdd(document, createQueue).Enqueue(symbolGroupAndFinder);
                            }
                        }
                    }
                }
#endif

                Contract.ThrowIfTrue(documentMap.Any(kvp => kvp.Value.Count != kvp.Value.ToSet().Count));
                return documentMap;
            }
        }

        private async Task<ConcurrentDictionary<Project, ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>>> CreateProjectMapAsync(
            ImmutableArray<SymbolGroup> symbols)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateProjectMapAsync, _cancellationToken))
            {
                Func<Project, ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>> createQueue = 
                    p => new ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>();

                var projectMap = new ConcurrentDictionary<Project, ConcurrentQueue<ValueTuple<SymbolGroup, IReferenceFinder>>>();

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
                foreach (var symbolGroup in symbols)
                {
                    foreach (var f in _finders)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var projects = await f.DetermineProjectsToSearchAsync(symbolGroup, _solution, scope, _cancellationToken).ConfigureAwait(false);
                        foreach (var project in projects.Distinct().WhereNotNull())
                        {
                            if (scope == null || scope.Contains(project))
                            {
                                projectMap.GetOrAdd(project, createQueue).Enqueue(ValueTuple.Create(symbolGroup, f));
                            }
                        }
                    }
                }
#endif

                Contract.ThrowIfTrue(projectMap.Any(kvp => kvp.Value.Count != kvp.Value.ToSet().Count));
                return projectMap;
            }
        }

        private async Task<ImmutableArray<SymbolGroup>> DetermineAllSymbolsAsync(
            SymbolAndProjectId symbolAndProjectId)
        {
            using (Logger.LogBlock(FunctionId.FindReference_DetermineAllSymbolsAsync, _cancellationToken))
            {
                var result = new ConcurrentSet<SymbolAndProjectId>(s_symbolComparer);
                await DetermineAllSymbolsCoreAsync(symbolAndProjectId, result).ConfigureAwait(false);

                // Now bucket the symbols accordingly.
                 
                return BucketResults(result);
            }
        }

        private ImmutableArray<SymbolGroup> BucketResults(
            ConcurrentSet<SymbolAndProjectId> result)
        {
            var buckets = ArrayBuilder<SymbolGroup>.GetInstance();

            var namedTypeToConstructors = new Dictionary<SymbolAndProjectId, List<SymbolAndProjectId>>(
                s_symbolComparer);

            // We combine named types and their constructors into one single bucket.
            foreach (var symbolAndProject in result)
            {
                if (symbolAndProject.Symbol.Kind == SymbolKind.NamedType)
                {
                    namedTypeToConstructors.Add(symbolAndProject, new List<SymbolAndProjectId>());
                }
            }

            foreach (var symbolAndProject in result)
            {
                if (symbolAndProject.Symbol.Kind  == SymbolKind.NamedType)
                {
                    // Skip named types as we've already processed them in the higher loop.
                    continue;
                }

                // If we find a constructor, bucket it with its named type (if we've seen the
                // named type for it).
                if (symbolAndProject.Symbol.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)symbolAndProject.Symbol;
                    if (method.MethodKind == MethodKind.Constructor)
                    {
                        List<SymbolAndProjectId> constructors;
                        if (namedTypeToConstructors.TryGetValue(
                                symbolAndProject.WithSymbol(method.ContainingType), out constructors))
                        {
                            constructors.Add(symbolAndProject);
                            continue;
                        }
                    }
                }

                // Anything else goes in its own bucket.
                buckets.Add(ImmutableArray.Create(symbolAndProject));
            }

            foreach (var kvp in namedTypeToConstructors)
            {
                var bucket = ArrayBuilder<SymbolAndProjectId>.GetInstance();
                bucket.Add(kvp.Key);
                bucket.AddRange(kvp.Value);
                buckets.Add(bucket.ToImmutableAndFree());
            }

            return buckets.ToImmutableAndFree();
        }

        private async Task DetermineAllSymbolsCoreAsync(
            SymbolAndProjectId symbolAndProjectId, ConcurrentSet<SymbolAndProjectId> result)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var searchSymbolAndProjectId = MapToAppropriateSymbol(symbolAndProjectId);

            // 2) Try to map this back to source symbol if this was a metadata symbol.
            var sourceSymbolAndProjectId = await SymbolFinder.FindSourceDefinitionAsync(searchSymbolAndProjectId, _solution, _cancellationToken).ConfigureAwait(false);
            if (sourceSymbolAndProjectId.Symbol != null)
            {
                searchSymbolAndProjectId = sourceSymbolAndProjectId;
            }

            var searchSymbol = searchSymbolAndProjectId.Symbol;
            if (searchSymbol != null && result.Add(searchSymbolAndProjectId))
            {
                await _progress.OnDefinitionFoundAsync(searchSymbolAndProjectId).ConfigureAwait(false);

                _foundReferences.GetOrAdd(searchSymbolAndProjectId, s_createSymbolLocations);

                // get project to search
                var projects = GetProjectScope();

                _cancellationToken.ThrowIfCancellationRequested();

                List<Task> finderTasks = new List<Task>();
                foreach (var f in _finders)
                {
                    finderTasks.Add(Task.Run(async () =>
                    {
                        var symbolTasks = new List<Task>();

                        var symbols = await f.DetermineCascadedSymbolsAsync(
                            searchSymbolAndProjectId, _solution, projects, _cancellationToken).ConfigureAwait(false);
                        AddSymbolTasks(result, symbols, symbolTasks);

                        // Defer to the language to see if it wants to cascade here in some special way.
                        var symbolProject = _solution.GetProject(searchSymbol.ContainingAssembly);
                        var service = symbolProject?.LanguageServices.GetService<ILanguageServiceReferenceFinder>();
                        if (service != null)
                        {
                            symbols = await service.DetermineCascadedSymbolsAsync(
                                searchSymbolAndProjectId, symbolProject, _cancellationToken).ConfigureAwait(false);
                            AddSymbolTasks(result, symbols, symbolTasks);
                        }

                        _cancellationToken.ThrowIfCancellationRequested();

                        await Task.WhenAll(symbolTasks).ConfigureAwait(false);
                    }, _cancellationToken));
                }

                await Task.WhenAll(finderTasks).ConfigureAwait(false);
            }
        }

        private void AddSymbolTasks(
            ConcurrentSet<SymbolAndProjectId> result,
            IEnumerable<SymbolAndProjectId> symbols,
            List<Task> symbolTasks)
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

        private static SymbolAndProjectId MapToAppropriateSymbol(
            SymbolAndProjectId symbolAndProjectId)
        {
            // Never search for an alias.  Always search for it's target.  Note: if the caller was
            // actually searching for an alias, they can always get that information out in the end
            // by checking the ReferenceLocations that are returned.
            var symbol = symbolAndProjectId.Symbol;
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

            return symbolAndProjectId.WithSymbol(searchSymbol);
        }
    }
}