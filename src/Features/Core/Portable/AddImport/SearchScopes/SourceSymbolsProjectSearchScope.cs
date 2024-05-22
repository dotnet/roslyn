// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    /// <summary>
    /// SearchScope used for searching *only* the source symbols contained within a project/compilation.
    /// i.e. symbols from metadata will not be searched.
    /// </summary>
    private class SourceSymbolsProjectSearchScope(
        AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
        ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol?>> projectToAssembly,
        Project project, bool ignoreCase) : ProjectSearchScope(provider, project, ignoreCase)
    {
        private readonly ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol?>> _projectToAssembly = projectToAssembly;

        protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
            SymbolFilter filter, SearchQuery searchQuery, CancellationToken cancellationToken)
        {
            var service = _project.Solution.Services.GetRequiredService<ISymbolTreeInfoCacheService>();
            var info = await service.TryGetPotentiallyStaleSourceSymbolTreeInfoAsync(_project, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                // Looks like there was nothing in the cache.  Return no results for now.
                return [];
            }

            // Don't create the assembly until it is actually needed by the SymbolTreeInfo.FindAsync
            // code.  Creating the assembly can be costly and we want to avoid it until it is actually
            // needed.
            var lazyAssembly = _projectToAssembly.GetOrAdd(_project, CreateLazyAssembly);

            var declarations = await info.FindAsync(
                searchQuery, lazyAssembly, filter, cancellationToken).ConfigureAwait(false);

            return declarations;

            static AsyncLazy<IAssemblySymbol?> CreateLazyAssembly(Project project)
                => AsyncLazy.Create(static async (project, c) =>
                       {
                           var compilation = await project.GetRequiredCompilationAsync(c).ConfigureAwait(false);
                           return (IAssemblySymbol?)compilation.Assembly;
                       },
                       arg: project);
        }
    }
}
