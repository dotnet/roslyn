// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        /// Given the syntax nodes to search,
        /// get all the method, event, property and type declaration syntax nodes.
        /// </summary>
        protected abstract ImmutableArray<SyntaxNode> GetMembers(IEnumerable<SyntaxNode> nodesToSearch);

        /// <summary>
        /// Get the token that represents declaration node.
        /// e.g. Identifier for method/property/event and this keyword for indexer.
        /// </summary>
        protected abstract SyntaxToken GetDeclarationToken(SyntaxNode declarationNode);

        public async ValueTask<ImmutableArray<InheritanceMarginItem>> GetInheritanceMemberItemsAsync(
            Document document,
            TextSpan spanToSearch,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var allDeclarationNodes = GetMembers(root.DescendantNodes(spanToSearch));
            if (allDeclarationNodes.IsEmpty)
            {
                return ImmutableArray<InheritanceMarginItem>.Empty;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            // Use mapping service to find correct solution & symbol.
            var mappingService = document.Project.Solution.Workspace.Services.GetRequiredService<ISymbolMappingService>();
            using var _ = ArrayBuilder<InheritanceMarginItem>.GetInstance(out var builder);

            // Iterate all the members symbol to find each their inheritance chain information
            foreach (var memberDeclarationNode in allDeclarationNodes)
            {
                var member = semanticModel.GetDeclaredSymbol(memberDeclarationNode, cancellationToken);
                if (member == null || member.IsStatic)
                {
                    continue;
                }

                var mappingResult = await mappingService.MapSymbolAsync(document, member, cancellationToken).ConfigureAwait(false);
                if (mappingResult == null)
                {
                    continue;
                }

                if (mappingResult.Symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    // Find its baseTypes and subTypes.
                    await AddInheritanceMemberItemsForNamedTypeAsync(
                        mappingResult.Project.Solution,
                        sourceText.Lines,
                        memberDeclarationNode,
                        namedTypeSymbol, builder, cancellationToken).ConfigureAwait(false);
                }
                else if (mappingResult.Symbol.IsOrdinaryMethod() || mappingResult.Symbol is IEventSymbol or IPropertySymbol)
                {
                    // Find the implementing/implemented/overridden/overriding members
                    await AddInheritanceMemberItemsForTypeMembersAsync(
                        mappingResult.Project.Solution,
                        sourceText.Lines,
                        memberDeclarationNode,
                        mappingResult.Symbol,
                        builder,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            return builder.ToImmutable();
        }

        private async Task AddInheritanceMemberItemsForNamedTypeAsync(
            Solution solution,
            TextLineCollection lines,
            SyntaxNode declarationNode,
            INamedTypeSymbol memberSymbol,
            ArrayBuilder<InheritanceMarginItem> builder,
            CancellationToken cancellationToken)
        {
            // Get all base types.
            var allBaseSymbols = BaseTypeFinder.FindBaseTypesAndInterfaces(memberSymbol);

            // Filter out
            // 1. System.Object. (otherwise margin would be shown for all classes)
            // 2. System.ValueType. (otherwise margin would be shown for all structs)
            // 3. System.Enum. (otherwise margin would be shown for all enum)
            var baseSymbols = allBaseSymbols
                .WhereAsArray(symbol => symbol.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum));

            // Get all derived types
            var derivedSymbols = await GetDerivedTypesAndImplementationsAsync(
                solution,
                memberSymbol,
                cancellationToken).ConfigureAwait(false);

            if (baseSymbols.Any() || derivedSymbols.Any())
            {
                var identifierToken = GetDeclarationToken(declarationNode);
                var lineNumber = lines.GetLineFromPosition(identifierToken.SpanStart).LineNumber;
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

        private async Task AddInheritanceMemberItemsForTypeMembersAsync(
            Solution solution,
            TextLineCollection lines,
            SyntaxNode declarationNode,
            ISymbol memberSymbol,
            ArrayBuilder<InheritanceMarginItem> builder,
            CancellationToken cancellationToken)
        {
            // For a given member symbol (method, property and event), its base and derived symbols are classified into 4 cases.
            // The mapping between images
            // Implemented : I↓
            // Implementing : I↑
            // Overridden: O↓
            // Overriding: O↑

            // Go down the inheritance chain to find all the overrides targets.
            var overriddenSymbols = await SymbolFinder.FindOverridesArrayAsync(memberSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Go up the inheritance chain to find all the implemented targets.
            var implementingSymbols = memberSymbol.ExplicitOrImplicitInterfaceImplementations();

            // Go up the inheritance chain to find all overriding targets
            var overridingSymbols = GetOverridingSymbols(memberSymbol);

            // Go down the inheritance chain to find all the implementing targets.
            var implementedSymbols = await GetImplementedSymbolsAsync(solution, memberSymbol, cancellationToken).ConfigureAwait(false);

            if (overriddenSymbols.Any() || overridingSymbols.Any() || implementingSymbols.Any() || implementedSymbols.Any())
            {
                var identifierToken = GetDeclarationToken(declarationNode);
                var lineNumber = lines.GetLineFromPosition(identifierToken.SpanStart).LineNumber;
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
    }
}
