// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
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

        public async ValueTask<ImmutableArray<InheritanceMemberItem>> GetInheritanceInfoAsync(
            Document document,
            TextSpan spanToSearch,
            CancellationToken cancellationToken)
        {
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

                        if (mappingResult.Symbol.IsOrdinaryMethod() || mappingResult.Symbol is IEventSymbol or IPropertySymbol)
                        {
                            // Find the implementing/implemented/overridden/overriding members
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
            // Get all base types.
            var allBaseTypes = BaseTypeFinder.FindBaseTypesAndInterfaces(memberSymbol);

            // Filter out
            // 1. System.Object. (otherwise margin would be shown for all classes)
            // 2. System.ValueType. (otherwise margin would be shown for all structs)
            // 3. System.Enum. (otherwise margin would be shown for all enum)
            var baseTypes = allBaseTypes
                .WhereAsArray(symbol => !IsUnwantedBaseType((ITypeSymbol)symbol));

            // Get all derived types
            var derivedTypes = await GetDerivedTypesAndImplementationsAsync(
                solution,
                memberSymbol,
                cancellationToken).ConfigureAwait(false);

            if (baseTypes.Any() || derivedTypes.Any())
            {
                var lineNumber = GetIdentifierLineNumber(sourceText, declarationNode);
                var item = await CreateInheritanceMemberInfoAsync(
                    solution,
                    memberSymbol,
                    lineNumber,
                    baseSymbols: baseTypes,
                    derivedTypesSymbols: derivedTypes.CastArray<ISymbol>(),
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
            // For a given member symbol (method, property and event), its base and derived symbols are classified into 4 cases.
            // The mapping between images
            // Implemented : I↓
            // Implementing : I↑
            // Overridden: O↓
            // Overriding: O↑

            // Go down the inheritance chain to find all the overrides targets.
            var overriddenMembers = await SymbolFinder.FindOverridesArrayAsync(memberSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Go up the inheritance chain to find all the implemented targets.
            var implementingMembers = memberSymbol.ExplicitOrImplicitInterfaceImplementations();

            // Go up the inheritance chain to find all overriding targets
            var overridingMembers = GetOverridingSymbols(memberSymbol);

            // Go down the inheritance chain to find all the implementing targets.
            var implementedMembers = await GetImplementedSymbolsAsync(solution, memberSymbol, cancellationToken).ConfigureAwait(false);

            if (overriddenMembers.Any() || overridingMembers.Any() || implementingMembers.Any() || implementedMembers.Any())
            {
                var lineNumber = GetIdentifierLineNumber(sourceText, declarationNode);
                var item = await CreateInheritanceMemberInfoForMemberAsync(
                    solution,
                    memberSymbol,
                    lineNumber,
                    implementingMembers: implementingMembers,
                    implementedMembers: implementedMembers,
                    overridenMembers: overriddenMembers,
                    overridingMembers: overridingMembers,
                    cancellationToken).ConfigureAwait(false);
                builder.Add(item);
            }
        }

        /// <summary>
        /// A set of widely used TypeSymbols that we don't want to show in margin
        /// </summary>
        private static bool IsUnwantedBaseType(ITypeSymbol symbol)
        {
            var specialType = symbol.SpecialType;
            return specialType == SpecialType.System_Object
                || specialType == SpecialType.System_ValueType
                || specialType == SpecialType.System_Enum;
        }
    }
}
