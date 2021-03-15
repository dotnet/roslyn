// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal static class InheritanceMarginHelpers
    {
        public static async Task<ImmutableArray<ISymbol>> GetImplementedSymbolsAsync(
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

        public static async Task<ImmutableArray<ISymbol>> GetOverridenSymbolsAsync(
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

        public static ImmutableArray<ISymbol> GetImplementingSymbols(ISymbol memberSymbol)
        {
            if (memberSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol
                    .AllInterfaces
                    .Concat(namedTypeSymbol.GetBaseTypes().ToImmutableArray())
                    .OfType<ISymbol>()
                    .ToImmutableArray();
            }
            else
            {
                return memberSymbol.ExplicitOrImplicitInterfaceImplementations();
            }
        }

        public static ImmutableArray<ISymbol> GetOverridingSymbols(ISymbol memberSymbol)
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
                    builder.Add(overridenMember);
                }

                return builder.ToImmutableArray();
            }
        }

        public static async Task<ImmutableArray<INamedTypeSymbol>> GetDerivedTypesAndImplementationsAsync(
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

        public static string GetTextTag(ISymbol symbol)
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
