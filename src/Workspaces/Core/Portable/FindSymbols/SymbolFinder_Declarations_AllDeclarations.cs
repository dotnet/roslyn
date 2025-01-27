// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols;

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
        return declarations;
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
        return declarations;
    }
}
