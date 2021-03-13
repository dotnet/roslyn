// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal abstract class AbstractInheritanceMarginService : IInheritanceMarginService
    {
        public async Task<ImmutableArray<InheritanceMemberItem>> GetInheritanceInfoForLineAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var allDeclarationNodes = GetMembers(root);

            if (allDeclarationNodes.IsEmpty)
            {
                return ImmutableArray<InheritanceMemberItem>.Empty;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<InheritanceMemberItem>.GetInstance(out var builder);

            foreach (var memberDeclarationNode in allDeclarationNodes)
            {
                var member = semanticModel.GetDeclaredSymbol(memberDeclarationNode, cancellationToken);
                var lineNumber = sourceText.Lines.GetLineFromPosition(memberDeclarationNode.SpanStart).LineNumber;
                if (member is INamedTypeSymbol { TypeKind: not TypeKind.Error } namedTypeSymbol)
                {
                    var baseTypes = GetImplementingSymbols(namedTypeSymbol);
                    var derivedTypes = await GetImplementedSymbolsAsync(
                        document,
                        namedTypeSymbol,
                        cancellationToken).ConfigureAwait(false);
                    if (!baseTypes.IsEmpty && !derivedTypes.IsEmpty)
                    {
                        var item = CreateInheritanceMemberInfo(
                            document,
                            namedTypeSymbol,
                            lineNumber,
                            baseSymbols: baseTypes,
                            derivedTypesSymbols: derivedTypes,
                            cancellationToken);
                        builder.Add(item);
                    }
                }

                if (member is IMethodSymbol or IEventSymbol or IPropertySymbol)
                {
                    var overridenSymbols = await GetOverridenSymbolsAsync(document, member, cancellationToken).ConfigureAwait(false);
                    var overridingMembers = GetOverridingSymbols(member);
                    var implementedMembers = await GetImplementedSymbolsAsync(document, member, cancellationToken).ConfigureAwait(false);
                    var implementingMembers = GetImplementingSymbols(member);
                    if (!overridenSymbols.IsEmpty
                        && !overridingMembers.IsEmpty
                        && !implementingMembers.IsEmpty
                        && !implementedMembers.IsEmpty)
                    {
                        var item = CreateInheritanceMemberInfoForMember(
                            document,
                            member,
                            lineNumber,
                            implementingMembers: implementingMembers,
                            implementedMembers: implementedMembers,
                            overridenMembers: overridenSymbols,
                            overridingMembers: overridingMembers,
                            cancellationToken);
                        builder.Add(item);
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static InheritanceMemberItem CreateInheritanceMemberInfo(
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

            var baseSymbolItems = baseSymbols
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, InheritanceRelationship.Implementing, cancellationToken));

            var derivedTypeItems = derivedTypesSymbols
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, InheritanceRelationship.Implemented, cancellationToken));

            return new InheritanceMemberItem(
                lineNumber,
                memberDescription,
                memberSymbol.GetGlyph(),
                baseSymbolItems.Concat(derivedTypeItems).ToImmutableArray());
        }

        private static InheritanceTargetItem CreateInheritanceItem(
            Document document,
            ISymbol targetSymbol,
            InheritanceRelationship inheritanceRelationshipWithOriginalMember,
            CancellationToken cancellationToken)
        {
            var targetDescription = new TaggedText(
                tag: GetTextTag(targetSymbol),
                text: targetSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var definitions = GoToDefinitionHelpers.GetDefinitions(
                targetSymbol,
                document.Project.Solution,
                thirdPartyNavigationAllowed: true,
                cancellationToken);

            return new InheritanceTargetItem(
                targetDescription,
                targetSymbol.GetGlyph(),
                inheritanceRelationshipWithOriginalMember,
                definitions);
        }

        private static InheritanceMemberItem CreateInheritanceMemberInfoForMember(
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

            var implementingMemberItems = implementingMembers
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, InheritanceRelationship.Implementing, cancellationToken));
            var implementedMemberItems = implementedMembers
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, InheritanceRelationship.Implemented, cancellationToken));
            var overridenMemberItems = overridenMembers
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, InheritanceRelationship.Overriden, cancellationToken));
            var overridingMemberItems = overridingMembers
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, InheritanceRelationship.Overriding, cancellationToken));

            return new InheritanceMemberItem(
                lineNumber,
                memberDescription,
                memberSymbol.GetGlyph(),
                implementingMemberItems.Concat(implementedMemberItems)
                    .Concat(overridenMemberItems)
                    .Concat(overridingMemberItems));
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
                return namedTypeSymbol.AllInterfaces.Concat(namedTypeSymbol.GetBaseTypes()).OfType<ISymbol>().ToImmutableArray();
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
                    builder.Add(overridenMember);
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


        protected abstract ImmutableArray<SyntaxNode> GetMembers(SyntaxNode root);
    }
}
