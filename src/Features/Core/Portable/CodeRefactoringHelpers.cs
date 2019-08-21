// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        /// Underselection is defined as omitting whole nodes from either the beginning or the end. It can be used e.g. to detect that
        /// following selection `1 + [|2 + 3|]` is underselecting the whole expression node tree.
        /// </para>
        /// <para>
        /// It's intended to be used in conjunction with <see cref="IRefactoringHelpersService.GetRelevantNodesAsync{TSyntaxNode}(Document, TextSpan, CancellationToken)"/>
        /// that, for non-empty selections, returns the smallest encompassing node. A node that can, for certain refactorings, be too large given user-selection even though
        /// it is the smallest that can be retrieved.
        /// </para>
        /// <para>
        /// Null node is always considered underselected.
        /// </para>
        /// </summary>
        public static bool IsNodeUnderselected(SyntaxNode? node, TextSpan selection)
        {
            if (node == null)
            {
                return true;
            }

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
            // It's a crude heuristic but it allows omitting parts of nodes or trivial tokens from the beginning/end 
            // but fires up e.g.: `1 + [|2 + 3|]`.
            return !beginningNode.Span.IntersectsWith(selection.Start) || !endNode.Span.IntersectsWith(selection.End - 1);
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
