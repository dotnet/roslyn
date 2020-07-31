// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private class MetadataSymbolsSearchScope : SearchScope
        {
            private readonly Solution _solution;
            private readonly IAssemblySymbol _assembly;
            private readonly ProjectId _assemblyProjectId;
            private readonly PortableExecutableReference _metadataReference;

            public MetadataSymbolsSearchScope(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                Solution solution,
                IAssemblySymbol assembly,
                ProjectId assemblyProjectId,
                PortableExecutableReference metadataReference,
                bool exact,
                CancellationToken cancellationToken)
                : base(provider, exact, cancellationToken)
            {
                _solution = solution;
                _assembly = assembly;
                _assemblyProjectId = assemblyProjectId;
                _metadataReference = metadataReference;
            }

            public override SymbolReference CreateReference<T>(SymbolResult<T> searchResult)
            {
                return new MetadataSymbolReference(
                    provider,
                    searchResult.WithSymbol<INamespaceOrTypeSymbol>(searchResult.Symbol),
                    _assemblyProjectId,
                    _metadataReference);
            }

            protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                SymbolFilter filter, SearchQuery searchQuery)
            {
                var service = _solution.Workspace.Services.GetService<ISymbolTreeInfoCacheService>();
                var info = await service.TryGetMetadataSymbolTreeInfoAsync(_solution, _metadataReference, CancellationToken).ConfigureAwait(false);
                if (info == null)
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                var declarations = await info.FindAsync(
                    searchQuery, _assembly,
                    filter, CancellationToken).ConfigureAwait(false);

                return declarations;
            }
        }
    }
}
