// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    using DocumentMap = MultiDictionary<Document, (SymbolAndProjectId symbolAndProjectId, IReferenceFinder finder)>;
    using ProjectMap = MultiDictionary<Project, (SymbolAndProjectId symbolAndProjectId, IReferenceFinder finder)>;
    using ProjectToDocumentMap = Dictionary<Project, MultiDictionary<Document, (SymbolAndProjectId symbolAndProjectId, IReferenceFinder finder)>>;

    internal partial class FindReferencesSearchEngine
    {
        private async Task<ProjectToDocumentMap> CreateProjectToDocumentMapAsync(ProjectMap projectMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateDocumentMapAsync, _cancellationToken))
            {
                var finalMap = new ProjectToDocumentMap();

                foreach (var kvp in projectMap)
                {
                    var project = kvp.Key;
                    var projectQueue = kvp.Value;

                    var documentMap = new DocumentMap();

                    foreach (var symbolAndFinder in projectQueue)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var symbolAndProjectId = symbolAndFinder.symbolAndProjectId;
                        var symbol = symbolAndProjectId.Symbol;
                        var finder = symbolAndFinder.finder;

                        var documents = await finder.DetermineDocumentsToSearchAsync(
                            symbol, project, _documents, _options, _cancellationToken).ConfigureAwait(false);
                        foreach (var document in documents.Distinct().WhereNotNull())
                        {
                            if (_documents == null || _documents.Contains(document))
                            {
                                documentMap.Add(document, symbolAndFinder);
                            }
                        }
                    }

                    Contract.ThrowIfTrue(documentMap.Any(kvp1 => kvp1.Value.Count != kvp1.Value.ToSet().Count));

                    if (documentMap.Count > 0)
                    {
                        finalMap.Add(project, documentMap);
                    }
                }

                return finalMap;
            }
        }

        private async Task<ProjectMap> CreateProjectMapAsync(ConcurrentSet<SymbolAndProjectId> symbols)
        {
            using (Logger.LogBlock(FunctionId.FindReference_CreateProjectMapAsync, _cancellationToken))
            {
                var projectMap = new ProjectMap();

                var scope = _documents?.Select(d => d.Project).ToImmutableHashSet();
                foreach (var symbolAndProjectId in symbols)
                {
                    foreach (var finder in _finders)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var projects = await finder.DetermineProjectsToSearchAsync(symbolAndProjectId.Symbol, _solution, scope, _cancellationToken).ConfigureAwait(false);
                        foreach (var project in projects.Distinct().WhereNotNull())
                        {
                            if (scope == null || scope.Contains(project))
                            {
                                projectMap.Add(project, (symbolAndProjectId, finder));
                            }
                        }
                    }
                }

                Contract.ThrowIfTrue(projectMap.Any(kvp => kvp.Value.Count != kvp.Value.ToSet().Count));
                return projectMap;
            }
        }

        private async Task<ConcurrentSet<SymbolAndProjectId>> DetermineAllSymbolsAsync(
            SymbolAndProjectId symbolAndProjectId)
        {
            using (Logger.LogBlock(FunctionId.FindReference_DetermineAllSymbolsAsync, _cancellationToken))
            {
                var result = new ConcurrentSet<SymbolAndProjectId>(
                    new SymbolAndProjectIdComparer(MetadataUnifyingEquivalenceComparer.Instance));
                await DetermineAllSymbolsCoreAsync(symbolAndProjectId, result).ConfigureAwait(false);
                return result;
            }
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
                            searchSymbolAndProjectId, _solution, projects, _options, _cancellationToken).ConfigureAwait(false);
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
            ImmutableArray<SymbolAndProjectId> symbols,
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
