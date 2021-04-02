// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal partial class AbstractInheritanceMarginService
    {
        private static async Task<InheritanceMarginItem> CreateInheritanceMemberItemAsync(
            Solution solution,
            INamedTypeSymbol memberSymbol,
            int lineNumber,
            ImmutableArray<ISymbol> baseSymbols,
            ImmutableArray<ISymbol> derivedTypesSymbols,
            CancellationToken cancellationToken)
        {
            var baseSymbolItems = await baseSymbols
                .SelectAsArrayAsync((symbol, _) => CreateInheritanceItemAsync(solution, symbol, InheritanceRelationship.Implementing, cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            var derivedTypeItems = await derivedTypesSymbols
                .SelectAsArrayAsync((symbol, _) => CreateInheritanceItemAsync(solution, symbol, InheritanceRelationship.Implemented, cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            var definitionItem = await memberSymbol.ToClassifiedDefinitionItemAsync(
                solution,
                isPrimary: true,
                includeHiddenLocations: false,
                FindReferencesSearchOptions.Default,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new InheritanceMarginItem(
                lineNumber,
                definitionItem.DisplayParts,
                memberSymbol.GetGlyph(),
                baseSymbolItems.Concat(derivedTypeItems));
        }

        private static async ValueTask<InheritanceTargetItem> CreateInheritanceItemAsync(
            Solution solution,
            ISymbol targetSymbol,
            InheritanceRelationship inheritanceRelationship,
            CancellationToken cancellationToken)
        {
            // Right now the targets are not shown in a classified way.
            var definition = await targetSymbol.ToNonClassifiedDefinitionItemAsync(
                solution,
                includeHiddenLocations: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var containingSymbol = targetSymbol.ContainingSymbol;
            var containingSymbolName = containingSymbol is INamespaceSymbol { IsGlobalNamespace: true }
                ? FeaturesResources.Global_Namespace
                : containingSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            return new InheritanceTargetItem(
                inheritanceRelationship,
                definition,
                targetSymbol.GetGlyph(),
                containingSymbolName);
        }

        private static async Task<InheritanceMarginItem> CreateInheritanceMemberInfoForMemberAsync(
            Solution solution,
            ISymbol memberSymbol,
            int lineNumber,
            ImmutableArray<ISymbol> implementingMembers,
            ImmutableArray<ISymbol> implementedMembers,
            ImmutableArray<ISymbol> overridenMembers,
            ImmutableArray<ISymbol> overridingMembers,
            CancellationToken cancellationToken)
        {
            var implementingMemberItems = await implementingMembers
                .SelectAsArrayAsync((symbol, _) => CreateInheritanceItemAsync(solution, symbol, InheritanceRelationship.Implementing, cancellationToken), cancellationToken).ConfigureAwait(false);
            var implementedMemberItems = await implementedMembers
                .SelectAsArrayAsync((symbol, _) => CreateInheritanceItemAsync(solution, symbol, InheritanceRelationship.Implemented, cancellationToken), cancellationToken).ConfigureAwait(false);
            var overridenMemberItems = await overridenMembers
                .SelectAsArrayAsync((symbol, _) => CreateInheritanceItemAsync(solution, symbol, InheritanceRelationship.Overridden, cancellationToken), cancellationToken).ConfigureAwait(false);
            var overridingMemberItems = await overridingMembers
                .SelectAsArrayAsync((symbol, _) => CreateInheritanceItemAsync(solution, symbol, InheritanceRelationship.Overriding, cancellationToken), cancellationToken).ConfigureAwait(false);

            var definitionItem = await memberSymbol.ToClassifiedDefinitionItemAsync(
                solution,
                isPrimary: true,
                includeHiddenLocations: false,
                FindReferencesSearchOptions.Default,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new InheritanceMarginItem(
                lineNumber,
                definitionItem.DisplayParts,
                memberSymbol.GetGlyph(),
                implementingMemberItems.Concat(implementedMemberItems)
                    .Concat(overridenMemberItems)
                    .Concat(overridingMemberItems));
        }

        /// <summary>
        /// For the <param name="memberSymbol"/>, get all the implemented symbols.
        /// Table for the mapping between images and inheritanceRelationship
        /// Implemented : I↓
        /// Implementing : I↑
        /// Overridden: O↓
        /// Overriding: O↑
        /// </summary>
        private static async Task<ImmutableArray<ISymbol>> GetImplementedSymbolsAsync(
            Solution solution,
            ISymbol memberSymbol,
            CancellationToken cancellationToken)
        {
            if (memberSymbol is INamedTypeSymbol { IsSealed: false } namedTypeSymbol)
            {
                var derivedTypes = await GetDerivedTypesAndImplementationsAsync(solution, namedTypeSymbol, cancellationToken).ConfigureAwait(false);
                return derivedTypes.CastArray<ISymbol>();
            }

            if (memberSymbol is IMethodSymbol or IEventSymbol or IPropertySymbol
                 && memberSymbol.ContainingSymbol.IsInterfaceType())
            {
                return await SymbolFinder.FindMemberImplementationsArrayAsync(
                    memberSymbol,
                    solution,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        /// <summary>
        /// Get members overriding the <param name="memberSymbol"/>
        /// Table for the mapping between images and inheritanceRelationship
        /// Implemented : I↓
        /// Implementing : I↑
        /// Overridden: O↓
        /// Overriding: O↑
        /// </summary>
        private static ImmutableArray<ISymbol> GetOverridingSymbols(ISymbol memberSymbol)
        {
            if (memberSymbol is INamedTypeSymbol)
            {
                return ImmutableArray<ISymbol>.Empty;
            }
            else
            {
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
                for (var overridenMember = memberSymbol.GetOverriddenMember();
                    overridenMember != null;
                    overridenMember = overridenMember.GetOverriddenMember())
                {
                    builder.Add(overridenMember.OriginalDefinition);
                }

                return builder.ToImmutableArray();
            }
        }

        /// <summary>
        /// Get the derived interfaces and derived classes for <param name="typeSymbol"/>.
        /// </summary>
        private static async Task<ImmutableArray<INamedTypeSymbol>> GetDerivedTypesAndImplementationsAsync(
            Solution solution,
            INamedTypeSymbol typeSymbol,
            CancellationToken cancellationToken)
        {
            if (typeSymbol.IsInterfaceType())
            {
                var allDerivedInterfaces = await SymbolFinder.FindDerivedInterfacesArrayAsync(
                    typeSymbol,
                    solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var allImplementations = await SymbolFinder.FindImplementationsArrayAsync(
                    typeSymbol,
                    solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return allDerivedInterfaces.Concat(allImplementations);
            }
            else
            {
                return await SymbolFinder.FindDerivedClassesArrayAsync(
                    typeSymbol,
                    solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
