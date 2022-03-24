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
        public static readonly BaseTypeSymbolsFinder Instance = new();

        protected override Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
            => Task.FromResult(BaseTypeFinder.FindBaseTypesAndInterfaces((INamedTypeSymbol)symbol).CastArray<ISymbol>());

        public async Task<(ImmutableArray<SymbolGroup> baseTypes, ImmutableArray<SymbolGroup> baseInterfaces)> GetBaseTypeAndBaseInterfaceSymbolGroupsAsync(
            ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);
            using var _1 = ArrayBuilder<SymbolGroup>.GetInstance(out var baseTypesBuilder);
            using var _2 = ArrayBuilder<SymbolGroup>.GetInstance(out var baseInterfacesBuilder);
            foreach (var (symbol, symbolGroup) in builder)
            {
                // Filter out
                // 1. System.Object. (otherwise margin would be shown for all classes)
                // 2. System.ValueType. (otherwise margin would be shown for all structs)
                // 3. System.Enum. (otherwise margin would be shown for all enum)
                // 4. Error type.
                // For example, if user has code like this,
                // class Bar : ISomethingIsNotDone { }
                // The interface has not been declared yet, so don't show this error type to user.
                if (!symbol.IsErrorType()
                    && IsNavigableSymbol(symbol)
                    && symbol is INamedTypeSymbol { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum) })
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
