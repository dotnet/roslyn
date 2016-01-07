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

        private class ProjectAndDirectReferencesSearchScope : ProjectSearchScope
        {
            public ProjectAndDirectReferencesSearchScope(Project project, bool ignoreCase, CancellationToken cancellationToken)
                : base(project, ignoreCase, cancellationToken)
            {
            }

            protected override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                return SymbolFinder.FindDeclarationsAsync(_project, searchQuery, filter, cancellationToken);
            }
        }

        private class ProjectSourceOnlySearchScope : ProjectSearchScope
        {
            private readonly ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> _projectToAssembly;

            public ProjectSourceOnlySearchScope(
                ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
                Project project, bool ignoreCase, CancellationToken cancellationToken)
                : base(project, ignoreCase, cancellationToken)
            {
                _projectToAssembly = projectToAssembly;
            }

            protected override async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                var service = _project.Solution.Workspace.Services.GetService<ISymbolTreeInfoCacheService>();
                var result = await service.TryGetSymbolTreeInfoAsync(_project, cancellationToken).ConfigureAwait(false);
                if (!result.Item1)
                {
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
                }

                // Don't create the assembly until it is actually needed by the SymbolTreeInfo.FindAsync
                // code.  Creating the assembly can be costly and we want to avoid it until it is actually
                // needed.
                var lazyAssembly = _projectToAssembly.GetOrAdd(_project, CreateLazyAssembly);

                return await result.Item2.FindAsync(searchQuery, lazyAssembly, cancellationToken).ConfigureAwait(false);
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

        private class MetadataSearchScope : SearchScope
        {
            private readonly IAssemblySymbol _assembly;
            private readonly PortableExecutableReference _metadataReference;
            private readonly Solution _solution;

            public MetadataSearchScope(
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
                var result = await service.TryGetSymbolTreeInfoAsync(_metadataReference, cancellationToken).ConfigureAwait(false);
                if (!result.Item1)
                {
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
                }

                return await result.Item2.FindAsync(searchQuery, _assembly, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}