// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.TypeHierarchy;

internal abstract class AbstractTypeHierarchyService : ITypeHierarchyService
{
    public ImmutableArray<INamedTypeSymbol> GetBaseTypesAndInterfaces(INamedTypeSymbol typeSymbol)
        => BaseTypeFinder.FindBaseTypesAndInterfaces(typeSymbol);

    public async Task<ImmutableArray<INamedTypeSymbol>> GetDerivedTypesAndImplementationsAsync(
        Solution solution,
        INamedTypeSymbol typeSymbol,
        bool transitive,
        CancellationToken cancellationToken)
    {
        if (typeSymbol.IsInterfaceType())
        {
            var allDerivedInterfaces = await SymbolFinder.FindDerivedInterfacesArrayAsync(
                typeSymbol,
                solution,
                transitive: transitive,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var allImplementations = await SymbolFinder.FindImplementationsArrayAsync(
                typeSymbol,
                solution,
                transitive: transitive,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return [.. allDerivedInterfaces, .. allImplementations];
        }

        return await SymbolFinder.FindDerivedClassesArrayAsync(
            typeSymbol,
            solution,
            transitive: transitive,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
