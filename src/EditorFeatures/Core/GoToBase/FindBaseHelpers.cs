// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;

namespace Microsoft.CodeAnalysis.Editor.GoToBase
{
    internal static class FindBaseHelpers
    {
        public static async Task<(ISymbol symbol, ImmutableArray<SymbolAndProjectId> implementations, string message)?> FindBasesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
            {
                return null;
            }

            var symbol = symbolAndProject.Value.symbol;
            var project = symbolAndProject.Value.project;

            var bases = await FindBasesWorkerAsync(symbol, project, cancellationToken).ConfigureAwait(false);
            var filteredSymbols = bases.WhereAsArray(s => s.Symbol.Locations.Any(l => l.IsInSource) || s.ProjectId is null);
            var message = filteredSymbols.Length == 0 ? EditorFeaturesResources.The_symbol_has_no_base : null;

            return (symbol, filteredSymbols, message);
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> FindBasesWorkerAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol &&
                (namedTypeSymbol.TypeKind == TypeKind.Class || namedTypeSymbol.TypeKind == TypeKind.Interface || namedTypeSymbol.TypeKind == TypeKind.Struct))
            {
                return (await BaseTypeFinder.FindBaseTypesAndInterfacesAsync(namedTypeSymbol, project, cancellationToken).ConfigureAwait(false));
            }
            else if (symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Method || symbol.Kind == SymbolKind.Event)
            {
                return await BaseTypeFinder.FindOverriddenAndImplementedMembersAsync(symbol, project, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return (ImmutableArray<SymbolAndProjectId>.Empty);
            }
        }
    }
}
