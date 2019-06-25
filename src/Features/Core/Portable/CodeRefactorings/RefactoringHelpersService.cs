using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract class RefactoringHelpersService : IRefactoringHelpersService
    {
        public abstract SyntaxNode ExtractNodeFromDeclarationAndAssignment<TNode>(SyntaxNode current) where TNode : SyntaxNode;

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

            // only consider what is direct selection touching when selection is empty 
            // prevents `[|C|] methodName(){}` from registering as relevant for method Node
            if (!selection.IsEmpty)
            {
                return default;
            }

            var tokenToLeft = await root.SyntaxTree.GetTouchingTokenToLeftAsync(selectionStripped.Start, cancellationToken).ConfigureAwait(false);
            var leftNode = tokenToLeft.Parent;
            do
            {
                // either touches a Token which parent is `TSyntaxNode` or is whose ancestor's span ends on selection
                var extrNode = extractNode(leftNode);
                if (extrNode != default && predicate(extrNode))
                {
                    return extrNode;
                }

                leftNode = leftNode?.Parent;
            }
            while (leftNode != null && leftNode.Span.End == selection.Start);

            var tokenToRight = await root.SyntaxTree.GetTouchingTokenToRightOrInAsync(selectionStripped.Start, cancellationToken).ConfigureAwait(false);
            var rightNode = tokenToRight.Parent;
            do
            {
                // either touches a Token which parent is `TSyntaxNode` or is whose ancestor's span starts on selection
                var extrNode = extractNode(rightNode);
                if (extrNode != default && predicate(extrNode))
                {
                    return extrNode;
                }

                rightNode = rightNode?.Parent;
            }
            while (rightNode != null && rightNode.Span.Start == selection.Start);

            return default;
        }

    }
}
