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
    /// Returns the symbols that are the same symbol as <paramref name="symbol"/> to the user: its copies in linked
    /// files (including <paramref name="symbol"/> itself), and the other part of each copy's partial member.
    /// </summary>
    private static async Task<ImmutableArray<ISymbol>> FindSameSymbolsAsync(
        ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        return [.. linkedSymbols, .. linkedSymbols.Select(GetOtherPartialPart).WhereNotNull()];
    }

    /// <summary>
    /// Whether <paramref name="symbol"/> is on the implementation part of a partial member and has the same name as
    /// the corresponding symbol on the definition part, so that searching that symbol also finds its uses.
    /// </summary>
    private static bool IsFoundThroughPartialDefinitionPart(ISymbol symbol)
        => IsOnPartialImplementationPart(symbol) && GetOtherPartialPart(symbol) is { } definition && definition.Name == symbol.Name;

    /// <summary>
    /// Returns the symbol corresponding to <paramref name="symbol"/> on the other part of its partial member, or <see
    /// langword="null"/> if <paramref name="symbol"/> isn't a partial member, or a parameter or type parameter of one.
    /// </summary>
    private static ISymbol? GetOtherPartialPart(ISymbol symbol)
        => symbol switch
        {
            IMethodSymbol method => (ISymbol?)method.PartialDefinitionPart ?? method.PartialImplementationPart,
            IPropertySymbol property => (ISymbol?)property.PartialDefinitionPart ?? property.PartialImplementationPart,
            IEventSymbol @event => (ISymbol?)@event.PartialDefinitionPart ?? @event.PartialImplementationPart,
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
