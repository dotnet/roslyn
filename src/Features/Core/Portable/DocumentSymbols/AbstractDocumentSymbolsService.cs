// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentSymbols
{
    internal abstract class AbstractDocumentSymbolsService : IDocumentSymbolsService
    {
        public Task<ImmutableArray<DocumentSymbolInfo>> GetSymbolsInDocumentAsync(Document document, DocumentSymbolsOptions options, CancellationToken cancellationToken)
        {
            if (options == DocumentSymbolsOptions.TypesAndMethodsOnly)
            {
                return GetMembersInDocumentTypesAndMethodsOnlyAsync(document, options, cancellationToken);
            }
            else
            {
                return GetMembersInDocumentFullHierarchyAsync(document, options, cancellationToken);
            }
        }

        private async Task<ImmutableArray<DocumentSymbolInfo>> GetMembersInDocumentTypesAndMethodsOnlyAsync(Document document, DocumentSymbolsOptions options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (semanticModel is null)
            {
                return ImmutableArray<DocumentSymbolInfo>.Empty;
            }

            using var _ = PooledHashSet<INamedTypeSymbol>.GetInstance(out var typesInFile);
            GetTypesInFile(semanticModel, typesInFile, cancellationToken);

            using var __ = ArrayBuilder<DocumentSymbolInfo>.GetInstance(out var typesBuilder);
            foreach (var type in typesInFile)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return ImmutableArray<DocumentSymbolInfo>.Empty;
                }

                typesBuilder.Add(GetInfoForType(type));
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
                        if (cancellationToken.IsCancellationRequested)
                        {
                            types.Clear();
                            return;
                        }

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

        private async Task<ImmutableArray<DocumentSymbolInfo>> GetMembersInDocumentFullHierarchyAsync(Document document, DocumentSymbolsOptions options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (semanticModel is null || root is null)
            {
                return ImmutableArray<DocumentSymbolInfo>.Empty;
            }

            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToVisit);
            nodesToVisit.Push(root);

            return GetMembersInNode(semanticModel, nodesToVisit, startingCount: 0, options, cancellationToken);

            ImmutableArray<DocumentSymbolInfo> GetMembersInNode(SemanticModel semanticModel, ArrayBuilder<SyntaxNode> nodesToVisit, int startingCount, DocumentSymbolsOptions options, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<DocumentSymbolInfo>.GetInstance(out var memberBuilder);
                while (nodesToVisit.Count > startingCount)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return ImmutableArray<DocumentSymbolInfo>.Empty;
                    }

                    var node = nodesToVisit.Pop();

                    var symbol = GetSymbol(semanticModel, node, cancellationToken);

                    if (symbol is not null and { IsImplicitlyDeclared: false })
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

                            childrenSymbols = GetMembersInNode(semanticModel, nodesToVisit, currentCount, options, cancellationToken);
                            Debug.Assert(nodesToVisit.Count == currentCount);
                        }

                        memberBuilder.Add(CreateInfo(symbol, childrenSymbols));
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

        protected abstract ISymbol? GetSymbol(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool ShouldSkipSyntaxChildren(SyntaxNode node, DocumentSymbolsOptions options);
        protected abstract bool ConsiderNestedNodesChildren(ISymbol node);
        protected abstract DocumentSymbolInfo GetInfoForType(INamedTypeSymbol type);
        protected abstract DocumentSymbolInfo CreateInfo(ISymbol symbol, ImmutableArray<DocumentSymbolInfo> childrenSymbols);
    }
}
