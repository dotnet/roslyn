// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default)
        {
            using var query = SearchQuery.Create(name, ignoreCase);
            var declarations = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                project, query, SymbolFilter.All, cancellationToken).ConfigureAwait(false);
            return declarations.SelectAsArray(t => t.Symbol);
        }

        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default)
        {
            using var query = SearchQuery.Create(name, ignoreCase);
            var declarations = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                project, query, filter, cancellationToken).ConfigureAwait(false);
            return declarations.SelectAsArray(t => t.Symbol);
        }
    }
}
