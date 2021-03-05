// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.GoToBase;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceChainMargin
{
    internal abstract partial class AbstractInheritanceChainService<TTypeDeclarationNode> : IInheritanceChainService
        where TTypeDeclarationNode : SyntaxNode
    {
        public async Task<ImmutableArray<DefinitionItem>> GetInheritanceInfoForLineAsync(
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
                return ImmutableArray<DefinitionItem>.Empty;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var goToBaseService = document.GetRequiredLanguageService<IGoToBaseService>();
            var findUsageService = document.GetRequiredLanguageService<IFindUsagesService>();

            using var _ = ArrayBuilder<DefinitionItem>.GetInstance(out var builder);
            foreach (var declarationNode in allDeclarationNodes)
            {
                var memberSymbol = semanticModel.GetDeclaredSymbol(declarationNode);
                if (memberSymbol != null)
                {
                    var position = declarationNode.SpanStart;
                    var goToBaseSearchContext = new SimpleFindUsagesContext(cancellationToken);
                    var goToImplementationContext = new SimpleFindUsagesContext(cancellationToken);
                    await goToBaseService.FindBasesAsync(document, position, goToBaseSearchContext).ConfigureAwait(false);
                    await findUsageService.FindImplementationsAsync(document, position, goToImplementationContext).ConfigureAwait(false);
                    var lineNumber = sourceText.Lines.GetLineFromPosition(position).LineNumber;
                    builder.AddRange(goToImplementationContext.GetDefinitions());
                    builder.AddRange(goToImplementationContext.GetDefinitions());
                }
            }

            return builder.ToImmutableAndClear();
        }

        private static void Add(
            ISymbol memberSymbol,
            int lineNumber,
            ArrayBuilder<InheritanceInfo> builder,
            SimpleFindUsagesContext usagesContext)
        {
            var displayName = memberSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var memberTaggedText = new TaggedText(GetTextTag(memberSymbol), displayName);

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
    }
}
