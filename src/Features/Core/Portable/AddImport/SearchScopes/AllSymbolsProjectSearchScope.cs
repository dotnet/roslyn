// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        /// <summary>
        /// SearchScope used for searching *all* the symbols contained within a project/compilation.
        /// i.e. the symbols created from source *and* symbols from references (both project and
        /// metadata).
        /// </summary>
        private class AllSymbolsProjectSearchScope : ProjectSearchScope
        {
            public AllSymbolsProjectSearchScope(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                Project project,
                bool exact,
                CancellationToken cancellationToken)
                : base(provider, project, exact, cancellationToken)
            {
            }

            protected override Task<ImmutableArray<ISymbol>> FindDeclarationsAsync(
                string name, SymbolFilter filter, SearchQuery searchQuery)
            {
                return SymbolFinder.FindAllDeclarationsWithNormalQueryAsync(_project, searchQuery, filter, CancellationToken);
            }
        }
    }
}