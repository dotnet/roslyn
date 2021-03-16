// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMapping;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal partial class AbstractInheritanceMarginService
    {
        private static async Task<InheritanceMemberItem> CreateInheritanceMemberInfoAsync(
            Document document,
            INamedTypeSymbol memberSymbol,
            int lineNumber,
            ImmutableArray<ISymbol> baseSymbols,
            ImmutableArray<ISymbol> derivedTypesSymbols,
            CancellationToken cancellationToken)
        {
            var memberDescription = new TaggedText(
                tag: GetTextTag(memberSymbol),
                text: memberSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var baseSymbolItems = await baseSymbols
                .SelectAsArrayAsync(symbol => CreateInheritanceItemAsync(document, symbol, InheritanceRelationship.Implementing, cancellationToken))
                .ConfigureAwait(false);

            var derivedTypeItems = await derivedTypesSymbols
                .SelectAsArrayAsync(symbol => CreateInheritanceItemAsync(document, symbol, InheritanceRelationship.Implemented, cancellationToken))
                .ConfigureAwait(false);

            return new InheritanceMemberItem(
                lineNumber,
                memberDescription,
                memberSymbol.GetGlyph(),
                baseSymbolItems.Concat(derivedTypeItems));
        }

        private static async Task<InheritanceTargetItem> CreateInheritanceItemAsync(
            Document document,
            ISymbol targetSymbol,
            InheritanceRelationship inheritanceRelationshipWithOriginalMember,
            CancellationToken cancellationToken)
        {
            var targetDescription = new TaggedText(
                tag: GetTextTag(targetSymbol),
                text: targetSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var definition = await targetSymbol.ToClassifiedDefinitionItemAsync(
                document.Project.Solution,
                isPrimary: true,
                includeHiddenLocations: false,
                FindReferencesSearchOptions.Default,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new InheritanceTargetItem(
                targetDescription,
                targetSymbol.GetGlyph(),
                inheritanceRelationshipWithOriginalMember,
                definition);
        }

        private static async Task<InheritanceMemberItem> CreateInheritanceMemberInfoForMemberAsync(
            Document document,
            ISymbol memberSymbol,
            int lineNumber,
            ImmutableArray<ISymbol> implementingMembers,
            ImmutableArray<ISymbol> implementedMembers,
            ImmutableArray<ISymbol> overridenMembers,
            ImmutableArray<ISymbol> overridingMembers,
            CancellationToken cancellationToken)
        {
            var memberDescription = new TaggedText(
                tag: GetTextTag(memberSymbol),
                text: memberSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var implementingMemberItems = await implementingMembers
                .SelectAsArrayAsync(symbol => CreateInheritanceItemAsync(document, symbol, InheritanceRelationship.Implementing, cancellationToken)).ConfigureAwait(false);
            var implementedMemberItems = await implementedMembers
                .SelectAsArrayAsync(symbol => CreateInheritanceItemAsync(document, symbol, InheritanceRelationship.Implemented, cancellationToken)).ConfigureAwait(false);
            var overridenMemberItems = await overridenMembers
                .SelectAsArrayAsync(symbol => CreateInheritanceItemAsync(document, symbol, InheritanceRelationship.Overriden, cancellationToken)).ConfigureAwait(false);
            var overridingMemberItems = await overridingMembers
                .SelectAsArrayAsync(symbol => CreateInheritanceItemAsync(document, symbol, InheritanceRelationship.Overriding, cancellationToken)).ConfigureAwait(false);

            return new InheritanceMemberItem(
                lineNumber,
                memberDescription,
                memberSymbol.GetGlyph(),
                implementingMemberItems.Concat(implementedMemberItems)
                    .Concat(overridenMemberItems)
                    .Concat(overridingMemberItems));
        }

        private static async Task<(ISymbol, Project project)?> GetMappingSymbolAsync(
            Document document,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            var mappingService = document.Project.Solution.Workspace.Services.GetRequiredService<ISymbolMappingService>();
            var result = await mappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            return (result.Symbol, result.Project);
        }

        private static async Task<ImmutableArray<ISymbol>> GetImplementedSymbolsAsync(
            Document document,
            ISymbol memberSymbol,
            CancellationToken cancellationToken)
        {
            if (memberSymbol is INamedTypeSymbol { IsSealed: false } namedTypeSymbol)
            {
                var derivedTypes = await GetDerivedTypesAndImplementationsAsync(document, namedTypeSymbol, cancellationToken).ConfigureAwait(false);
                return derivedTypes.OfType<ISymbol>().ToImmutableArray();
            }

            if (memberSymbol is IMethodSymbol or IEventSymbol or IPropertySymbol)
            {
                if (memberSymbol.ContainingSymbol.IsInterfaceType())
                {
                    return await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(
                        memberSymbol,
                        document.Project.Solution,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        private static async Task<ImmutableArray<ISymbol>> GetOverridenSymbolsAsync(
            Document document,
            ISymbol memberSymbol,
            CancellationToken cancellationToken)
        {
            if (!memberSymbol.IsOverridable())
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            return await SymbolFinder.FindOverridesArrayAsync(
                memberSymbol,
                document.Project.Solution,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static ImmutableArray<ISymbol> GetImplementingSymbols(ISymbol memberSymbol)
        {
            if (memberSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol
                    .AllInterfaces
                    .Concat(namedTypeSymbol.GetBaseTypes().ToImmutableArray())
                    .OfType<ISymbol>()
                    .Where(symbol => !symbol.IsErrorType())
                    .ToImmutableArray();
            }
            else
            {
                return memberSymbol.ExplicitOrImplicitInterfaceImplementations();
            }
        }

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
                    if (!overridenMember.IsErrorType())
                    {
                        builder.Add(overridenMember);
                    }
                }

                return builder.ToImmutableArray();
            }
        }

        private static async Task<ImmutableArray<INamedTypeSymbol>> GetDerivedTypesAndImplementationsAsync(
            Document document,
            INamedTypeSymbol typeSymbol,
            CancellationToken cancellationToken)
        {
            if (typeSymbol.IsInterfaceType())
            {
                var allDerivedInterfaces = await SymbolFinder.FindDerivedInterfacesArrayAsync(
                    typeSymbol,
                    document.Project.Solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var allImplementations = await SymbolFinder.FindImplementationsArrayAsync(
                    typeSymbol,
                    document.Project.Solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return allDerivedInterfaces.Concat(allImplementations).WhereAsArray(symbol => !symbol.IsErrorType());
            }
            else
            {
                return (await SymbolFinder.FindDerivedClassesArrayAsync(
                    typeSymbol,
                    document.Project.Solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false)).WhereAsArray(symbol => !symbol.IsErrorType());
            }
        }

        private static string GetTextTag(ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol.TypeKind switch
                {
                    TypeKind.Class => TextTags.Class,
                    TypeKind.Struct => TextTags.Struct,
                    TypeKind.Interface => TextTags.Interface,
                    _ => throw ExceptionUtilities.UnexpectedValue(namedTypeSymbol.TypeKind),
                };
            }
            else
            {
                return symbol.Kind switch
                {
                    SymbolKind.Method => TextTags.Method,
                    SymbolKind.Property => TextTags.Property,
                    SymbolKind.Event => TextTags.Event,
                    _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind),
                };
            }
        }
    }
}
