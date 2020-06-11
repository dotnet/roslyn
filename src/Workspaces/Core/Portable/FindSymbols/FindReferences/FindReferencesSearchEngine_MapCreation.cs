// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using DocumentMap = MultiDictionary<Document, (ISymbol symbol, IReferenceFinder finder)>;
    using ProjectMap = MultiDictionary<Project, (ISymbol symbol, IReferenceFinder finder)>;
    using ProjectToDocumentMap = Dictionary<Project, MultiDictionary<Document, (ISymbol symbol, IReferenceFinder finder)>>;

    internal partial class FindReferencesSearchEngine
    {
        private static readonly Func<Project, DocumentMap> s_createDocumentMap = _ => new DocumentMap();

        private async Task<ProjectToDocumentMap> CreateProjectToDocumentMapAsync(ProjectMap projectMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateDocumentMapAsync, _cancellationToken))
            {
                using var _ = ArrayBuilder<Task<(ImmutableArray<Document>, ISymbol, IReferenceFinder)>>.GetInstance(out var tasks);

                foreach (var (project, projectQueue) in projectMap)
                {
                    foreach (var (symbol, finder) in projectQueue)
                    {
                        tasks.Add(Task.Run(() =>
                            DetermineDocumentsToSearchAsync(project, symbol, finder), _cancellationToken));
                    }
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);

                var finalMap = new ProjectToDocumentMap();
                foreach (var (documents, symbol, finder) in results)
                {
                    foreach (var document in documents)
                    {
                        finalMap.GetOrAdd(document.Project, s_createDocumentMap)
                                .Add(document, (symbol, finder));
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

        private async Task<(ImmutableArray<Document>, ISymbol, IReferenceFinder)> DetermineDocumentsToSearchAsync(
            Project project, ISymbol symbol, IReferenceFinder finder)
        {
            var documents = await finder.DetermineDocumentsToSearchAsync(
                symbol, project, _documents, _options, _cancellationToken).ConfigureAwait(false);
            var finalDocs = documents.WhereNotNull().Distinct().Where(
                d => _documents == null || _documents.Contains(d)).ToImmutableArray();
            return (finalDocs, symbol, finder);
        }

        private async Task<ProjectMap> CreateProjectMapAsync(ConcurrentSet<ISymbol> symbols)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateProjectMapAsync, _cancellationToken))
            {
                var projectMap = new ProjectMap();

                var scope = _documents?.Select(d => d.Project).ToImmutableHashSet();
                foreach (var symbol in symbols)
                {
                    foreach (var finder in _finders)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var projects = await finder.DetermineProjectsToSearchAsync(symbol, _solution, scope, _cancellationToken).ConfigureAwait(false);
                        foreach (var project in projects.Distinct().WhereNotNull())
                        {
                            if (scope == null || scope.Contains(project))
                            {
                                projectMap.Add(project, (symbol, finder));
                            }
                        }
                    }
                }

                Contract.ThrowIfTrue(projectMap.Any(kvp => kvp.Value.Count != kvp.Value.ToSet().Count));
                return projectMap;
            }
        }

        private async Task<ConcurrentSet<ISymbol>> DetermineAllSymbolsAsync(
            ISymbol symbol)
        {
            using (Logger.LogBlock(FunctionId.FindReference_DetermineAllSymbolsAsync, _cancellationToken))
            {
                var result = new ConcurrentSet<ISymbol>(MetadataUnifyingEquivalenceComparer.Instance);
                await DetermineAllSymbolsCoreAsync(symbol, result).ConfigureAwait(false);
                return result;
            }
        }

        private async Task DetermineAllSymbolsCoreAsync(
            ISymbol symbol, ConcurrentSet<ISymbol> result)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var searchSymbol = MapToAppropriateSymbol(symbol);

            // 2) Try to map this back to source symbol if this was a metadata symbol.
            var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(searchSymbol, _solution, _cancellationToken).ConfigureAwait(false);
            if (sourceSymbol != null)
            {
                searchSymbol = sourceSymbol;
            }

            if (searchSymbol != null && result.Add(searchSymbol))
            {
                await _progress.OnDefinitionFoundAsync(searchSymbol).ConfigureAwait(false);

                // get project to search
                var projects = GetProjectScope();

                _cancellationToken.ThrowIfCancellationRequested();

                var finderTasks = new List<Task>();
                foreach (var f in _finders)
                {
                    finderTasks.Add(Task.Run(async () =>
                    {
                        var symbolTasks = new List<Task>();

                        var symbols = await f.DetermineCascadedSymbolsAsync(
                            searchSymbol, _solution, projects, _options, _cancellationToken).ConfigureAwait(false);
                        AddSymbolTasks(result, symbols, symbolTasks);

                        // Defer to the language to see if it wants to cascade here in some special way.
                        var symbolProject = _solution.GetProject(searchSymbol.ContainingAssembly);
                        var service = symbolProject?.LanguageServices.GetService<ILanguageServiceReferenceFinder>();
                        if (service != null)
                        {
                            symbols = await service.DetermineCascadedSymbolsAsync(
                                searchSymbol, symbolProject, _cancellationToken).ConfigureAwait(false);
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
            ConcurrentSet<ISymbol> result,
            ImmutableArray<ISymbol> symbols,
            List<Task> symbolTasks)
        {
            if (!symbols.IsDefault)
            {
                foreach (var child in symbols)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    symbolTasks.Add(Task.Run(() => DetermineAllSymbolsCoreAsync(child, result), _cancellationToken));
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
