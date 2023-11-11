// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// Computes a syntactic text change range that determines the range of a document that was changed by an edit. The
    /// portions outside this change range are guaranteed to be syntactically identical (see <see
    /// cref="SyntaxNode.IsIncrementallyIdenticalTo"/>).  This algorithm is intended to be <em>fast</em>.  It is
    /// technically linear in the number of nodes and tokens that may need to examined.  However, in practice, it should
    /// operate in sub-linear time as it will bail the moment tokens don't match, and it's able to skip over matching
    /// nodes fully without examining the contents of those nodes.  This is intended for consumers that want a
    /// reasonably accurate change range computer, but do not want to spend an inordinate amount of time getting the
    /// most accurate and minimal result possible.
    /// </summary>
    /// <remarks>
    /// This computation is not guaranteed to be minimal.  It may return a range that includes parts that are unchanged.
    /// This means it is also legal for the change range to just specify the entire file was changed. The quality of
    /// results will depend on how well the parsers did with incremental parsing, and how much time is given to do the
    /// comparison.  In practice, for large files (i.e. 15kloc) with standard types of edits, this generally returns
    /// results in around 50-100 usecs on a i7 3GHz desktop.
    /// <para>
    /// This algorithm will respect the timeout provided to the best of abilities.  If any information has been computed
    /// when the timeout elapses, it will be returned.
    /// </para>
    /// </remarks>
    internal static class SyntacticChangeRangeComputer
    {
        public static TextChangeRange ComputeSyntacticChangeRange(SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (oldRoot.IsIncrementallyIdenticalTo(newRoot))
                return default;

            var stopwatch = SharedStopwatch.StartNew();

            // We will be comparing the trees for two documents like so:
            //
            //       --------------------------------------------
            //  old: |                                          |
            //       --------------------------------------------
            //
            //       ---------------------------------------------------
            //  new: |                                                 |
            //       ---------------------------------------------------
            //
            // (Note that `new` could be smaller or the same length as `old`, it makes no difference).
            //
            // The algorithm will sweep in from both sides, as long as the nodes and tokens it's touching on each side
            // are 'identical' (i.e. are the exact same green node, and were thus reused over an incremental parse.).
            // This will leave us with:
            //
            //       --------------------------------------------
            //  old: |  CLW  |                      |    CRW    |
            //       --------------------------------------------
            //       |       |                          \           \
            //       ---------------------------------------------------
            //  new: |  CLW  |                             |    CRW    |
            //       ---------------------------------------------------
            //
            // Where CLW and CRW refer to the common-left-width and common-right-width respectively. The part in between
            // this s the change range:
            //
            //       --------------------------------------------
            //  old: |       |**********************|           |
            //       --------------------------------------------
            //               |**************************\
            //       ---------------------------------------------------
            //  new: |       |*****************************|           |
            //       ---------------------------------------------------
            //
            // The Span changed will go from `[CLW, Old_Width - CRW)`, and the NewLength will be `New_Width - CLW - CRW`

            var commonLeftWidth = ComputeCommonLeftWidth(oldRoot, newRoot, stopwatch, timeout, cancellationToken);
            var commonRightWidth = ComputeCommonRightWidth(oldRoot, newRoot, stopwatch, timeout, cancellationToken);

            var oldRootWidth = oldRoot.FullWidth();
            var newRootWidth = newRoot.FullWidth();

            Contract.ThrowIfTrue(commonLeftWidth > oldRootWidth);
            Contract.ThrowIfTrue(commonLeftWidth > newRootWidth);
            Contract.ThrowIfTrue(commonRightWidth > oldRootWidth);
            Contract.ThrowIfTrue(commonRightWidth > newRootWidth);

            // it's possible for the common left/right to overlap.  This can happen as some tokens
            // in the parser have a true shared underlying state, so they may get consumed both on 
            // a leftward and rightward walk.  Cap the right width so that it never overlaps the left
            // width in either the old or new tree.
            commonRightWidth = Math.Min(commonRightWidth, oldRootWidth - commonLeftWidth);
            commonRightWidth = Math.Min(commonRightWidth, newRootWidth - commonLeftWidth);

            return new TextChangeRange(
                TextSpan.FromBounds(start: commonLeftWidth, end: oldRootWidth - commonRightWidth),
                newRootWidth - commonLeftWidth - commonRightWidth);

            static int ComputeCommonLeftWidth(SyntaxNodeOrToken oldNode, SyntaxNodeOrToken newNode, SharedStopwatch stopwatch, TimeSpan timeout, CancellationToken cancellationToken)
            {
                var oldChildren = oldNode.ChildNodesAndTokens();
                var newChildren = newNode.ChildNodesAndTokens();
                var minChildCount = Math.Min(oldChildren.Count, newChildren.Count);

                // If we've run out of time, just return what we've computed so far.  It's not as accurate as
                // we could be.  But the caller wants the results asap.
                if (stopwatch.Elapsed > timeout)
                    return oldNode.FullSpan.Start;

                if (minChildCount == 0)
                    return oldNode.FullSpan.End;

                cancellationToken.ThrowIfCancellationRequested();

                for (var i = 0; i < minChildCount; i++)
                {
                    var oldChild = oldChildren[i];
                    var newChild = newChildren[i];

                    // If the two nodes/tokens were the same just skip past them.  They're part of the common left width.
                    if (oldChild.IsIncrementallyIdenticalTo(newChild))
                        continue;

                    // if we reached a token for either of these, then we can't break things down any further, and we hit
                    // the furthest point they are common.
                    if (oldChild.IsToken || newChild.IsToken)
                        return oldChild.FullSpan.Start;

                    // These children appear different, call ComputeCommonSuffixStart to determine where they differ.
                    // If it doesn't find any difference, then keep iterating.
                    var childResult = ComputeCommonLeftWidth(oldChild, newChild, stopwatch, timeout, cancellationToken);
                    if (childResult != oldChild.FullSpan.End)
                        return childResult;
                }

                return oldChildren[minChildCount - 1].FullSpan.End;
            }

            static int ComputeCommonRightWidth(SyntaxNode oldRoot, SyntaxNode newRoot, SharedStopwatch stopwatch, TimeSpan timeout, CancellationToken cancellationToken)
            {
                return oldRoot.FullSpan.End - ComputeCommonSuffixStart(oldRoot, newRoot, stopwatch, timeout, cancellationToken);
            }

            static int ComputeCommonSuffixStart(SyntaxNodeOrToken oldNode, SyntaxNodeOrToken newNode, SharedStopwatch stopwatch, TimeSpan timeout, CancellationToken cancellationToken)
            {
                var oldChildren = oldNode.ChildNodesAndTokens();
                var newChildren = newNode.ChildNodesAndTokens();
                var minChildCount = Math.Min(oldChildren.Count, newChildren.Count);

                // If we've run out of time, just return what we've computed so far.  It's not as accurate as
                // we could be.  But the caller wants the results asap.
                if (stopwatch.Elapsed > timeout)
                    return oldNode.FullSpan.End;

                if (minChildCount == 0)
                    return oldNode.FullSpan.Start;

                cancellationToken.ThrowIfCancellationRequested();

                for (var i = 1; i <= minChildCount; i++)
                {
                    var oldChild = oldChildren[^i];
                    var newChild = newChildren[^i];

                    // If the two nodes/tokens were the same just skip past them.  They're part of the common left width.
                    if (oldChild.IsIncrementallyIdenticalTo(newChild))
                        continue;

                    // if we reached a token for either of these, then we can't break things down any further, and we hit
                    // the furthest point they are common.
                    if (oldChild.IsToken || newChild.IsToken)
                        return oldChild.FullSpan.End;

                    // These children appear different, call ComputeCommonSuffixStart to determine where they differ.
                    // If it doesn't find any difference, then keep iterating.
                    var childResult = ComputeCommonSuffixStart(oldChild, newChild, stopwatch, timeout, cancellationToken);
                    if (childResult != oldChild.FullSpan.Start)
                        return childResult;
                }

                return oldChildren[^minChildCount].FullSpan.Start;
            }
        }
    }
}
