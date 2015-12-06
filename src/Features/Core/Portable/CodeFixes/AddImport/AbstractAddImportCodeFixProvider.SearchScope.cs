﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal abstract partial class AbstractAddImportCodeFixProvider
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

            public async Task<IEnumerable<SearchResult<ISymbol>>> FindDeclarationsAsync(string name, SymbolFilter filter)
            {
                var query = this.Exact ? new SearchQuery(name, ignoreCase: true) : new SearchQuery(GetInexactPredicate(name));
                var symbols = await FindDeclarationsAsync(name, filter, query).ConfigureAwait(false);

                if (Exact)
                {
                    // Exact matches always have a weight of 0.  This way they come before all other matches.
                    return symbols.Select(s => SearchResult.Create(s.Name, s, weight: 0)).ToList();
                }

                // TODO(cyrusn): It's a shame we have to compute this twice.  However, there's no
                // great way to store the original value we compute because it happens deep in the 
                // compiler bowels when we call FindDeclarations.
                return symbols.Select(s =>
                {
                    double matchCost;
                    var isCloseMatch = EditDistance.IsCloseMatch(name, s.Name, out matchCost);

                    Debug.Assert(isCloseMatch);
                    return SearchResult.Create(s.Name, s, matchCost);
                }).ToList();
            }

            private Func<string, bool> GetInexactPredicate(string name)
            {
                // Create the edit distance object outside of the lambda  That way we only create it
                // once and it can cache all the information it needs while it does the IsCloseMatch
                // check against all the possible candidates.
                var editDistance = new EditDistance(name);
                return n =>
                {
                    double matchCost;
                    return editDistance.IsCloseMatch(n, out matchCost);
                };
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
