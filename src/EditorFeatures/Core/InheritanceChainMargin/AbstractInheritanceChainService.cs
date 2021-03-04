// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.GoToBase;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceChainMargin
{
    internal abstract class AbstractInheritanceChainService<TTypeDeclarationNode> : IInheritanceChainService
        where TTypeDeclarationNode : SyntaxNode
    {
        public async Task<ImmutableArray<MemberInheritanceInfo>> GetInheritanceInfoForLineAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var allDeclarationNodes = GetDeclarationNodes(root);
            var allMemberDeclarationNodes = allDeclarationNodes
                .SelectMany(node => GetMembers(node))
                .ToImmutableArray();

            if (allDeclarationNodes.IsEmpty && allMemberDeclarationNodes.IsEmpty)
            {
                return ImmutableArray<MemberInheritanceInfo>.Empty;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<MemberInheritanceInfo>.GetInstance(out var builder);
            foreach (var declarationNode in allDeclarationNodes)
            {
                var symbol = semanticModel.GetDeclaredSymbol(declarationNode);
                if (symbol != null)
                {
                }
            }

            return builder.ToImmutableAndClear();
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

        protected abstract ImmutableArray<TTypeDeclarationNode> GetDeclarationNodes(SyntaxNode root);

        protected abstract ImmutableArray<SyntaxNode> GetMembers(TTypeDeclarationNode typeDeclarationNode);
    }
}
