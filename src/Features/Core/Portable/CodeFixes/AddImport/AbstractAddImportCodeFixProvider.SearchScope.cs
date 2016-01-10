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

            protected readonly AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider;
            protected readonly CancellationToken cancellationToken;

            protected SearchScope(AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider, bool exact, CancellationToken cancellationToken)
            {
                this.provider = provider;
                this.Exact = exact;
                this.cancellationToken = cancellationToken;
            }

            protected abstract Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery query);
            public abstract SymbolReference CreateReference<T>(SymbolResult<T> symbol) where T : INamespaceOrTypeSymbol;

            public async Task<IEnumerable<SymbolResult<ISymbol>>> FindDeclarationsAsync(string name, TSimpleNameSyntax nameNode, SymbolFilter filter)
            {
                if (name != null && string.IsNullOrWhiteSpace(name))
                {
                    return SpecializedCollections.EmptyEnumerable<SymbolResult<ISymbol>>();
                }

                var query = this.Exact ? SearchQuery.Create(name, ignoreCase: true) : SearchQuery.CreateFuzzy(name);
                var symbols = await FindDeclarationsAsync(name, filter, query).ConfigureAwait(false);

                if (Exact)
                {
                    // We did an exact, case insensitive, search.  Case sensitive matches should
                    // be preffered though over insensitive ones.
                    return symbols.Select(s => SymbolResult.Create(s.Name, nameNode, s, weight: s.Name == name ? 0 : 1)).ToList();
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
                        return SymbolResult.Create(s.Name, nameNode, s, matchCost);
                    }).ToList();
                }
            }
        }

        private class ProjectSearchScope : SearchScope
        {
            private readonly bool _includeDirectReferences;
            private readonly Project _project;

            public ProjectSearchScope(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                Project project,
                bool includeDirectReferences,
                bool exact,
                CancellationToken cancellationToken)
                : base(provider, exact, cancellationToken)
            {
                _project = project;
                _includeDirectReferences = includeDirectReferences;
            }

            protected override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                return SymbolFinder.FindDeclarationsAsync(
                    _project, searchQuery, filter, _includeDirectReferences, cancellationToken);
            }

            public override SymbolReference CreateReference<T>(SymbolResult<T> searchResult)
            {
                return new ProjectSymbolReference(
                    provider, searchResult.WithSymbol<INamespaceOrTypeSymbol>(searchResult.Symbol), _project.Id);
            }
        }

        private class MetadataSearchScope : SearchScope
        {
            private readonly IAssemblySymbol _assembly;
            private readonly PortableExecutableReference _metadataReference;
            private readonly Solution _solution;

            public MetadataSearchScope(
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

            protected override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                return SymbolFinder.FindDeclarationsAsync(
                    _solution, _assembly, _metadataReference, searchQuery, filter, cancellationToken);
            }
        }
    }
}
