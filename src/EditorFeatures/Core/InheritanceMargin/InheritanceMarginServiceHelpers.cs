// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal static class InheritanceMarginServiceHelper
    {
        public static async ValueTask<ImmutableArray<SerializableInheritanceMarginItem>> GetInheritanceMemberItemAsync(
            Solution solution,
            ProjectId projectId,
            ImmutableArray<(SymbolKey symbolKey, int lineNumber)> symbolKeyAndLineNumbers,
            CancellationToken cancellationToken)
        {
            var remoteClient = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
            if (remoteClient != null)
            {
                var result = await remoteClient.TryInvokeAsync<IRemoteInheritanceMarginService, ImmutableArray<SerializableInheritanceMarginItem>>(
                    solution,
                    (remoteInheritanceMarginService, solutionInfo, cancellationToken) =>
                        remoteInheritanceMarginService.GetInheritanceMarginItemsAsync(solutionInfo, projectId, symbolKeyAndLineNumbers, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                if (!result.HasValue)
                {
                    return ImmutableArray<SerializableInheritanceMarginItem>.Empty;
                }

                return result.Value;
            }
            else
            {
                return await GetInheritanceMemberItemInProcAsync(solution, projectId, symbolKeyAndLineNumbers, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async ValueTask<ImmutableArray<SerializableInheritanceMarginItem>> GetInheritanceMemberItemInProcAsync(
            Solution solution,
            ProjectId projectId,
            ImmutableArray<(SymbolKey symbolKey, int lineNumber)> symbolKeyAndLineNumbers,
            CancellationToken cancellationToken)
        {
            var project = solution.GetRequiredProject(projectId);
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<SerializableInheritanceMarginItem>.GetInstance(out var builder);
            foreach (var (symbolKey, lineNumber) in symbolKeyAndLineNumbers)
            {
                var symbol = symbolKey.Resolve(compilation, cancellationToken: cancellationToken).Symbol;
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    await AddInheritanceMemberItemsForNamedTypeAsync(solution, namedTypeSymbol, lineNumber, builder, cancellationToken).ConfigureAwait(false);
                }

                if (symbol is IEventSymbol or IPropertySymbol or IMethodSymbol)
                {
                    await AddInheritanceMemberItemsForTypeMembersAsync(solution, symbol, lineNumber, builder, cancellationToken).ConfigureAwait(false);
                }
            }

            return builder.ToImmutable();
        }

        private static async ValueTask AddInheritanceMemberItemsForNamedTypeAsync(
            Solution solution,
            INamedTypeSymbol memberSymbol,
            int lineNumber,
            ArrayBuilder<SerializableInheritanceMarginItem> builder,
            CancellationToken cancellationToken)
        {
            // Get all base types.
            var allBaseSymbols = BaseTypeFinder.FindBaseTypesAndInterfaces(memberSymbol);

            // Filter out
            // 1. System.Object. (otherwise margin would be shown for all classes)
            // 2. System.ValueType. (otherwise margin would be shown for all structs)
            // 3. System.Enum. (otherwise margin would be shown for all enum)
            // 4. Error type.
            // For example, if user has code like this,
            // class Bar : ISomethingIsNotDone { }
            // The interface has not been declared yet, so don't show this error type to user.
            var baseSymbols = allBaseSymbols
                .WhereAsArray(symbol => !symbol.IsErrorType() && symbol.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum));

            // Get all derived types
            var allDerivedSymbols = await GetDerivedTypesAndImplementationsAsync(
                solution,
                memberSymbol,
                cancellationToken).ConfigureAwait(false);

            // Ensure the user won't be able to see symbol outside the solution for derived symbols.
            // For example, if user is viewing 'IEnumerable interface' from metadata, we don't want to tell
            // the user all the derived types under System.Collections
            var derivedSymbols = allDerivedSymbols.WhereAsArray(symbol => symbol.Locations.Any(l => l.IsInSource));

            if (baseSymbols.Any() || derivedSymbols.Any())
            {
                var item = await CreateInheritanceMemberItemAsync(
                    solution,
                    memberSymbol,
                    lineNumber,
                    baseSymbols: baseSymbols.CastArray<ISymbol>(),
                    derivedTypesSymbols: derivedSymbols.CastArray<ISymbol>(),
                    cancellationToken).ConfigureAwait(false);
                builder.AddIfNotNull(item);
            }
        }

        private static async ValueTask AddInheritanceMemberItemsForTypeMembersAsync(
            Solution solution,
            ISymbol memberSymbol,
            int lineNumber,
            ArrayBuilder<SerializableInheritanceMarginItem> builder,
            CancellationToken cancellationToken)
        {
            // For a given member symbol (method, property and event), its base and derived symbols are classified into 4 cases.
            // The mapping between images
            // Implemented : I↓
            // Implementing : I↑
            // Overridden: O↓
            // Overriding: O↑

            // Go down the inheritance chain to find all the overrides targets.
            var allOverriddenSymbols = await SymbolFinder.FindOverridesArrayAsync(memberSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Go up the inheritance chain to find all the implemented targets.
            var implementingSymbols = memberSymbol.ExplicitOrImplicitInterfaceImplementations();

            // Go up the inheritance chain to find all overriding targets
            var overridingSymbols = GetOverridingSymbols(memberSymbol);

            // Go down the inheritance chain to find all the implementing targets.
            var allImplementedSymbols = await GetImplementedSymbolsAsync(solution, memberSymbol, cancellationToken).ConfigureAwait(false);

            // For all overriden & implemented symbols, make sure it is in source.
            // For example, if the user is viewing System.Threading.SynchronizationContext from metadata,
            // then don't show the derived overriden & implemented method in the default implementation for System.Threading.SynchronizationContext in metadata
            var overriddenSymbols = allOverriddenSymbols.WhereAsArray(symbol => symbol.Locations.Any(l => l.IsInSource));
            var implementedSymbols = allImplementedSymbols.WhereAsArray(symbol => symbol.Locations.Any(l => l.IsInSource));

            if (overriddenSymbols.Any() || overridingSymbols.Any() || implementingSymbols.Any() || implementedSymbols.Any())
            {
                var item = await CreateInheritanceMemberInfoForMemberAsync(
                    solution,
                    memberSymbol,
                    lineNumber,
                    implementingMembers: implementingSymbols,
                    implementedMembers: implementedSymbols,
                    overridenMembers: overriddenSymbols,
                    overridingMembers: overridingSymbols,
                    cancellationToken).ConfigureAwait(false);

                builder.AddIfNotNull(item);
            }
        }

        private static async ValueTask<SerializableInheritanceMarginItem> CreateInheritanceMemberItemAsync(
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

            return new SerializableInheritanceMarginItem(
                lineNumber,
                FindUsagesHelpers.GetDisplayParts(memberSymbol),
                memberSymbol.GetGlyph(),
                baseSymbolItems.Concat(derivedTypeItems));
        }

        private static async ValueTask<SerializableInheritanceTargetItem> CreateInheritanceItemAsync(
            Solution solution,
            ISymbol targetSymbol,
            InheritanceRelationship inheritanceRelationship,
            CancellationToken cancellationToken)
        {
            var symbolInSource = await SymbolFinder.FindSourceDefinitionAsync(targetSymbol, solution, cancellationToken).ConfigureAwait(false);
            targetSymbol = symbolInSource ?? targetSymbol;

            // Right now the targets are not shown in a classified way.
            var definition = await targetSymbol.ToNonClassifiedDefinitionItemAsync(
                solution,
                includeHiddenLocations: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var containingSymbol = targetSymbol.ContainingSymbol;
            var containingSymbolName = containingSymbol is INamespaceSymbol { IsGlobalNamespace: true }
                ? FeaturesResources.Global_Namespace
                : containingSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            return new SerializableInheritanceTargetItem(
                inheritanceRelationship,
                // Id is used by FAR service for caching, it is not used in inheritance margin
                SerializableDefinitionItem.Dehydrate(id: 0, definition),
                targetSymbol.GetGlyph(),
                containingSymbolName);
        }

        private static async ValueTask<SerializableInheritanceMarginItem> CreateInheritanceMemberInfoForMemberAsync(
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

            return new SerializableInheritanceMarginItem(
                lineNumber,
                FindUsagesHelpers.GetDisplayParts(memberSymbol),
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
