// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.TypeHierarchy;

internal interface ITypeHierarchyService : ILanguageService
{
    ImmutableArray<INamedTypeSymbol> GetBaseTypesAndInterfaces(INamedTypeSymbol typeSymbol, bool transitive);

    Task<ImmutableArray<INamedTypeSymbol>> GetDerivedTypesAndImplementationsAsync(
        Solution solution,
        INamedTypeSymbol typeSymbol,
        bool transitive,
        CancellationToken cancellationToken);
}
