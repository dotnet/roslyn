// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.InheritanceMargin.InheritanceMarginServiceHelper;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal abstract class AbstractInheritanceMarginService : IInheritanceMarginService
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

            var mappingService = document.Project.Solution.Workspace.Services.GetRequiredService<ISymbolMappingService>();
            using var _ = ArrayBuilder<(SymbolKey symbolKey, int lineNumber)>.GetInstance(out var builder);

            Project? project = null;

            foreach (var memberDeclarationNode in allDeclarationNodes)
            {
                var member = semanticModel.GetDeclaredSymbol(memberDeclarationNode, cancellationToken);
                if (member == null || !CanHaveInheritanceTarget(member))
                {
                    continue;
                }

                // Use mapping service to find correct solution & symbol. (e.g. metadata symbol)
                var mappingResult = await mappingService.MapSymbolAsync(document, member, cancellationToken).ConfigureAwait(false);
                if (mappingResult == null)
                {
                    continue;
                }

                // All the symbols here are declared in the same document, they should belong to the same project.
                // So here it is enough to get the project once.
                project ??= mappingResult.Project;
                builder.Add((mappingResult.Symbol.GetSymbolKey(cancellationToken), sourceText.Lines.GetLineFromPosition(GetDeclarationToken(memberDeclarationNode).SpanStart).LineNumber));
            }

            var symbolKeyAndLineNumbers = builder.ToImmutable();
            if (symbolKeyAndLineNumbers.IsEmpty || project == null)
            {
                return ImmutableArray<InheritanceMarginItem>.Empty;
            }

            var solution = project.Solution;
            var serializedInheritanceMarginItems = await GetInheritanceMemberItemAsync(
                solution,
                project.Id,
                symbolKeyAndLineNumbers,
                cancellationToken).ConfigureAwait(false);
            return await serializedInheritanceMarginItems.SelectAsArrayAsync(
                (serializedItem, _) => InheritanceMarginItem.ConvertAsync(solution, serializedItem, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private static bool CanHaveInheritanceTarget(ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return !symbol.IsStatic && namedType.TypeKind is TypeKind.Interface or TypeKind.Class or TypeKind.Struct;
            }

            if (symbol is IEventSymbol or IPropertySymbol
                or IMethodSymbol
                {
                    MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.UserDefinedOperator or MethodKind.Conversion
                })
            {
                return true;
            }

            return false;
        }
    }
}
