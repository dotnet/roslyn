﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
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
            protected readonly AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider;
            public readonly CancellationToken CancellationToken;

            protected SearchScope(AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider, bool exact, CancellationToken cancellationToken)
            {
                this.provider = provider;
                Exact = exact;
                CancellationToken = cancellationToken;
            }

            protected abstract Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery query);
            public abstract SymbolReference CreateReference<T>(SymbolResult<T> symbol) where T : INamespaceOrTypeSymbol;

            public async Task<ImmutableArray<SymbolResult<ISymbol>>> FindDeclarationsAsync(
                string name, TSimpleNameSyntax nameNode, SymbolFilter filter)
            {
                if (name != null && string.IsNullOrWhiteSpace(name))
                {
                    return ImmutableArray<SymbolResult<ISymbol>>.Empty;
                }

                var query = this.Exact ? SearchQuery.Create(name, ignoreCase: true) : SearchQuery.CreateFuzzy(name);
                var symbols = await FindDeclarationsAsync(name, filter, query).ConfigureAwait(false);

                if (Exact)
                {
                    // We did an exact, case insensitive, search.  Case sensitive matches should
                    // be preffered though over insensitive ones.
                    return symbols.SelectAsArray(s => 
                        SymbolResult.Create(s.Name, nameNode, s, weight: s.Name == name ? 0 : 1));
                }

                // TODO(cyrusn): It's a shame we have to compute this twice.  However, there's no
                // great way to store the original value we compute because it happens deep in the 
                // compiler bowels when we call FindDeclarations.
                using (var similarityChecker = new WordSimilarityChecker(name, substringsAreSimilar: false))
                {
                    return symbols.SelectAsArray(s =>
                    {
                        double matchCost;
                        var areSimilar = similarityChecker.AreSimilar(s.Name, out matchCost);

                        Debug.Assert(areSimilar);
                        return SymbolResult.Create(s.Name, nameNode, s, matchCost);
                    });
                }
            }
        }

        private abstract class ProjectSearchScope : SearchScope
        {
            protected readonly Project _project;

            public ProjectSearchScope(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                Project project,
                bool ignoreCase,
                CancellationToken cancellationToken)
                : base(provider, ignoreCase, cancellationToken)
            {
                _project = project;
            }

            public override SymbolReference CreateReference<T>(SymbolResult<T> symbol)
            {
                return new ProjectSymbolReference(
                    provider, symbol.WithSymbol<INamespaceOrTypeSymbol>(symbol.Symbol), _project);
            }
        }

        /// <summary>
        /// SearchScope used for searching *all* the symbols contained within a project/compilation.
        /// i.e. the symbols created from source *and* symbols from references (both project and
        /// metadata).
        /// </summary>
        private class AllSymbolsProjectSearchScope : ProjectSearchScope
        {
            public AllSymbolsProjectSearchScope(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                Project project,
                bool ignoreCase,
                CancellationToken cancellationToken)
                : base(provider, project, ignoreCase, cancellationToken)
            {
            }

            protected override Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                return SymbolFinder.FindDeclarationsAsync(_project, searchQuery, filter, CancellationToken);
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
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
                Project project, bool ignoreCase, CancellationToken cancellationToken)
                : base(provider, project, ignoreCase, cancellationToken)
            {
                _projectToAssembly = projectToAssembly;
            }

            protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                var service = _project.Solution.Workspace.Services.GetService<ISymbolTreeInfoCacheService>();
                var info = await service.TryGetSourceSymbolTreeInfoAsync(_project, CancellationToken).ConfigureAwait(false);
                if (info == null)
                {
                    // Looks like there was nothing in the cache.  Return no results for now.
                    return ImmutableArray<ISymbol>.Empty;
                }

                // Don't create the assembly until it is actually needed by the SymbolTreeInfo.FindAsync
                // code.  Creating the assembly can be costly and we want to avoid it until it is actually
                // needed.
                var lazyAssembly = _projectToAssembly.GetOrAdd(_project, CreateLazyAssembly);

                return await info.FindAsync(searchQuery, lazyAssembly, filter, CancellationToken).ConfigureAwait(false);
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
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                Solution solution,
                IAssemblySymbol assembly,
                PortableExecutableReference metadataReference,
                bool exact,
                CancellationToken cancellationToken)
                : base(provider, exact, cancellationToken)
            {
                _solution = solution;
                _assembly = assembly;
                _metadataReference = metadataReference;
            }

            public override SymbolReference CreateReference<T>(SymbolResult<T> searchResult)
            {
                return new MetadataSymbolReference(
                    provider,
                    searchResult.WithSymbol<INamespaceOrTypeSymbol>(searchResult.Symbol),
                    _metadataReference);
            }

            protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                var service = _solution.Workspace.Services.GetService<ISymbolTreeInfoCacheService>();
                var info = await service.TryGetMetadataSymbolTreeInfoAsync(_solution, _metadataReference, CancellationToken).ConfigureAwait(false);
                if (info == null)
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                return await info.FindAsync(searchQuery, _assembly, filter, CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
