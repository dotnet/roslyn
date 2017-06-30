// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        /// <summary>
        /// SearchScope used for searching *only* the source symbols contained within a project/compilation.
        /// i.e. symbols from metadata will not be searched.
        /// </summary>
        private class SourceSymbolsProjectSearchScope : ProjectSearchScope
        {
            private readonly ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> _projectToAssembly;

            public SourceSymbolsProjectSearchScope(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
                Project project, bool ignoreCase, CancellationToken cancellationToken)
                : base(provider, project, ignoreCase, cancellationToken)
            {
                _projectToAssembly = projectToAssembly;
            }

            protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                SymbolFilter filter, SearchQuery searchQuery)
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

                var declarations = await info.FindAsync(
                    searchQuery, lazyAssembly, _project.Id,
                    filter, CancellationToken).ConfigureAwait(false);

                return declarations.SelectAsArray(d => d.Symbol);
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
    }
}
