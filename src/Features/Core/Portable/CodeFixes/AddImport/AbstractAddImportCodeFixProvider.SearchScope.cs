// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
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
                return symbols.Select(s =>
                {
                    double matchCost;
                    var isCloseMatch = EditDistance.IsCloseMatch(name, s.Name, out matchCost);

                    Debug.Assert(isCloseMatch);
                    return SearchResult.Create(s.Name, nameNode, s, matchCost);
                }).ToList();
            }
        }

        private class ProjectSearchScope : SearchScope
        {
            private readonly bool _includeDirectReferences;
            private readonly Project _project;

            public ProjectSearchScope(Project project, bool includeDirectReferences, bool ignoreCase, CancellationToken cancellationToken)
                : base(ignoreCase, cancellationToken)
            {
                _project = project;
                _includeDirectReferences = includeDirectReferences;
            }

            protected override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                return SymbolFinder.FindDeclarationsAsync(
                    _project, searchQuery, filter, _includeDirectReferences, cancellationToken);
            }

            public override SymbolReference CreateReference<T>(SearchResult<T> searchResult)
            {
                return new ProjectSymbolReference(
                    searchResult.WithSymbol<INamespaceOrTypeSymbol>(searchResult.Symbol), _project.Id);
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

            protected override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                return SymbolFinder.FindDeclarationsAsync(
                    _solution, _assembly, _metadataReference.FilePath, searchQuery, filter, cancellationToken);
            }
        }
    }
}
