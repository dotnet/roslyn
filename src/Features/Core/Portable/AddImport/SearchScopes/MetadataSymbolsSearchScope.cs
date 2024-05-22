// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private class MetadataSymbolsSearchScope(
        AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
        Project assemblyProject,
        IAssemblySymbol assembly,
        PortableExecutableReference metadataReference,
        bool exact) : SearchScope(provider, exact)
    {
        private readonly Project _assemblyProject = assemblyProject;
        private readonly IAssemblySymbol _assembly = assembly;
        private readonly PortableExecutableReference _metadataReference = metadataReference;

        public override SymbolReference CreateReference<T>(SymbolResult<T> searchResult)
        {
            return new MetadataSymbolReference(
                provider,
                searchResult.WithSymbol<INamespaceOrTypeSymbol>(searchResult.Symbol),
                _assemblyProject.Id,
                _metadataReference);
        }

        protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
            SymbolFilter filter, SearchQuery searchQuery, CancellationToken cancellationToken)
        {
            var service = _assemblyProject.Solution.Services.GetRequiredService<ISymbolTreeInfoCacheService>();
            var info = await service.TryGetPotentiallyStaleMetadataSymbolTreeInfoAsync(_assemblyProject, _metadataReference, cancellationToken).ConfigureAwait(false);
            if (info == null)
                return [];

            var declarations = await info.FindAsync(
                searchQuery, _assembly, filter, cancellationToken).ConfigureAwait(false);

            return declarations;
        }
    }
}
