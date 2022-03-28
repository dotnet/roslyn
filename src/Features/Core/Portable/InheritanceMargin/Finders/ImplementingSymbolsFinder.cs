// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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

        protected override async Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var implementingSymbols = await InheritanceMarginServiceHelper.GetImplementingSymbolsForTypeMemberAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
            using var _ = PooledDictionary<ISymbol, HashSet<ISymbol>>.GetInstance(out var indegreeSymbolsMapBuilder);
            foreach (var implementingSymbol in implementingSymbols)
            {
                if (!indegreeSymbolsMapBuilder.ContainsKey(implementingSymbol))
                {
                    var indegreeSymbols = new HashSet<ISymbol>();

                    var overriddenMember = implementingSymbol.GetOverriddenMember();
                    if (overriddenMember != null && implementingSymbols.Contains(overriddenMember))
                    {
                        indegreeSymbols.Add(overriddenMember);
                    }

                    foreach (var implementedInterfaceMember in implementingSymbol.ExplicitOrImplicitInterfaceImplementations())
                    {
                        if (implementingSymbols.Contains(implementedInterfaceMember))
                        {
                            indegreeSymbols.Add(implementedInterfaceMember);
                        }
                    }

                    indegreeSymbolsMapBuilder[implementingSymbol] = indegreeSymbols;
                }
            }

            return TopologicalSortAsArray(implementingSymbols, indegreeSymbolsMapBuilder.ToImmutableDictionary());
        }

        public async Task<ImmutableArray<SymbolGroup>> GetImplementingSymbolsGroupAsync(ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            RoslynDebug.Assert(initialSymbol.ContainingSymbol.IsInterfaceType());
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<SymbolGroup>.GetInstance(out var implementingSymbolGroupBuilder);
            foreach (var (symbol, symbolGroup) in builder)
            {
                if (symbol.Locations.Any(l => l.IsInSource))
                    implementingSymbolGroupBuilder.Add(symbolGroup);
            }

            return implementingSymbolGroupBuilder.ToImmutable();
        }
    }
}
