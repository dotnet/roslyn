// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceChainMargin
{
    internal abstract partial class AbstractInheritanceChainService
    {
        private static async Task<ImmutableArray<INamedTypeSymbol>> FindDerivedTypesAndImplementationsAsync(
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
                return allDerivedInterfaces.Concat(allImplementations);
            }
            else
            {
                return await SymbolFinder.FindDerivedClassesArrayAsync(
                    typeSymbol,
                    document.Project.Solution,
                    transitive: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task<ImmutableArray<ISymbol>> FindOverrideMembersAsync(
            Document document,
            ISymbol memberSymbol,
            CancellationToken cancellationToken)
            => SymbolFinder.FindOverridesArrayAsync(
                memberSymbol,
                document.Project.Solution,
                cancellationToken: cancellationToken);

        private static Task<ImmutableArray<ISymbol>> FindImplementingMembersAsync(
            Document document,
            ISymbol memberSymbol,
            CancellationToken cancellationToken)
            => SymbolFinder.FindImplementedInterfaceMembersArrayAsync(
                memberSymbol,
                document.Project.Solution,
                cancellationToken: cancellationToken);

        private static ImmutableArray<ISymbol> FindOverridenMembers(
            ISymbol member,
            ImmutableArray<INamedTypeSymbol> allBaseTypeSymbols)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);

            if (member is IMethodSymbol methodSymbol)
            {
                for (var symbol = methodSymbol.OverriddenMethod; symbol != null; symbol = symbol.OverriddenMethod)
                {
                    builder.Add(symbol);
                }
            }

            if (member is IPropertySymbol propertySymbol)
            {
                for (var symbol = propertySymbol.OverriddenProperty; symbol != null; symbol = symbol.OverriddenProperty)
                {
                    builder.Add(symbol);
                }
            }

            if (member is IEventSymbol eventSymbol)
            {
                for (var symbol = eventSymbol.OverriddenEvent; symbol != null; symbol = symbol.OverriddenEvent)
                {
                    builder.Add(symbol);
                }
            }

            return builder.ToImmutableArray();
        }

        private static ImmutableArray<ISymbol> FindImplementingMembers(
            ISymbol memberSymbol,
            ImmutableArray<INamedTypeSymbol> allInterfaceSymbols)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
            foreach (var baseInterfaceSymbol in allInterfaceSymbols)
            {
                var baseTypeMembers = baseInterfaceSymbol.GetMembers();
                foreach (var baseTypeMember in baseTypeMembers)
                {
                    var implementation = baseInterfaceSymbol.FindImplementationForInterfaceMember(baseTypeMember);
                    if (implementation != null && implementation.Equals(memberSymbol))
                    {
                        builder.Add(baseTypeMember);
                    }
                }
            }

            return builder.ToImmutableArray();
        }

        // private static InheritanceInfo CreateMemberInheritanceInfo(
        //     INamedTypeSymbol memberSymbol,
        //     int memberDeclarationLine,
        //     ImmutableArray<INamedTypeSymbol> baseTypes,
        //     ImmutableArray<INamedTypeSymbol> subtypes)
        // {
        //     var memberDisplayName = memberSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        //     var taggedTextForMember = new TaggedText(GetTextTag(memberSymbol), memberDisplayName);
        //
        //     foreach (var baseType in baseTypes)
        //     {
        //
        //     }
        //
        //     foreach (var subtype in subtypes)
        //     {
        //
        //     }
        // }
        //
        // private static InheritanceItem CreateInheritanceItemForType(INamedTypeSymbol typeSymbol)
        // {
        //     var displayName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        // }
        //
        // private static InheritanceItem CreateInheritanceItemForMember(ISymbol memberSymbol)
        // {
        //     ISymbolNavigationService
        //     memberSymbol.Locations.SelectAsArray(l => l)
        //
        // }

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
