// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class CodeRefactoringHelpers
    {
        /// <summary>
        /// <para>
        /// Returns an instance of <typeparamref name="TSyntaxNode"/> for refactoring given specified selection in document or null
        /// if no such instance exists.
        /// </para>
        /// <para>
        /// A <typeparamref name="TSyntaxNode"/> instance is returned if:
        /// - Selection is zero-width and inside/touching a Token with direct parent of type <typeparamref name="TSyntaxNode"/>.
        /// - Selection is zero-width and touching a Token whose ancestor ends/starts precisely on current selection .
        /// - Token whose direct parent of type <typeparamref name="TSyntaxNode"/> is selected.
        /// - Whole node of a type <typeparamref name="TSyntaxNode"/> is selected.
        /// </para>
        /// <para>
        /// Note: this function strips all whitespace from both the beginning and the end of given <paramref name="selection"/>.
        /// The stripped version is then used to determine relevant <see cref="SyntaxNode"/>. It also handles incomplete selections
        /// of tokens gracefully.
        /// </para>
        /// </summary>
        public static async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(
            Document document, TextSpan selection, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectionStripped = await GetStrippedTextSpan(document, selection, cancellationToken).ConfigureAwait(false);

            var node = root.FindNode(selectionStripped, getInnermostNodeForTie: true);
            SyntaxNode prevNode;
            do
            {
                if (node is TSyntaxNode)
                {
                    return (TSyntaxNode)node;
                }

                prevNode = node;
                node = node.Parent;

            } while (node != null && prevNode.FullWidth() == node.FullWidth());

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
                if (leftNode is TSyntaxNode)
                {
                    return (TSyntaxNode)leftNode;
                }

                leftNode = leftNode?.Parent;
            } while (leftNode != null && leftNode.Span.End == selection.Start);


            var tokenToRight = await root.SyntaxTree.GetTouchingTokenToRightOrInAsync(selectionStripped.Start, cancellationToken).ConfigureAwait(false);
            var rightNode = tokenToRight.Parent;
            do
            {
                // either touches a Token which parent is `TSyntaxNode` or is whose ancestor's span starts on selection
                if (rightNode is TSyntaxNode)
                {
                    return (TSyntaxNode)rightNode;
                }

                rightNode = rightNode?.Parent;
            } while (rightNode != null && rightNode.Span.Start == selection.Start);

            return default;

        }

        public static Task<bool> RefactoringSelectionIsValidAsync(
            Document document, TextSpan selection, SyntaxNode node, CancellationToken cancellation)
        {
            return RefactoringSelectionIsValidAsync(document, selection, node, ImmutableArray<SyntaxNode>.Empty, cancellation);
        }

        /// <summary>
        /// <para>
        /// Determines if a refactoring should be offered for a given node, given the specified selection in a document.
        /// The refactoring is offered either if the selection is zero-width and not inside one of the specified holes
        /// or if the selection contains the entire node and perhaps some whitespace.
        /// </para>
        /// <para>
        /// Note: this function considers the span containing the node to start at the earliest preceding whitespace
        /// (including up to one newline) before the node’s <see cref="SyntaxNode.SpanStart"/> and up through the last
        /// whitespace following the <see cref="SyntaxNode.Span"/>’s <see cref="TextSpan.End"/>.
        /// </para>
        /// <para>
        /// For the <paramref name="holes"/>, the position is considered invalid if it is *within* the hole, not if it
        /// is touching the hole edges.
        /// </para>
        /// </summary>
        public static async Task<bool> RefactoringSelectionIsValidAsync(
            Document document,
            TextSpan selection,
            SyntaxNode node,
            ImmutableArray<SyntaxNode> holes,
            CancellationToken cancellationToken)
        {
            if (selection.Length == 0)
            {
                return await RefactoringPositionIsValidAsync(
                    document, selection.Start, node, holes, cancellationToken).ConfigureAwait(false);
            }

            // If we have a selection, it needs to be selecting at least the full node. We allow the selection to start
            // in whitespace (including up to one newline) before the start of the node, and we allow it to end after
            // the end of the node and the end of the line the node ends on.
            var expandedSpan = await GetExpandedNodeSpan(document, node, cancellationToken).ConfigureAwait(false);
            if ((selection.Start >= expandedSpan.Start && selection.Start <= node.SpanStart) &&
                (selection.End >= node.Span.End && selection.End <= expandedSpan.End))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// <para>
        /// Determines if a refactoring should be offered for a given node, given the specified position in a document.
        /// The refactoring is offered if the position is somewhere on the span containing the node and not in any of
        /// the specified <paramref name="holes"/> to avoid.
        /// </para>
        /// <para>
        /// Note: this function considers the span containing the node to start at the earliest preceding whitespace
        /// (including up to one newline) before the node’s <see cref="SyntaxNode.SpanStart"/> and up through the last
        /// whitespace following the <see cref="SyntaxNode.Span"/>’s <see cref="TextSpan.End"/>.
        /// </para>
        /// <para>
        /// For the <paramref name="holes"/>, the position is considered invalid if it is *within* the hole, not if it
        /// is touching the hole edges.
        /// </para>
        /// </summary>
        public static async Task<bool> RefactoringPositionIsValidAsync(
            Document document,
            int position,
            SyntaxNode node,
            ImmutableArray<SyntaxNode> holes,
            CancellationToken cancellationToken)
        {
            var expandedSpan = await GetExpandedNodeSpan(document, node, cancellationToken).ConfigureAwait(false);
            if (!expandedSpan.IntersectsWith(position))
            {
                // Position isn’t within the node’s widened span. Definitely not valid here.
                return false;
            }

            foreach (var hole in holes)
            {
                if (position > hole.Span.Start && position < hole.Span.End)
                {
                    // Position was in one of the holes. Not valid here.
                    return false;
                }
            }

            return true;
        }

        private static Task<TextSpan> GetExpandedNodeSpan(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            return GetExpandedTextSpan(document, node.Span, cancellationToken);
        }

        private static async Task<TextSpan> GetExpandedTextSpan(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var nodeStartLine = sourceText.Lines.GetLineFromPosition(span.Start);

            // Enable vertical selections that catch the previous line break and perhaps some whitespace.
            if (nodeStartLine.LineNumber != 0)
            {
                nodeStartLine = sourceText.Lines[nodeStartLine.LineNumber - 1];
            }

            var nodeEndLine = sourceText.Lines.GetLineFromPosition(span.End);

            var start = span.Start;
            var end = span.End;

            while (start > nodeStartLine.Start && char.IsWhiteSpace(sourceText[start - 1]))
            {
                start--;
            }

            while (end < nodeEndLine.End && char.IsWhiteSpace(sourceText[end]))
            {
                end++;
            }

            return TextSpan.FromBounds(start, end);
        }

        /// <summary>
        /// Strips leading and trailing whitespace from <paramref name="span"/>.
        /// </summary>
        /// <remarks>
        /// Returns unchanged <paramref name="span"/> in case <see cref="TextSpan.IsEmpty"/>.
        /// Returns empty Span with original <see cref="TextSpan.Start"/> in case it contains only whitespace.
        /// </remarks>
        private static async Task<TextSpan> GetStrippedTextSpan(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            if (span.IsEmpty)
            {
                return span;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var start = span.Start;
            var end = span.End;

            while (start < end && char.IsWhiteSpace(sourceText[end - 1]))
            {
                end--;
            }

            while (start < end && char.IsWhiteSpace(sourceText[start]))
            {
                start++;
            }

            return start == end
                ? new TextSpan(start, 0)
                : TextSpan.FromBounds(start, end);
        }
    }
}
