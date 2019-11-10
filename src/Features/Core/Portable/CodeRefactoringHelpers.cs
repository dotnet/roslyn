// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        /// Underselection is defined as omitting whole nodes from either the beginning or the end. It can be used e.g. to detect that
        /// following selection `1 + [|2 + 3|]` is underselecting the whole expression node tree.
        /// </para>
        /// <para>
        /// Returns false if only and precisely one <see cref="SyntaxToken"/> is selected. In that case the <paramref name="selection"/> 
        /// is treated more as a caret location.
        /// </para>
        /// <para>
        /// It's intended to be used in conjunction with <see cref="IRefactoringHelpersService.GetRelevantNodesAsync{TSyntaxNode}(Document, TextSpan, CancellationToken)"/>
        /// that, for non-empty selections, returns the smallest encompassing node. A node that can, for certain refactorings, be too large given user-selection even though
        /// it is the smallest that can be retrieved.
        /// </para>
        /// <para>
        /// When <paramref name="selection"/> doesn't intersect the node in any way it's not considered to be underselected.
        /// </para>
        /// <para>
        /// Null node is always considered underselected.
        /// </para>
        /// </summary>
        public static bool IsNodeUnderselected(SyntaxNode? node, TextSpan selection)
        {
            // Selection is null -> it's always underselected
            // REASON: Easier API use -> underselected node, don't work on it further
            if (node == null)
            {
                return true;
            }

            // Selection or node is empty -> can't be underselected
            if (selection.IsEmpty || node.Span.IsEmpty)
            {
                return false;
            }

            // Selection is larger than node.Span -> can't be underselecting
            if (selection.Contains(node.Span))
            {
                return false;
            }

            // Selection doesn't intersect node -> can't be underselecting.
            // RATIONALE: If there's no intersection then we got the node in some other way, e.g. 
            // extracting it after user selected `;` at the end of an expression statement 
            // `foo()[|;|]` for `foo()` node.
            if (!node.FullSpan.OverlapsWith(selection))
            {
                return false;
            }

            // Only precisely one token of the node is selected -> treat is as empty selection -> not 
            // underselected. The rationale is that if only one Token is selected then the selection 
            // wasn't about precisely getting the one node and nothing else & therefore we should treat 
            // it as empty selection.
            if (node.FullSpan.Contains(selection.Start))
            {
                var selectionStartToken = node.FindToken(selection.Start);
                if (selection.IsAround(selectionStartToken))
                {
                    return false;
                }
            }

            var beginningNode = node.FindToken(node.Span.Start).Parent;
            var endNode = node.FindToken(node.Span.End - 1).Parent;

            // Node is underselected if either the first (lowest) child doesn't contain start of selection
            // of the last child doesn't intersect with the end.

            // Node is underselected if either the first (lowest) child ends before the selection has started
            // or the last child starts after the selection ends (i.e. one of them is completely on the outside of selection).
            // It's a crude heuristic but it allows omitting parts of nodes or trivial tokens from the beginning/end 
            // but fires up e.g.: `1 + [|2 + 3|]`.
            return beginningNode.Span.End <= selection.Start || endNode.Span.Start >= selection.End;
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
