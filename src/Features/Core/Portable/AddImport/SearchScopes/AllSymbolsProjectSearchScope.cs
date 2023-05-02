// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        /// <summary>
        /// SearchScope used for searching *all* the symbols contained within a project/compilation.
        /// i.e. the symbols created from source *and* symbols from references (both project and
        /// metadata).
        /// </summary>
        private class AllSymbolsProjectSearchScope : ProjectSearchScope
        {
            public AllSymbolsProjectSearchScope(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                Project project,
                bool exact)
                : base(provider, project, exact)
            {
            }

            protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                SymbolFilter filter, SearchQuery searchQuery, CancellationToken cancellationToken)
            {
                var declarations = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                    _project, searchQuery, filter, cancellationToken).ConfigureAwait(false);

                return declarations;
            }
        }
    }
}
