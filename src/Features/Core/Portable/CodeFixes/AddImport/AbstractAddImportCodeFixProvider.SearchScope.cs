// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        /// <summary>
        /// SearchScope is used to control where the <see cref="AbstractAddImportCodeFixProvider{TSimpleNameSyntax}"/>
        /// searches.  We search different scopes in different ways.  For example we use 
        /// SymbolTreeInfos to search unreferenced projects and metadata dlls.  However,
        /// for the current project we're editing we defer to the compiler to do the 
        /// search.
        /// </summary>
        private abstract class SearchScope
        {
            public readonly bool Exact;
            protected readonly CancellationToken cancellationToken;

            protected SearchScope(bool exact, CancellationToken cancellationToken)
            {
                this.Exact = exact;
                this.cancellationToken = cancellationToken;
            }

            protected abstract Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery query);
            public abstract SymbolReference CreateReference<T>(SearchResult<T> symbol) where T : INamespaceOrTypeSymbol;

            public async Task<IEnumerable<SearchResult<ISymbol>>> FindDeclarationsAsync(string name, TSimpleNameSyntax nameNode, SymbolFilter filter)
            {
                if (name != null && string.IsNullOrWhiteSpace(name))
                {
                    return SpecializedCollections.EmptyEnumerable<SearchResult<ISymbol>>();
                }

                var query = this.Exact ? SearchQuery.Create(name, ignoreCase: true) : SearchQuery.CreateFuzzy(name);
                var symbols = await FindDeclarationsAsync(name, filter, query).ConfigureAwait(false);

                if (Exact)
                {
                    // We did an exact, case insensitive, search.  Case sensitive matches should
                    // be preffered though over insensitive ones.
                    return symbols.Select(s => SearchResult.Create(s.Name, nameNode, s, weight: s.Name == name ? 0 : 1)).ToList();
                }

                // TODO(cyrusn): It's a shame we have to compute this twice.  However, there's no
                // great way to store the original value we compute because it happens deep in the 
                // compiler bowels when we call FindDeclarations.
                using (var similarityChecker = new WordSimilarityChecker(name))
                {
                    return symbols.Select(s =>
                    {
                        double matchCost;
                        var areSimilar = similarityChecker.AreSimilar(s.Name, out matchCost);

                        Debug.Assert(areSimilar);
                        return SearchResult.Create(s.Name, nameNode, s, matchCost);
                    }).ToList();
                }
            }
        }

        private abstract class ProjectSearchScope : SearchScope
        {
            protected readonly Project _project;

            public ProjectSearchScope(Project project, bool ignoreCase, CancellationToken cancellationToken)
                : base(ignoreCase, cancellationToken)
            {
                _project = project;
            }

            public override SymbolReference CreateReference<T>(SearchResult<T> searchResult)
            {
                return new ProjectSymbolReference(
                    searchResult.WithSymbol<INamespaceOrTypeSymbol>(searchResult.Symbol), _project.Id);
            }
        }

        /// <summary>
        /// SearchScope used for searching *all* the symbols contained within a project/compilation.
        /// i.e. the symbols created from source *and* symbols from references (both project and
        /// metadata).
        /// </summary>
        private class AllSymbolsProjectSearchScope : ProjectSearchScope
        {
            public AllSymbolsProjectSearchScope(Project project, bool ignoreCase, CancellationToken cancellationToken)
                : base(project, ignoreCase, cancellationToken)
            {
            }

            protected override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                return SymbolFinder.FindDeclarationsAsync(_project, searchQuery, filter, cancellationToken);
            }
        }

        /// <summary>
        /// SearchScope used for searching *only* the source symbols contained within a project/compilation.
        /// i.e. symbols from metadata will not be searched.
        /// </summary>
        private class SourceSymbolsProjectSearchScope : ProjectSearchScope
        {
            private readonly ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> _projectToAssembly;

            public SourceSymbolsProjectSearchScope(
                ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
                Project project, bool ignoreCase, CancellationToken cancellationToken)
                : base(project, ignoreCase, cancellationToken)
            {
                _projectToAssembly = projectToAssembly;
            }

            protected override async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                var service = _project.Solution.Workspace.Services.GetService<ISymbolTreeInfoCacheService>();
                var info = await service.TryGetSymbolTreeInfoAsync(_project, cancellationToken).ConfigureAwait(false);
                if (info == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
                }

                // Don't create the assembly until it is actually needed by the SymbolTreeInfo.FindAsync
                // code.  Creating the assembly can be costly and we want to avoid it until it is actually
                // needed.
                var lazyAssembly = _projectToAssembly.GetOrAdd(_project, CreateLazyAssembly);

                return await info.FindAsync(searchQuery, lazyAssembly, cancellationToken).ConfigureAwait(false);
            }

            private static AsyncLazy<IAssemblySymbol> CreateLazyAssembly(Project project)
            {
                return new AsyncLazy<IAssemblySymbol>(
                    async c =>
                    {
                        var compilation = await project.GetCompilationAsync(c).ConfigureAwait(false);
                        return compilation.Assembly;
                    }, cacheResult: true);
            }
        }

        private class MetadataSymbolsSearchScope : SearchScope
        {
            private readonly IAssemblySymbol _assembly;
            private readonly PortableExecutableReference _metadataReference;
            private readonly Solution _solution;

            public MetadataSymbolsSearchScope(
                Solution solution,
                IAssemblySymbol assembly,
                PortableExecutableReference metadataReference,
                bool exact,
                CancellationToken cancellationToken)
                : base(exact, cancellationToken)
            {
                _solution = solution;
                _assembly = assembly;
                _metadataReference = metadataReference;
            }

            public override SymbolReference CreateReference<T>(SearchResult<T> searchResult)
            {
                return new MetadataSymbolReference(
                    searchResult.WithSymbol<INamespaceOrTypeSymbol>(searchResult.Symbol),
                    _metadataReference);
            }

            protected override async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                var service = _solution.Workspace.Services.GetService<ISymbolTreeInfoCacheService>();
                var info = await service.TryGetSymbolTreeInfoAsync(_solution, _assembly, _metadataReference, cancellationToken).ConfigureAwait(false);
                if (info == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
                }

                return await info.FindAsync(searchQuery, _assembly, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}