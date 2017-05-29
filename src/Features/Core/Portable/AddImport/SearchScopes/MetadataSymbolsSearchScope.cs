// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class MetadataSymbolsSearchScope : SearchScope
        {
            private readonly Solution _solution;
            private readonly IAssemblySymbol _assembly;
            private readonly ProjectId _assemblyProjectId;
            private readonly PortableExecutableReference _metadataReference;
            private readonly Checksum _metadataChecksum;

            public MetadataSymbolsSearchScope(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                Solution solution,
                IAssemblySymbol assembly,
                ProjectId assemblyProjectId,
                PortableExecutableReference metadataReference,
                Checksum metadataChecksum,
                bool exact,
                CancellationToken cancellationToken)
                : base(provider, exact, cancellationToken)
            {
                _solution = solution;
                _assembly = assembly;
                _assemblyProjectId = assemblyProjectId;
                _metadataReference = metadataReference;
                _metadataChecksum = metadataChecksum;
            }

            public override SymbolReference CreateReference<T>(SymbolResult<T> searchResult)
            {
                return new MetadataSymbolReference(
                    provider,
                    searchResult.WithSymbol<INamespaceOrTypeSymbol>(searchResult.Symbol),
                    _metadataReference);
            }

            protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                SymbolFilter filter, SearchQuery searchQuery)
            {
                var result = await AddImportSymbolTreeInfoService.TryFindMetadataSymbolsAsync(
                    _solution, _metadataReference, _metadataChecksum, _assembly,
                    _assemblyProjectId, filter, searchQuery, CancellationToken).ConfigureAwait(false);

                return result.SelectAsArray(s => s.Symbol);
            }
        }
    }
}