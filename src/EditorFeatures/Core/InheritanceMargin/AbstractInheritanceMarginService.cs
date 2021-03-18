// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal abstract partial class AbstractInheritanceMarginService : IInheritanceMarginService
    {
        /// <summary>
        /// Given the root and the searching span,
        /// get all the method, event, property and type declaration syntax nodes.
        /// </summary>
        protected abstract ImmutableArray<SyntaxNode> GetMembers(SyntaxNode root, TextSpan spanToSearch);

        /// <summary>
        /// Get the line number for the identifier of declaration node is declared.
        /// </summary>
        protected abstract int GetIdentifierLineNumber(SourceText sourceText, SyntaxNode declarationNode);

        public async Task<ImmutableArray<InheritanceMemberItem>> GetInheritanceInfoAsync(
            Document document,
            TextSpan spanToSearch,
            CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var featureEnabled = options.GetOption(InheritanceMarginOptions.ShowInheritanceMargin);
            if (!featureEnabled)
            {
                return ImmutableArray<InheritanceMemberItem>.Empty;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var allDeclarationNodes = GetMembers(root, spanToSearch);
            if (allDeclarationNodes.IsEmpty)
            {
                return ImmutableArray<InheritanceMemberItem>.Empty;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var mappingService = document.Project.Solution.Workspace.Services.GetRequiredService<ISymbolMappingService>();
            using var _ = ArrayBuilder<InheritanceMemberItem>.GetInstance(out var builder);

            // Iterate all the members symbol to find each their inheritance chain information
            foreach (var memberDeclarationNode in allDeclarationNodes)
            {
                var member = semanticModel.GetDeclaredSymbol(memberDeclarationNode, cancellationToken);
                if (member != null && !member.IsStatic)
                {
                    var mappingResult = await mappingService.MapSymbolAsync(document, member, cancellationToken).ConfigureAwait(false);
                    if (mappingResult != null)
                    {
                        if (mappingResult.Symbol is INamedTypeSymbol namedTypeSymbol)
                        {
                            // Find its baseTypes and subTypes.
                            await AddInheritanceMemberItemsForNamedTypeAsync(
                                mappingResult.Project.Solution,
                                sourceText,
                                memberDeclarationNode,
                                namedTypeSymbol, builder, cancellationToken).ConfigureAwait(false);
                        }

                        if (mappingResult.Symbol is IEventSymbol or IPropertySymbol || mappingResult.Symbol.IsOrdinaryMethod())
                        {
                            // Find the implementing/implemented/overriden/overriding members
                            await AddInheritanceMemberItemsForTypeMembersAsync(
                                mappingResult.Project.Solution,
                                sourceText,
                                memberDeclarationNode,
                                mappingResult.Symbol,
                                builder,
                                cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            return builder.ToImmutable();
        }

        private async Task AddInheritanceMemberItemsForNamedTypeAsync(
            Solution solution,
            SourceText sourceText,
            SyntaxNode declarationNode,
            INamedTypeSymbol memberSymbol,
            ArrayBuilder<InheritanceMemberItem> builder,
            CancellationToken cancellationToken)
        {
            // Get all base types
            var allBaseTypes = BaseTypeFinder.FindBaseTypesAndInterfaces(memberSymbol);

            // Filter out System.Object. (otherwise margin would be shown for all classes)
            var baseTypes = allBaseTypes
                .WhereAsArray(type =>
                    !(type is ITypeSymbol { SpecialType: SpecialType.System_Object }));

            // Get all derived types
            var derivedTypes = await GetDerivedTypesAndImplementationsAsync(
                solution,
                memberSymbol,
                cancellationToken).ConfigureAwait(false);

            if (!(baseTypes.IsEmpty && derivedTypes.IsEmpty))
            {
                var lineNumber = GetIdentifierLineNumber(sourceText, declarationNode);
                var item = await CreateInheritanceMemberInfoAsync(
                    solution,
                    memberSymbol,
                    lineNumber,
                    baseSymbols: baseTypes,
                    derivedTypesSymbols: derivedTypes.OfType<ISymbol>().ToImmutableArray(),
                    cancellationToken).ConfigureAwait(false);
                builder.Add(item);
            }
        }

        private async Task AddInheritanceMemberItemsForTypeMembersAsync(
            Solution solution,
            SourceText sourceText,
            SyntaxNode declarationNode,
            ISymbol memberSymbol,
            ArrayBuilder<InheritanceMemberItem> builder,
            CancellationToken cancellationToken)
        {
            // Go up the inheritance chain to find all the overrides targets.
            var overridenMembers = await SymbolFinder.FindOverridesArrayAsync(memberSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Go up the inheritance chain to find all the implementing targets.
            var implementingMembers = memberSymbol.ExplicitOrImplicitInterfaceImplementations();

            // Go down the inheritance chain to find all overrides targets
            var overridingMembers = GetOverridingSymbols(memberSymbol);

            // Go down the inheritance chain to find all the implmeneing targets.
            var implementedMembers = await GetImplementedSymbolsAsync(solution, memberSymbol, cancellationToken).ConfigureAwait(false);

            if (!(overridenMembers.IsEmpty
                && !overridingMembers.IsEmpty
                && !implementingMembers.IsEmpty
                && !implementedMembers.IsEmpty))
            {
                var lineNumber = GetIdentifierLineNumber(sourceText, declarationNode);
                var item = await CreateInheritanceMemberInfoForMemberAsync(
                    solution,
                    memberSymbol,
                    lineNumber,
                    implementingMembers: implementingMembers,
                    implementedMembers: implementedMembers,
                    overridenMembers: overridenMembers,
                    overridingMembers: overridingMembers,
                    cancellationToken).ConfigureAwait(false);
                builder.Add(item);
            }
        }
    }
}
