// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        /// <summary>
        /// Return all the base interfaces and base types of <param name="symbol"/> in topological order.
        /// e.g
        /// 'class A : B { }'
        /// 'class B : IB { }'
        /// 'interface IB : IC { }'
        /// If 'class A' is the input symbol, the result should be in the order like: 'B', 'IB', 'IC'.
        /// </summary>
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

            // If the symbol has base type, it means we need to topologically sort all the base interfaces and base types.
            // Consider the all the base types and base interfaces as vertices, and each of them is pointed by its derived types/interfaces in the graph.
            // We need an 'incomingSymbols' map, whose key is the vertice of the graph,
            // the values is its derived types and interfaces to perform topologically sort.
            // e.g.
            // interface IA { }
            // interface IB { }
            // class A : IA { }
            // class B : A, IB, IA { }
            // 
            // The map would be
            // {
            //     "IA": ["A", "B"],
            //     "IB": ["B"],
            //     "A": ["B"],
            //     "B": []
            // }
            using var _ = GetPooledHashSetDictionary(out var incomingSymbolsMapBuilder);
            var baseTypes = BaseTypeFinder.FindBaseTypes(namedTypeSymbol);

            // Add the entry for all base types.
            for (var i = 0; i < baseTypes.Length; i++)
            {
                // baseTypes are order like,
                //                        [0]          [1]           [2]
                // symbol_we_search -> baseClass1 -> baseClass2 -> baseClass3 ...
                // There is no edge points to the 'baseClass1'.
                // And since interface can't have base class, the item before a baseClass is just the previous item in the array.
                // e.g. 'baseClass2' could only be pointed by 'baseClass1'.
                var baseType = baseTypes[i];
                var incomingSymbolsSetForBaseType = s_symbolHashSetPool.Allocate();
                incomingSymbolsMapBuilder[baseType] = incomingSymbolsSetForBaseType;
                if (i > 0)
                {
                    incomingSymbolsSetForBaseType.Add(baseTypes[i - 1]);
                }

                // Then for the interfaces of this base type, add a set containing this base type
                foreach (var baseInterface in baseType.Interfaces)
                {
                    if (incomingSymbolsMapBuilder.TryGetValue(baseInterface, out var indegreeSymbols))
                    {
                        indegreeSymbols.Add(baseType);
                    }
                    else
                    {
                        var incomingSymbolSetForBaseInterface = s_symbolHashSetPool.Allocate();
                        incomingSymbolSetForBaseInterface.Add(baseType);
                        incomingSymbolsMapBuilder[baseInterface] = incomingSymbolSetForBaseInterface;
                    }
                }
            }

            foreach (var baseInterface in namedTypeSymbol.AllInterfaces)
            {
                if (!incomingSymbolsMapBuilder.ContainsKey(baseInterface))
                {
                    incomingSymbolsMapBuilder[baseInterface] = s_symbolHashSetPool.Allocate();
                }

                // For all the interfaces of this interface, add a set containing this interface.
                foreach (var @interface in baseInterface.Interfaces)
                {
                    if (incomingSymbolsMapBuilder.TryGetValue(@interface, out var indegreeSymbols))
                    {
                        indegreeSymbols.Add(baseInterface);
                    }
                    else
                    {
                        var incomingSymbolSetForBaseInterface = s_symbolHashSetPool.Allocate();
                        incomingSymbolSetForBaseInterface.Add(baseInterface);
                        incomingSymbolsMapBuilder[@interface] = incomingSymbolSetForBaseInterface;
                    }
                }
            }

            // Topological sort all the base type and base interface
            return Task.FromResult(TopologicalSortAsArray(
                baseTypes.AddRange(namedTypeSymbol.AllInterfaces).CastArray<ISymbol>(),
                incomingSymbolsMapBuilder));
        }

        public async Task<(ImmutableArray<SymbolGroup> baseTypes, ImmutableArray<SymbolGroup> baseInterfaces)> GetBaseTypeAndBaseInterfaceSymbolGroupsAsync(
            ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _1 = GetPooledHashSetDictionary(out var builder);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);
            using var _2 = ArrayBuilder<SymbolGroup>.GetInstance(out var baseTypesBuilder);
            using var _3 = ArrayBuilder<SymbolGroup>.GetInstance(out var baseInterfacesBuilder);
            foreach (var (symbol, symbolSet) in builder)
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
                        baseInterfacesBuilder.Add(new SymbolGroup(symbolSet));
                    }
                    else
                    {
                        Debug.Assert(symbol is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct });
                        baseTypesBuilder.Add(new SymbolGroup(symbolSet));
                    }
                }
            }

            return (baseTypesBuilder.ToImmutable(), baseInterfacesBuilder.ToImmutable());
        }
    }
}
