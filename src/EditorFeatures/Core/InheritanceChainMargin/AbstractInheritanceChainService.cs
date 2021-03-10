// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceChainMargin
{
    internal abstract class AbstractInheritanceChainService<TTypeDeclarationNode> : IInheritanceChainService
        where TTypeDeclarationNode : SyntaxNode
    {
        public async Task<ImmutableDictionary<int, ImmutableArray<InheritanceMemberItem>>> GetInheritanceInfoForLineAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var allDeclarationNodes = GetDeclarationNodes(root);
            var allIdentifierPositions = allDeclarationNodes.SelectMany(node => GetMemberIdentifiersPosition(node))
                .ToImmutableArray();

            if (allIdentifierPositions.IsEmpty)
            {
                return ImmutableDictionary<int, ImmutableArray<InheritanceMemberItem>>.Empty;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            using var builder = new LineInheritanceInfoBuilder();

            foreach (var identifierPosition in allIdentifierPositions)
            {
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
                    document,
                    identifierPosition,
                    cancellationToken).ConfigureAwait(false);
                var lineNumber = sourceText.Lines.GetLineFromPosition(identifierPosition).LineNumber;
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    var baseTypes = GetMembersImplementedBySymbol(namedTypeSymbol);
                    var derivedTypes = await GetAllDerivedTypesAndMembersAsync(
                        document,
                        namedTypeSymbol,
                        cancellationToken).ConfigureAwait(false);
                    var item = CreateInheritanceMemberInfo(
                        document,
                        namedTypeSymbol,
                        baseTypes,
                        derivedTypes,
                        cancellationToken);
                    builder.AddItem(lineNumber, item);
                }

                if (symbol is IMethodSymbol or IEventSymbol or IPropertySymbol)
                {
                    var overrideMembers = await GetMembersOverridesSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
                    var overridenMembers = GetMembersOverridenBySymbol(symbol);
                    var implementingMembers = await GetMembersImplementingSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
                    var implementedMembers = GetMembersImplementedBySymbol(symbol);

                    var item = CreateInheritanceMemberInfoForMember(
                        document,
                        symbol,
                        implementingMembers,
                        implementedMembers,
                        overrideMembers,
                        overridenMembers,
                        cancellationToken);
                    builder.AddItem(lineNumber, item);
                }
            }

            return builder.ToImmutable();
        }

        private static InheritanceMemberItem CreateInheritanceMemberInfo(
            Document document,
            INamedTypeSymbol memberSymbol,
            ImmutableArray<ISymbol> baseSymbols,
            ImmutableArray<ISymbol> derivedTypesSymbols,
            CancellationToken cancellationToken)
        {
            var memberDescription = new TaggedText(
                tag: GetTextTag(memberSymbol),
                text: memberSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var baseSymbolItems = baseSymbols
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, Relationship.Implementing, cancellationToken));

            var derivedTypeItems = derivedTypesSymbols
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, Relationship.Implemented, cancellationToken));

            return new InheritanceMemberItem(
                memberDescription,
                memberSymbol.GetGlyph(),
                baseSymbolItems.Concat(derivedTypeItems).ToImmutableArray());
        }

        private static InheritanceTargetItem CreateInheritanceItem(
            Document document,
            ISymbol targetSymbol,
            Relationship relationshipWithOriginalMember,
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
                relationshipWithOriginalMember,
                definitions);
        }

        private static InheritanceMemberItem CreateInheritanceMemberInfoForMember(
            Document document,
            ISymbol memberSymbol,
            ImmutableArray<ISymbol> implementingMembers,
            ImmutableArray<ISymbol> implementedMembers,
            ImmutableArray<ISymbol> overrideMembers,
            ImmutableArray<ISymbol> overridingMembers,
            CancellationToken cancellationToken)
        {
            var memberDescription = new TaggedText(
                tag: GetTextTag(memberSymbol),
                text: memberSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var implementingMemberItems = implementingMembers
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, Relationship.Implementing, cancellationToken));
            var implementedMemberItems = implementedMembers
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, Relationship.Implemented, cancellationToken));
            var overrideMembersItems = overrideMembers
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, Relationship.Overriding, cancellationToken));
            var overridenMembersItems = overridingMembers
                .SelectAsArray(symbol => CreateInheritanceItem(document, symbol, Relationship.Overriden, cancellationToken));

            return new InheritanceMemberItem(
                memberDescription,
                memberSymbol.GetGlyph(),
                implementingMemberItems.Concat(implementedMemberItems)
                    .Concat(overrideMembersItems)
                    .Concat(overridenMembersItems));
        }

        public static async Task<ImmutableArray<ISymbol>> GetMembersImplementingSymbolAsync(
            Document document,
            ISymbol memberSymbol,
            CancellationToken cancellationToken)
        {
            if (memberSymbol is INamedTypeSymbol { IsSealed: false } namedTypeSymbol)
            {
                var derivedTypes = await FindDerivedTypesAndImplementationsAsync(document, namedTypeSymbol, cancellationToken).ConfigureAwait(false);
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

        public static async Task<ImmutableArray<ISymbol>> GetMembersOverridesSymbolAsync(
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

        public static ImmutableArray<ISymbol> GetMembersImplementedBySymbol(ISymbol memberSymbol)
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

        public static ImmutableArray<ISymbol> GetMembersOverridenBySymbol(ISymbol memberSymbol)
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

        private static async Task<ImmutableArray<ISymbol>> GetAllDerivedTypesAndMembersAsync(
            Document document,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                var derivedTypes = await FindDerivedTypesAndImplementationsAsync(document, namedTypeSymbol, cancellationToken).ConfigureAwait(false);
                return derivedTypes.OfType<ISymbol>().ToImmutableArray();
            }

            if (symbol.ContainingSymbol.IsInterfaceType())
            {
                return await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(
                     symbol,
                     document.Project.Solution,
                     cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return ImmutableArray<ISymbol>.Empty;
        }

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


        protected abstract ImmutableArray<TTypeDeclarationNode> GetDeclarationNodes(SyntaxNode root);

        protected abstract ImmutableArray<SyntaxNode> GetMembers(TTypeDeclarationNode typeDeclarationNode);

        protected abstract ImmutableArray<int> GetMemberIdentifiersPosition(TTypeDeclarationNode typeDeclarationNode);
    }


    internal class LineInheritanceInfoBuilder : IDisposable
    {
        private readonly Dictionary<int, ArrayBuilder<InheritanceMemberItem>> _lineNumberToMemberItems;

        public LineInheritanceInfoBuilder()
        {
            _lineNumberToMemberItems = new Dictionary<int, ArrayBuilder<InheritanceMemberItem>>();
        }

        public void AddItem(int lineNumber, InheritanceMemberItem item)
        {
            if (_lineNumberToMemberItems.TryGetValue(lineNumber, out var builder))
            {
                builder.Add(item);
            }
            else
            {
                var newBuilder = ArrayBuilder<InheritanceMemberItem>.GetInstance();
                newBuilder.Add(item);
                _lineNumberToMemberItems[lineNumber] = newBuilder;
            }
        }

        public ImmutableDictionary<int, ImmutableArray<InheritanceMemberItem>> ToImmutable()
            => _lineNumberToMemberItems.ToImmutableDictionary(
                keySelector: kvp => kvp.Key,
                elementSelector: kvp => kvp.Value.ToImmutable());

        public void Dispose()
        {
            if (_lineNumberToMemberItems.Count > 0)
            {
                foreach (var (_, items) in _lineNumberToMemberItems)
                {
                    items.Free();
                }

                _lineNumberToMemberItems.Clear();
            }
        }
    }
}
