// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal static class SymbolFinderInternal
{
    /// <inheritdoc cref="SymbolFinder.FindSourceDefinitionAsync"/>
    internal static ValueTask<ISymbol?> FindSourceDefinitionAsync(
        ISymbol? symbol, Solution solution, CancellationToken cancellationToken)
    {
        if (symbol != null)
        {
            symbol = symbol.GetOriginalUnreducedDefinition();
            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Local:
                case SymbolKind.NamedType:
                case SymbolKind.Parameter:
                case SymbolKind.Property:
                case SymbolKind.TypeParameter:
                case SymbolKind.Namespace:
                    return FindSourceDefinitionWorkerAsync(symbol, solution, cancellationToken);
            }
        }

        return new ValueTask<ISymbol?>(result: null);
    }

    private static async ValueTask<ISymbol?> FindSourceDefinitionWorkerAsync(
        ISymbol symbol,
        Solution solution,
        CancellationToken cancellationToken)
    {
        // If it's already in source, then we might already be done
        if (InSource(symbol))
        {
            // If our symbol doesn't have a containing assembly, there's nothing better we can do to map this
            // symbol somewhere else. The common case for this is a merged INamespaceSymbol that spans assemblies.
            if (symbol.ContainingAssembly == null)
                return symbol;

            // Just because it's a source symbol doesn't mean we have the final symbol we actually want. In retargeting cases,
            // the retargeted symbol is from "source" but isn't equal to the actual source definition in the other project. Thus,
            // we only want to return symbols from source here if it actually came from a project's compilation's assembly. If it isn't
            // then we have a retargeting scenario and want to take our usual path below as if it was a metadata reference
            foreach (var sourceProject in solution.Projects)
            {
                // If our symbol is actually a "regular" source symbol, then we know the compilation is holding the symbol alive
                // and thus TryGetCompilation is sufficient. For another example of this pattern, see Solution.GetProject(IAssemblySymbol)
                // which we happen to call below.
                if (sourceProject.TryGetCompilation(out var compilation) &&
                    symbol.ContainingAssembly.Equals(compilation.Assembly))
                {
                    return symbol;
                }
            }
        }
        else if (!symbol.Locations.Any(static loc => loc.IsInMetadata))
        {
            // We have a symbol that's neither in source nor metadata
            return null;
        }

        var project = solution.GetProject(symbol.ContainingAssembly, cancellationToken);

        if (project is not null)
        {
            var projectCompilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbolId = symbol.GetSymbolKey(cancellationToken);
            var result = symbolId.Resolve(projectCompilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken);

            return InSource(result.Symbol) ? result.Symbol : result.CandidateSymbols.FirstOrDefault(InSource);
        }

        return null;

        static bool InSource([NotNullWhen(true)] ISymbol? symbol)
           => symbol != null && symbol.Locations.Any(static loc => loc.IsInSource);
    }
}
