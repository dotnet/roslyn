// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class CodeRefactoringHelpers
    {
        /// <summary>
        /// <para>
        /// Determines if a <paramref name="node"/> is underselected given <paramref name="selection"/>.
        /// </para>
        /// <para>
        /// Underselection is defined as ommiting whole nodes from either the beginning or the end. It can be used e.g. to detect that
        /// following selection `1 + [|2 + 3|]` is underselecting the whole expression node tree.
        /// </para>
        /// <para>
        /// It's intended to be used in conjunction with <see cref="IRefactoringHelpersService.GetRelevantNodesAsync{TSyntaxNode}(Document, TextSpan, CancellationToken)"/>
        /// that, for non-empty selections, returns the smallest encompassing node. A node that can, for certain refactorings, be too large given user-selection even though
        /// it is the smallest that can be retrieved.
        /// </para>
        /// </summary>
        public static bool IsNodeUnderselected(SyntaxNode node, TextSpan selection)
        {
            if (selection.IsEmpty || node.Span.IsEmpty)
            {
                return false;
            }

            // If selection is larger than node.Span -> can't be underselecting
            if (selection.Contains(node.Span))
            {
                return false;
            }

            var beginningNode = node.FindToken(node.Span.Start).Parent;
            var endNode = node.FindToken(node.Span.End - 1).Parent;

            // Node is underselected if either the first (lowest) child doesn't contain start of selection
            // of the last child doesn't intersect with the end.
            // It's a crude heuristic but it allows omiting parts of nodes or trivial tokens from the beginning/end 
            // but fires up e.g.: `1 + [|2 + 3|]`.
            return !beginningNode.Span.IntersectsWith(selection.Start) || !endNode.Span.IntersectsWith(selection.End - 1);
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
        /// Trims leading and trailing whitespace from <paramref name="span"/>.
        /// </summary>
        /// <remarks>
        /// Returns unchanged <paramref name="span"/> in case <see cref="TextSpan.IsEmpty"/>.
        /// Returns empty Span with original <see cref="TextSpan.Start"/> in case it contains only whitespace.
        /// </remarks>
        public static async Task<TextSpan> GetTrimmedTextSpan(Document document, TextSpan span, CancellationToken cancellationToken)
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

            return TextSpan.FromBounds(start, end);
        }
    }
}
