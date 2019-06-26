// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract class AbstractRefactoringHelpersService : IRefactoringHelpersService
    {
        public abstract SyntaxNode ExtractNodeFromDeclarationAndAssignment<TNode>(SyntaxNode node) where TNode : SyntaxNode;

        public Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(
            Document document, TextSpan selection, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            return TryGetSelectedNodeAsync<TSyntaxNode>(document, selection, Functions<SyntaxNode>.Identity, cancellationToken);
        }

        public async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(
            Document document, TextSpan selection, Func<SyntaxNode, SyntaxNode> extractNode, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            return await TryGetSelectedNodeAsync(document, selection, extractNode, n => n is TSyntaxNode, cancellationToken).ConfigureAwait(false) as TSyntaxNode;
        }

        public async Task<SyntaxNode> TryGetSelectedNodeAsync(Document document, TextSpan selection, Func<SyntaxNode, SyntaxNode> extractNode, Predicate<SyntaxNode> predicate, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectionStripped = await CodeRefactoringHelpers.GetStrippedTextSpan(document, selection, cancellationToken).ConfigureAwait(false);

            // Handle selections
            // - the smallest node whose span inlcudes whole selection
            // - extraction from such node is attempted, result tested via predicate
            // - travels upwards through same-sized nodes, extracting and testing predicate
            var node = root.FindNode(selectionStripped, getInnermostNodeForTie: true);
            SyntaxNode prevNode;
            do
            {
                var extrNode = extractNode(node);
                if (extrNode != default && predicate(extrNode))
                {
                    return extrNode;
                }

                prevNode = node;
                node = node.Parent;

            }
            while (node != null && prevNode.FullWidth() == node.FullWidth());

            // consider what selection is touching only when it's empty -> prevents
            // `[|C|] methodName(){}` from registering as relevant for method Node
            if (!selection.IsEmpty)
            {
                return default;
            }

            // Handle Tokens & Nodes that start/end to left/right of given (empty) selection

            var tokenToLeft = await root.SyntaxTree.GetTouchingTokenToLeftAsync(selectionStripped.Start, cancellationToken).ConfigureAwait(false);
            var leftNode = tokenToLeft.Parent;
            do
            {
                // consider either a Node that is a parent of touched Token (selection can be within) or ancestor Node of such Token whose span ends on selection
                var extractedNode = extractNode(leftNode);
                if (extractedNode != default && predicate(extractedNode))
                {
                    return extractedNode;
                }

                leftNode = leftNode?.Parent;
            }
            while (leftNode != null && leftNode.Span.End == selection.Start);

            var tokenToRight = await root.SyntaxTree.GetTouchingTokenToRightOrInAsync(selectionStripped.Start, cancellationToken).ConfigureAwait(false);
            var rightNode = tokenToRight.Parent;
            do
            {
                // consider either a Node that is a parent of touched Token (selection can be within) or ancestor Node of such Token whose span starts on selection
                var extractedNode = extractNode(rightNode);
                if (extractedNode != default && predicate(extractedNode))
                {
                    return extractedNode;
                }

                rightNode = rightNode?.Parent;
            }
            while (rightNode != null && rightNode.Span.Start == selection.Start);

            return default;
        }
    }
}
