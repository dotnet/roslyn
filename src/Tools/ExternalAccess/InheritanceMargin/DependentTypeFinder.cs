// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.InheritanceMargin
{
    public static class DependentTypeFinder
    {
        public static async Task<ImmutableArray<INamedTypeSymbol>> FindImmediatelyDerivedAndImplementingTypesAsync(INamedTypeSymbol type, Solution solution, CancellationToken cancellationToken)
        {
            var types = await FindSymbols.DependentTypeFinder.FindImmediatelyDerivedAndImplementingTypesAsync(type, solution, cancellationToken).ConfigureAwait(false);
            return types.SelectAsArray(symbolAndProjectId => symbolAndProjectId.Symbol);
        }
    }
}
