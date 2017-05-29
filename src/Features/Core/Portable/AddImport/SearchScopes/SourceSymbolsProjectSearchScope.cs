// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        /// <summary>
        /// SearchScope used for searching *only* the source symbols contained within a project/compilation.
        /// i.e. symbols from metadata will not be searched.
        /// </summary>
        private class SourceSymbolsProjectSearchScope : ProjectSearchScope
        {
            private readonly ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> _projectToAssembly;

            public SourceSymbolsProjectSearchScope(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
                Project project, bool ignoreCase, CancellationToken cancellationToken)
                : base(provider, project, ignoreCase, cancellationToken)
            {
                _projectToAssembly = projectToAssembly;
            }

            protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                SymbolFilter filter, SearchQuery searchQuery)
            {
                var result = await AddImportSymbolTreeInfoService.TryFindSourceSymbolsAsync(
                    _project, filter, searchQuery, _projectToAssembly, CancellationToken).ConfigureAwait(false);

                return result.SelectAsArray(s => s.Symbol);
            }
        }
    }
}