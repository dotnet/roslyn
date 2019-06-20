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
        /// Returns the relevant <see cref="SyntaxNode"/> instance if refactoring for <typeparamref name="TSyntaxNode"/> type
        /// should be offered given the specified selection in document.
        /// </para>
        /// <para>
        /// A <typeparamref name="TSyntaxNode"/> instance is returned if selection is zero-width and inside/touching a Token whose
        /// direct parent is of type <typeparamref name="TSyntaxNode"/> or if a Node of said type is the smallest Node containing
        /// the whole <paramref name="selection"/>. Otherwise returns <code>null</code>.
        /// </para>
        /// <para>
        /// Note: this function strips all whitespace from both the beginning and end given <paramref name="selection"/> 
        /// and the stripped selection to determine the relevant Node. 
        /// </para>
        /// </summary>
        public static async Task<TSyntaxNode> TryGetSelectedNode<TSyntaxNode>(
            Document document, TextSpan selection, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectionStripped = await GetStrippedTextSpan(document, selection, cancellationToken).ConfigureAwait(false);

            var node = root.FindNode(selectionStripped) as TSyntaxNode;
            if (node == null)
            {
                // e.g. "C LocalFunction[||](C c)" -> root.FindNode return ParameterList but we still want to return LocalFunctionNode
                var identifier = await root.SyntaxTree.GetTouchingTokenAsync(selectionStripped.Start,
                    token => token.Parent is TSyntaxNode, cancellationToken).ConfigureAwait(false);
                node = identifier.Parent as TSyntaxNode;
            }

            return node;
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

        private static Task<TextSpan> GetExpandedNodeSpan(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
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

        private static async Task<TextSpan> GetStrippedTextSpan(
            Document document,
            TextSpan span,
            CancellationToken cancellationToken
            )
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var start = span.Start;
            var end = span.End;

            if (span.IsEmpty)
            {
                return span;
            }

            while (start < end && char.IsWhiteSpace(sourceText[start]))
            {
                start++;
            }

            while (start < end && char.IsWhiteSpace(sourceText[end - 1]) && char.IsWhiteSpace(sourceText[end]))
            {
                end--;
            }

            return TextSpan.FromBounds(start, end);
        }
    }
}
