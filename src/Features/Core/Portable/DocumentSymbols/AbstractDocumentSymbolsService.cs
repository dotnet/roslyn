// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentSymbols
{
    internal abstract class AbstractDocumentSymbolsService : IDocumentSymbolsService
    {
        protected abstract ISymbol? GetSymbol(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool ShouldSkipSyntaxChildren(SyntaxNode node, DocumentSymbolsOptions options);
        protected abstract bool ConsiderNestedNodesChildren(ISymbol node);
        protected abstract DocumentSymbolInfo GetMemberInfoForType(INamedTypeSymbol type, SyntaxTree tree, ISymbolDeclarationService declarationService, CancellationToken cancellationToken);
        protected abstract DocumentSymbolInfo CreateInfo(ISymbol symbol, SyntaxTree tree, ISymbolDeclarationService declarationService, ImmutableArray<DocumentSymbolInfo> childrenSymbols, CancellationToken cancellationToken);

        public async Task<ImmutableArray<DocumentSymbolInfo>> GetSymbolsInDocumentAsync(Document document, DocumentSymbolsOptions options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var declarationService = document.GetRequiredLanguageService<ISymbolDeclarationService>();
            if (options == DocumentSymbolsOptions.TypesAndMembersOnly)
            {
                return GetMembersInDocumentTypesAndMethodsOnly(options, semanticModel, tree, declarationService, cancellationToken);
            }
            else
            {
                return await GetMembersInDocumentFullHierarchyAsync(options, semanticModel, tree, declarationService, cancellationToken).ConfigureAwait(false);
            }
        }

        private ImmutableArray<DocumentSymbolInfo> GetMembersInDocumentTypesAndMethodsOnly(
            DocumentSymbolsOptions options,
            SemanticModel semanticModel,
            SyntaxTree tree,
            ISymbolDeclarationService declarationService,
            CancellationToken cancellationToken)
        {
            using var _1 = PooledHashSet<INamedTypeSymbol>.GetInstance(out var typesInFile);
            GetTypesInFile(semanticModel, typesInFile, cancellationToken);

            using var _2 = ArrayBuilder<DocumentSymbolInfo>.GetInstance(out var typesBuilder);
            foreach (var type in typesInFile)
            {
                cancellationToken.ThrowIfCancellationRequested();
                typesBuilder.Add(GetMemberInfoForType(type, tree, declarationService, cancellationToken));
            }

            typesBuilder.Sort((d1, d2) => d1.Text.CompareTo(d2.Text));
            return typesBuilder.ToImmutable();

            void GetTypesInFile(SemanticModel semanticModel, HashSet<INamedTypeSymbol> types, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.DocumentSymbols_GetDocumentSymbols_GetTypesInFile, cancellationToken))
                {
                    using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToVisit);

                    nodesToVisit.Push(semanticModel.SyntaxTree.GetRoot(cancellationToken));

                    while (!nodesToVisit.IsEmpty())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var node = nodesToVisit.Pop();
                        var symbol = GetSymbol(semanticModel, node, cancellationToken);

                        if (symbol is INamedTypeSymbol type)
                        {
                            types.Add(type);
                        }

                        if (ShouldSkipSyntaxChildren(node, options))
                        {
                            // quick bail out to prevent us from creating every nodes exist in current file
                            continue;
                        }

                        foreach (var child in node.ChildNodes())
                        {
                            nodesToVisit.Push(child);
                        }
                    }
                }
            }
        }

        private async Task<ImmutableArray<DocumentSymbolInfo>> GetMembersInDocumentFullHierarchyAsync(
            DocumentSymbolsOptions options,
            SemanticModel semanticModel,
            SyntaxTree tree,
            ISymbolDeclarationService declarationService,
            CancellationToken cancellationToken)
        {
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToVisit);
            nodesToVisit.Push(root);

            return GetMembersInNode(semanticModel, tree, nodesToVisit, startingCount: 0, options, cancellationToken);

            ImmutableArray<DocumentSymbolInfo> GetMembersInNode(SemanticModel semanticModel, SyntaxTree tree, ArrayBuilder<SyntaxNode> nodesToVisit, int startingCount, DocumentSymbolsOptions options, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<DocumentSymbolInfo>.GetInstance(out var memberBuilder);
                while (nodesToVisit.Count > startingCount)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var node = nodesToVisit.Pop();

                    var symbol = GetSymbol(semanticModel, node, cancellationToken);

                    if (symbol is { IsImplicitlyDeclared: false })
                    {
                        ImmutableArray<DocumentSymbolInfo> childrenSymbols;
                        if (ShouldSkipSyntaxChildren(node, options))
                        {
                            childrenSymbols = ImmutableArray<DocumentSymbolInfo>.Empty;
                        }
                        else if (!ConsiderNestedNodesChildren(symbol))
                        {
                            childrenSymbols = ImmutableArray<DocumentSymbolInfo>.Empty;
                            AddChildrenToVisit(nodesToVisit, node);
                        }
                        else
                        {
                            var currentCount = nodesToVisit.Count;
                            AddChildrenToVisit(nodesToVisit, node);

                            childrenSymbols = GetMembersInNode(semanticModel, tree, nodesToVisit, currentCount, options, cancellationToken);
                            Debug.Assert(nodesToVisit.Count == currentCount);
                        }

                        memberBuilder.Add(CreateInfo(symbol, tree, declarationService, childrenSymbols, cancellationToken));
                    }
                    else
                    {
                        if (ShouldSkipSyntaxChildren(node, options))
                        {
                            continue;
                        }
                        AddChildrenToVisit(nodesToVisit, node);
                    }
                }

                return memberBuilder.ToImmutable();

                static void AddChildrenToVisit(ArrayBuilder<SyntaxNode> nodesToVisit, SyntaxNode node)
                {
                    var childSyntaxElements = node.ChildNodesAndTokens();
                    // Push onto the stack in reverse order to make sure we visit in file order
                    for (var i = childSyntaxElements.Count - 1; i >= 0; i--)
                    {
                        if (childSyntaxElements[i].AsNode() is { } child)
                        {
                            nodesToVisit.Push(child);
                        }
                    }
                }
            }
        }

        protected static ImmutableArray<TextSpan> GetDeclaringSpans(ISymbol symbol, SyntaxTree tree)
        {
            foreach (var location in symbol.Locations)
            {
                if (tree.Equals(location.SourceTree))
                {
                    return ImmutableArray.Create(location.SourceSpan);
                }
            }

            return ImmutableArray<TextSpan>.Empty;
        }
    }
}
