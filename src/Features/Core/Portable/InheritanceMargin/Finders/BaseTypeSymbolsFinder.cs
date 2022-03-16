// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.InheritanceMargin.InheritanceMarginServiceHelper;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class BaseTypeSymbolsFinder : InheritanceSymbolsFinder
    {
        protected override Task<ImmutableArray<ISymbol>> GetUpSymbolsAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
            => Task.FromResult(BaseTypeFinder.FindBaseTypesAndInterfaces((INamedTypeSymbol)symbol).CastArray<ISymbol>());

        protected override Task<ImmutableArray<ISymbol>> GetDownSymbolsAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken) => throw ExceptionUtilities.Unreachable;

        public async Task<(ImmutableArray<SymbolGroup> baseTypes, ImmutableArray<SymbolGroup> baseInterfaces)> GetBaseTypeAndBaseInterfaceSymbolGroupsAsync(
            ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetUpSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);
            using var _1 = ArrayBuilder<SymbolGroup>.GetInstance(out var baseTypesBuilder);
            using var _2 = ArrayBuilder<SymbolGroup>.GetInstance(out var baseInterfacesBuilder);
            foreach (var (symbol, symbolGroup) in builder)
            {
                if (!symbol.IsErrorType()
                    && IsNavigableSymbol(symbol)
                    && symbol is INamedTypeSymbol namedTypeSymbol
                    && namedTypeSymbol.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum))
                {
                    if (symbol.IsInterfaceType())
                    {
                        baseInterfacesBuilder.Add(symbolGroup);
                    }
                    else
                    {
                        Debug.Assert(symbol is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct });
                        baseTypesBuilder.Add(symbolGroup);
                    }
                }
            }

            return (baseTypesBuilder.ToImmutable(), baseInterfacesBuilder.ToImmutable());
        }
    }
}
