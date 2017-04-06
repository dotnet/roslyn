// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
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