// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class ImplementingSymbolsFinder : InheritanceSymbolsFinder
    {
        public static readonly ImplementingSymbolsFinder Instance = new();

        /// <summary>
        /// Get all the implementing members in derived types for <param name="symbol"/> in topological order.
        /// </summary>
        protected override async Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var implementingSymbols = await InheritanceMarginServiceHelper.GetImplementingSymbolsForTypeMemberAsync(solution, symbol, cancellationToken).ConfigureAwait(false);

            // ImplementingSymbols contains
            // 1. The directly implemeting members's symbol for a interface member.
            // 2. The overriding symbols of implementing members.
            // Consider each of the symbols as a vertice, and it would be pointed by its overridden member or implemented members in interfaces.
            // We need an 'IncomingSymbolMap' whose key is the vertice, and value is a set of the overridden member and implemented members in interfaces to perform topological sort
            // e.g 
            // interface IBar { void Sub(); }
            // class Bar : IBar { public virtual void Sub(); }
            // class Bar2 : Bar, IBar { public overridden void Sub() { } }
            // The map would looks like
            // {
            //     "IBar.Sub()" : [],
            //     "Bar.Sub()" : ["IBar.Sub()"],
            //     "Bar2.Sub()" : ["IBar.Sub()", "Bar.Sub()"],
            // }
            using var _ = GetPooledHashSetDictionary(out var incomingSymbolsMap);
            foreach (var implementingSymbol in implementingSymbols)
            {
                if (!incomingSymbolsMap.ContainsKey(implementingSymbol))
                {
                    var indegreeSymbols = s_symbolHashSetPool.Allocate();

                    // 1. If this symbol has an overridden member, and it is in implemeting symbols list.
                    // It means overriden member point to this symbol.
                    var overriddenMember = implementingSymbol.GetOverriddenMember();
                    if (overriddenMember != null && implementingSymbols.Contains(overriddenMember))
                    {
                        indegreeSymbols.Add(overriddenMember);
                    }

                    // 2. Also check all the implemented interface member. If the i
                    foreach (var implementedInterfaceMember in implementingSymbol.ExplicitOrImplicitInterfaceImplementations())
                    {
                        if (implementingSymbols.Contains(implementedInterfaceMember))
                        {
                            indegreeSymbols.Add(implementedInterfaceMember);
                        }
                    }

                    incomingSymbolsMap[implementingSymbol] = indegreeSymbols;
                }
            }

            return TopologicalSortAsArray(implementingSymbols, incomingSymbolsMap);
        }

        public async Task<ImmutableArray<SymbolGroup>> GetImplementingSymbolsGroupAsync(ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            RoslynDebug.Assert(initialSymbol.ContainingSymbol.IsInterfaceType());
            using var _1 = GetPooledHashSetDictionary(out var builder);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _2 = ArrayBuilder<SymbolGroup>.GetInstance(out var implementingSymbolGroupBuilder);
            foreach (var (symbol, symbolSet) in builder)
            {
                if (symbol.Locations.Any(l => l.IsInSource))
                    implementingSymbolGroupBuilder.Add(new SymbolGroup(symbolSet));
            }

            return implementingSymbolGroupBuilder.ToImmutable();
        }
    }
}
