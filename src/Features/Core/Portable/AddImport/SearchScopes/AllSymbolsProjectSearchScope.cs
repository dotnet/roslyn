// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                bool exact,
                CancellationToken cancellationToken)
                : base(provider, project, exact, cancellationToken)
            {
            }

            protected override async Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                SymbolFilter filter, SearchQuery searchQuery)
            {
                var declarations = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                    _project, searchQuery, filter, CancellationToken).ConfigureAwait(false);

                return declarations.SelectAsArray(d => d.Symbol);
            }
        }
    }
}
