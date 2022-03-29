// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin.Finders;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal static class InheritanceMarginServiceHelper
    {
        private static readonly SymbolDisplayFormat s_displayFormat = new(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName |
                    SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        public static async ValueTask<ImmutableArray<SerializableInheritanceMarginItem>> GetInheritanceMemberItemAsync(
            Solution solution,
            ProjectId projectId,
            ImmutableArray<(SymbolKey symbolKey, int lineNumber)> symbolKeyAndLineNumbers,
            CancellationToken cancellationToken)
        {
            var remoteClient = await RemoteHostClient.TryGetClientAsync(solution.Workspace.Services, cancellationToken).ConfigureAwait(false);
            if (remoteClient != null)
            {
                // Here the line number is also passed to the remote process. It is done in this way because
                // when a set of symbols is passed to remote process, those without inheritance targets would not be returned.
                // To match the returned inheritance targets to the line number, we need set an 'Id' when calling the remote process,
                // however, given the line number is just an int, setting up an int 'Id' for an int is quite useless, so just passed it to the remote process.
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
                    await AddInheritanceMemberItemsForMembersAsync(solution, symbol, lineNumber, builder, cancellationToken).ConfigureAwait(false);
                }
            }

            return builder.ToImmutable();
        }

        private static async ValueTask AddInheritanceMemberItemsForNamedTypeAsync(
            Solution solution,
            INamedTypeSymbol namedTypeSymbol,
            int lineNumber,
            ArrayBuilder<SerializableInheritanceMarginItem> builder,
            CancellationToken cancellationToken)
        {
            var (baseTypeSymbolGroups, baseInterfaceSymbolGroups) = await BaseTypeSymbolsFinder.Instance
                .GetBaseTypeAndBaseInterfaceSymbolGroupsAsync(
                    namedTypeSymbol, solution, cancellationToken).ConfigureAwait(false);

            var derivedTypeSymbolGroups = await DerivedTypeSymbolsFinder.Instance.GetDerivedTypeSymbolGroupsAsync(
                namedTypeSymbol,
                solution,
                cancellationToken).ConfigureAwait(false);

            if (namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                if (baseInterfaceSymbolGroups.Any() || derivedTypeSymbolGroups.Any())
                {
                    var item = await CreateInheritanceMemberItemForInterfaceAsync(
                        solution,
                        namedTypeSymbol,
                        lineNumber,
                        baseSymbolGroups: baseInterfaceSymbolGroups,
                        derivedTypesSymbolGroups: derivedTypeSymbolGroups,
                        cancellationToken).ConfigureAwait(false);
                    builder.AddIfNotNull(item);
                }
            }
            else
            {
                Debug.Assert(namedTypeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct);
                if (baseTypeSymbolGroups.Any() || baseInterfaceSymbolGroups.Any() || derivedTypeSymbolGroups.Any())
                {
                    var item = await CreateInheritanceItemForClassAndStructureAsync(
                        solution,
                        namedTypeSymbol,
                        lineNumber,
                        baseTypeSymbolGroups: baseTypeSymbolGroups,
                        implementedInterfacesSymbolGroups: baseInterfaceSymbolGroups,
                        derivedTypesSymbolGroups: derivedTypeSymbolGroups,
                        cancellationToken).ConfigureAwait(false);
                    builder.AddIfNotNull(item);
                }
            }
        }

        private static async ValueTask AddInheritanceMemberItemsForMembersAsync(
            Solution solution,
            ISymbol memberSymbol,
            int lineNumber,
            ArrayBuilder<SerializableInheritanceMarginItem> builder,
            CancellationToken cancellationToken)
        {
            if (memberSymbol.ContainingSymbol.IsInterfaceType())
            {
                // Go down the inheritance chain to find all the implementing targets.
                var implementingSymbolGroups = await ImplementingSymbolsFinder.Instance.GetImplementingSymbolsGroupAsync(memberSymbol, solution, cancellationToken).ConfigureAwait(false);
                if (implementingSymbolGroups.Any())
                {
                    var item = await CreateInheritanceMemberItemForInterfaceMemberAsync(solution,
                        memberSymbol,
                        lineNumber,
                        implementingMembers: implementingSymbolGroups,
                        cancellationToken).ConfigureAwait(false);
                    builder.AddIfNotNull(item);
                }
            }
            else
            {
                // Go down the inheritance chain to find all the overriding targets.
                var overridingSymbolGroups = await OverridingSymbolsFinder.Instance.GetOverridingSymbolsGroupAsync(memberSymbol, solution, cancellationToken).ConfigureAwait(false);

                // Go up the inheritance chain to find all overridden targets & implemented targets.
                var (implementedSymbolGroups, overriddenSymbolGroups) = await ImplementedSymbolAndOverriddenSymbolsFinder.Instance
                    .GetImplementedSymbolAndOverrriddenSymbolGroupsAsync(memberSymbol, solution, cancellationToken).ConfigureAwait(false);
                if (overridingSymbolGroups.Any() || overriddenSymbolGroups.Any() || implementedSymbolGroups.Any())
                {
                    var item = await CreateInheritanceMemberItemForClassOrStructMemberAsync(solution,
                        memberSymbol,
                        lineNumber,
                        implementedMemberSymbolGroups: implementedSymbolGroups,
                        overridingMemberSymbolGroups: overridingSymbolGroups,
                        overriddenMemberSymbolGroups: overriddenSymbolGroups,
                        cancellationToken).ConfigureAwait(false);
                    builder.AddIfNotNull(item);
                }
            }
        }

        private static async ValueTask<SerializableInheritanceMarginItem> CreateInheritanceMemberItemForInterfaceAsync(
            Solution solution,
            INamedTypeSymbol interfaceSymbol,
            int lineNumber,
            ImmutableArray<SymbolGroup> baseSymbolGroups,
            ImmutableArray<SymbolGroup> derivedTypesSymbolGroups,
            CancellationToken cancellationToken)
        {
            var baseSymbolItems = await baseSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.InheritedInterface,
                    cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            var derivedTypeItems = await derivedTypesSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(solution,
                    symbolGroup,
                    InheritanceRelationship.ImplementingType,
                    cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            return new SerializableInheritanceMarginItem(
                lineNumber,
                FindUsagesHelpers.GetDisplayParts(interfaceSymbol),
                interfaceSymbol.GetGlyph(),
                baseSymbolItems.Concat(derivedTypeItems));
        }

        private static async ValueTask<SerializableInheritanceMarginItem> CreateInheritanceMemberItemForInterfaceMemberAsync(
            Solution solution,
            ISymbol memberSymbol,
            int lineNumber,
            ImmutableArray<SymbolGroup> implementingMembers,
            CancellationToken cancellationToken)
        {
            var implementedMemberItems = await implementingMembers
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.ImplementingMember,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            return new SerializableInheritanceMarginItem(
                lineNumber,
                FindUsagesHelpers.GetDisplayParts(memberSymbol),
                memberSymbol.GetGlyph(),
                implementedMemberItems);
        }

        private static async ValueTask<SerializableInheritanceMarginItem> CreateInheritanceItemForClassAndStructureAsync(
            Solution solution,
            INamedTypeSymbol memberSymbol,
            int lineNumber,
            ImmutableArray<SymbolGroup> baseTypeSymbolGroups,
            ImmutableArray<SymbolGroup> implementedInterfacesSymbolGroups,
            ImmutableArray<SymbolGroup> derivedTypesSymbolGroups,
            CancellationToken cancellationToken)
        {
            // If the target is an interface, it would be shown as 'Inherited interface',
            // and if it is an class/struct, it would be shown as 'Base Type'
            var baseTypeItems = await baseTypeSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.BaseType,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var implementedInterfaces = await implementedInterfacesSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.ImplementedInterface,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var derivedTypeItems = await derivedTypesSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(solution,
                    symbolGroup,
                    InheritanceRelationship.DerivedType,
                    cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            return new SerializableInheritanceMarginItem(
                lineNumber,
                FindUsagesHelpers.GetDisplayParts(memberSymbol),
                memberSymbol.GetGlyph(),
                baseTypeItems.Concat(implementedInterfaces).Concat(derivedTypeItems));
        }

        private static async ValueTask<SerializableInheritanceMarginItem> CreateInheritanceMemberItemForClassOrStructMemberAsync(
            Solution solution,
            ISymbol memberSymbol,
            int lineNumber,
            ImmutableArray<SymbolGroup> implementedMemberSymbolGroups,
            ImmutableArray<SymbolGroup> overridingMemberSymbolGroups,
            ImmutableArray<SymbolGroup> overriddenMemberSymbolGroups,
            CancellationToken cancellationToken)
        {
            var implementedMemberItems = await implementedMemberSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.ImplementedMember,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var overridenMemberItems = await overriddenMemberSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.OverriddenMember,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var overridingMemberItems = await overridingMemberSymbolGroups
                .Distinct()
                .SelectAsArrayAsync((symbolGroup, _) => CreateInheritanceItemAsync(
                    solution,
                    symbolGroup,
                    InheritanceRelationship.OverridingMember,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            return new SerializableInheritanceMarginItem(
                lineNumber,
                FindUsagesHelpers.GetDisplayParts(memberSymbol),
                memberSymbol.GetGlyph(),
                implementedMemberItems.Concat(overridenMemberItems).Concat(overridingMemberItems));
        }

        private static async ValueTask<SerializableInheritanceTargetItem> CreateInheritanceItemAsync(
            Solution solution,
            SymbolGroup symbolGroup,
            InheritanceRelationship inheritanceRelationship,
            CancellationToken cancellationToken)
        {
            var targetSymbol = symbolGroup.Symbols.First();
            var symbolInSource = await SymbolFinder.FindSourceDefinitionAsync(targetSymbol, solution, cancellationToken).ConfigureAwait(false);
            targetSymbol = symbolInSource ?? targetSymbol;

            // Right now the targets are not shown in a classified way.
            var definition = ToSlimDefinitionItem(targetSymbol, solution);

            var displayName = targetSymbol.ToDisplayString(s_displayFormat);

            return new SerializableInheritanceTargetItem(
                inheritanceRelationship,
                // Id is used by FAR service for caching, it is not used in inheritance margin
                SerializableDefinitionItem.Dehydrate(id: 0, definition),
                targetSymbol.GetGlyph(),
                displayName);
        }

        /// <summary>
        /// For the <param name="memberSymbol"/>, get all the implementing symbols.
        /// </summary>
        internal static async Task<ImmutableArray<ISymbol>> GetImplementingSymbolsForTypeMemberAsync(
            Solution solution,
            ISymbol memberSymbol,
            CancellationToken cancellationToken)
        {
            if (memberSymbol is IMethodSymbol or IEventSymbol or IPropertySymbol
                && memberSymbol.ContainingSymbol.IsInterfaceType())
            {
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
                // 1. Find all direct implementations for this member
                var implementationSymbols = await SymbolFinder.FindMemberImplementationsArrayAsync(
                    memberSymbol,
                    solution,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                builder.AddRange(implementationSymbols);

                // 2. Continue searching the overriden symbols. For example:
                // interface IBar { void Foo(); }
                // class Bar : IBar { public virtual void Foo() { } }
                // class Bar2 : IBar { public override void Foo() { } }
                // For 'IBar.Foo()', we need to find 'Bar2.Foo()'
                foreach (var implementationSymbol in implementationSymbols)
                {
                    builder.AddRange(await SymbolFinder.FindOverridesArrayAsync(implementationSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
                }

                return builder.ToImmutableArray();
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        /// <summary>
        /// Get overridden members the <param name="memberSymbol"/>.
        /// </summary>
        internal static ImmutableArray<ISymbol> GetOverriddenSymbols(ISymbol memberSymbol)
        {
            if (memberSymbol is INamedTypeSymbol)
            {
                return ImmutableArray<ISymbol>.Empty;
            }
            else
            {
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
                for (var overriddenMember = memberSymbol.GetOverriddenMember();
                    overriddenMember != null;
                    overriddenMember = overriddenMember.GetOverriddenMember())
                {
                    builder.Add(overriddenMember.OriginalDefinition);
                }

                return builder.ToImmutableArray();
            }
        }

        /// <summary>
        /// Get the derived interfaces and derived classes for <param name="typeSymbol"/>.
        /// </summary>
        internal static async Task<ImmutableArray<INamedTypeSymbol>> GetDerivedTypesAndImplementationsAsync(
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

        /// <summary>
        /// Create the DefinitionItem based on the numbers of locations for <paramref name="symbol"/>.
        /// If there is only one location, create the DefinitionItem contains only the documentSpan or symbolKey to save memory.
        /// Because in such case, when clicking InheritanceMarginGlpph, it will directly navigate to the symbol.
        /// Otherwise, create the full non-classified DefinitionItem. Because in such case we want to display all the locations to the user
        /// by reusing the FAR window.
        /// </summary>
        private static DefinitionItem ToSlimDefinitionItem(ISymbol symbol, Solution solution)
        {
            RoslynDebug.Assert(IsNavigableSymbol(symbol));
            var locations = symbol.Locations;
            if (locations.Length > 1)
            {
                return symbol.ToNonClassifiedDefinitionItem(
                    solution,
                    FindReferencesSearchOptions.Default with { UnidirectionalHierarchyCascade = true },
                    includeHiddenLocations: false);
            }

            if (locations.Length == 1)
            {
                var location = locations[0];
                if (location.IsInMetadata)
                {
                    return DefinitionItem.CreateMetadataDefinition(
                        tags: ImmutableArray<string>.Empty,
                        displayParts: ImmutableArray<TaggedText>.Empty,
                        nameDisplayParts: ImmutableArray<TaggedText>.Empty,
                        solution,
                        symbol);
                }
                else if (location.IsInSource && location.IsVisibleSourceLocation())
                {
                    var document = solution.GetDocument(location.SourceTree);
                    if (document != null)
                    {
                        var documentSpan = new DocumentSpan(document, location.SourceSpan);
                        return DefinitionItem.Create(
                            tags: ImmutableArray<string>.Empty,
                            displayParts: ImmutableArray<TaggedText>.Empty,
                            documentSpan,
                            nameDisplayParts: ImmutableArray<TaggedText>.Empty);
                    }
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal static bool IsNavigableSymbol(ISymbol symbol)
        {
            var locations = symbol.Locations;
            if (locations.Length == 1)
            {
                var location = locations[0];
                return location.IsInMetadata || (location.IsInSource && location.IsVisibleSourceLocation());
            }

            return !locations.IsEmpty;
        }
    }
}
