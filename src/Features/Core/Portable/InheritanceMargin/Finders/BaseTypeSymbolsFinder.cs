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

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class BaseTypeSymbolsFinder : InheritanceSymbolsFinder
    {
        public static readonly BaseTypeSymbolsFinder Instance = new();

        protected override Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var namedTypeSymbol = (INamedTypeSymbol)symbol;
            if (namedTypeSymbol.BaseType == null)
            {
                // AllInterfaces are topologically sorted by default.
                return Task.FromResult(namedTypeSymbol.AllInterfaces.CastArray<ISymbol>());
            }

            // Calculate indegree for all the symbols
            using var _ = PooledDictionary<ISymbol, HashSet<ISymbol>>.GetInstance(out var indegreeSymbolsMapBuilder);
            var baseTypes = BaseTypeFinder.FindBaseTypes(namedTypeSymbol);
            for (var i = 0; i < baseTypes.Length; i++)
            {
                // baseTypes are order like,
                //                        [0]          [1]           [2]
                // symbol_we_search -> baseClass1 -> baseClass2 -> baseClass3 ...
                // There is no edge points to the 'baseClass1'.
                // And since interface can't have base class, the item before a baseClass is just the previous item in the array.
                // e.g. 'baseClass2' could only be pointed by 'baseClass1'.
                var baseType = baseTypes[i];
                if (i == 0)
                {
                    indegreeSymbolsMapBuilder[baseType] = new HashSet<ISymbol>();
                }
                else
                {
                    indegreeSymbolsMapBuilder[baseType] = new HashSet<ISymbol>() { baseTypes[i - 1] };
                }

                foreach (var baseInterface in baseType.Interfaces)
                {
                    if (indegreeSymbolsMapBuilder.TryGetValue(baseInterface, out var indegreeSymbols))
                    {
                        indegreeSymbols.Add(baseType);
                    }
                    else
                    {
                        indegreeSymbolsMapBuilder[baseInterface] = new HashSet<ISymbol>() { baseType };
                    }
                }
            }

            foreach (var baseInterface in namedTypeSymbol.AllInterfaces)
            {
                if (!indegreeSymbolsMapBuilder.ContainsKey(baseInterface))
                {
                    indegreeSymbolsMapBuilder[baseInterface] = new HashSet<ISymbol>();
                }

                foreach (var @interface in baseInterface.Interfaces)
                {
                    if (indegreeSymbolsMapBuilder.TryGetValue(@interface, out var indegreeSymbols))
                    {
                        indegreeSymbols.Add(baseInterface);
                    }
                    else
                    {
                        indegreeSymbolsMapBuilder[@interface] = new HashSet<ISymbol>() { baseInterface };
                    }
                }
            }

            return Task.FromResult(TopologicalSortAsArray(
                baseTypes.AddRange(namedTypeSymbol.AllInterfaces).CastArray<ISymbol>(),
                indegreeSymbolsMapBuilder.ToImmutableDictionary()));
        }

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
