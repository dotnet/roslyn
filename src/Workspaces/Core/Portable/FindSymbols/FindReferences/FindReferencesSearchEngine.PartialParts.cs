// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <remarks>
/// The parts of a partial member are one symbol to the user, like the copies of a symbol in linked files: both parts
/// are reported in the same <see cref="SymbolGroup"/>.  A search of either part finds the uses of both, as <see
/// cref="SymbolFinder.OriginalSymbolsMatch"/> does not distinguish partial parts, so when cascading only the definition
/// part is searched.  A parameter or type parameter named differently in the two parts is the exception: its uses in
/// the implementation part are spelled with the other name, so both parts are searched.
/// </remarks>
internal sealed partial class FindReferencesSearchEngine
{
    /// <summary>
    /// Returns the symbols that Find References reports as one definition with <paramref name="symbol"/>, including
    /// <paramref name="symbol"/> itself.  A use can bind to any of them: each project that compiles a linked file binds
    /// to its own copy of the symbol, and a use of a partial member binds to either part, such as a call to the
    /// definition part or a parameter reference in the implementation part's body.
    /// </summary>
    private static async Task<ImmutableArray<ISymbol>> FindSameSymbolsAsync(
        ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        return [.. linkedSymbols, .. linkedSymbols.Select(GetOtherPartialPart).WhereNotNull()];
    }

    /// <summary>
    /// Whether <paramref name="symbol"/> doesn't need to be searched, because searching the corresponding symbol on
    /// the definition part of its partial member already finds its uses.  Searching it as well would report each of
    /// its uses twice.
    /// </summary>
    private static bool IsFoundThroughPartialDefinitionPart(ISymbol symbol)
    {
        if (!IsOnPartialImplementationPart(symbol))
            return false;

        return symbol switch
        {
            // A search looks for tokens spelled with the searched symbol's name, and OriginalSymbolsMatch ignores
            // partial parts but compares names.  So searching the definition part's parameter or type parameter finds
            // the implementation part's uses only when both parts use the same name.  They can differ (CS8826), and
            // then the implementation part's uses are only found by searching it too.
            IParameterSymbol or ITypeParameterSymbol
                => GetOtherPartialPart(symbol) is { } definition && definition.Name == symbol.Name,

            // Both parts of a partial member have the member's name, and OriginalSymbolsMatch ignores partial parts, so
            // searching the definition part finds the uses of both.
            _ => true,
        };
    }

    /// <summary>
    /// Returns the symbol corresponding to <paramref name="symbol"/> on the other part of its partial member, or <see
    /// langword="null"/> if <paramref name="symbol"/> isn't a partial member, or a parameter or type parameter of one.
    /// </summary>
    private static ISymbol? GetOtherPartialPart(ISymbol symbol)
        => symbol switch
        {
            IMethodSymbol method => method.PartialDefinitionPart ?? method.PartialImplementationPart,
            IPropertySymbol property => property.PartialDefinitionPart ?? property.PartialImplementationPart,
            IEventSymbol @event => @event.PartialDefinitionPart ?? @event.PartialImplementationPart,
            IParameterSymbol parameter => GetOtherPartialPart(parameter.ContainingSymbol) switch
            {
                IMethodSymbol method => ElementAtOrNull(method.Parameters, parameter.Ordinal),
                IPropertySymbol property => ElementAtOrNull(property.Parameters, parameter.Ordinal),
                _ => null,
            },
            ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } typeParameter
                => GetOtherPartialPart(typeParameter.ContainingSymbol) is IMethodSymbol method
                    ? ElementAtOrNull(method.TypeParameters, typeParameter.Ordinal)
                    : null,
            _ => null,
        };

    private static bool IsOnPartialImplementationPart(ISymbol symbol)
        => symbol switch
        {
            IMethodSymbol method => method.PartialDefinitionPart != null,
            IPropertySymbol property => property.PartialDefinitionPart != null,
            IEventSymbol @event => @event.PartialDefinitionPart != null,
            IParameterSymbol parameter => IsOnPartialImplementationPart(parameter.ContainingSymbol),
            ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } typeParameter => IsOnPartialImplementationPart(typeParameter.ContainingSymbol),
            _ => false,
        };

    private static T? ElementAtOrNull<T>(ImmutableArray<T> array, int index) where T : class
        => (uint)index < (uint)array.Length ? array[index] : null;
}
