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
using static Microsoft.CodeAnalysis.InheritanceMargin.InheritanceMarginHelpers;

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
            var lines = sourceText.Lines;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<InheritanceMemberItem>.GetInstance(out var builder);

            foreach (var memberDeclarationNode in allDeclarationNodes)
            {
                var member = semanticModel.GetDeclaredSymbol(memberDeclarationNode, cancellationToken);
                if (member is INamedTypeSymbol { TypeKind: not TypeKind.Error } namedTypeSymbol)
                {
                    var baseTypes = GetImplementingSymbols(namedTypeSymbol);
                    var derivedTypes = await GetImplementedSymbolsAsync(
                        document,
                        namedTypeSymbol,
                        cancellationToken).ConfigureAwait(false);
                    if (!(baseTypes.IsEmpty && derivedTypes.IsEmpty))
                    {
                        var lineNumber = lines.GetLineFromPosition(memberDeclarationNode.SpanStart).LineNumber;
                        var item = await CreateInheritanceMemberInfoAsync(
                            document,
                            namedTypeSymbol,
                            lineNumber,
                            baseSymbols: baseTypes,
                            derivedTypesSymbols: derivedTypes,
                            cancellationToken).ConfigureAwait(false);
                        builder.Add(item);
                    }
                }

                if (member is IMethodSymbol or IEventSymbol or IPropertySymbol)
                {
                    var overridenSymbols = await GetOverridenSymbolsAsync(document, member, cancellationToken).ConfigureAwait(false);
                    var overridingMembers = GetOverridingSymbols(member);
                    var implementedMembers = await GetImplementedSymbolsAsync(document, member, cancellationToken).ConfigureAwait(false);
                    var implementingMembers = GetImplementingSymbols(member);
                    if (!(overridenSymbols.IsEmpty
                        && !overridingMembers.IsEmpty
                        && !implementingMembers.IsEmpty
                        && !implementedMembers.IsEmpty))
                    {
                        var lineNumber = lines.GetLineFromPosition(memberDeclarationNode.SpanStart).LineNumber;
                        var item = await CreateInheritanceMemberInfoForMemberAsync(
                            document,
                            member,
                            lineNumber,
                            implementingMembers: implementingMembers,
                            implementedMembers: implementedMembers,
                            overridenMembers: overridenSymbols,
                            overridingMembers: overridingMembers,
                            cancellationToken).ConfigureAwait(false);
                        builder.Add(item);
                    }
                }
            }

            return builder.ToImmutable();
        }

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

        protected abstract ImmutableArray<SyntaxNode> GetMembers(SyntaxNode root);
    }
}
