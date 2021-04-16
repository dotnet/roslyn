// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
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
        private static readonly ObjectPool<Stack<SyntaxNodeOrToken>> s_pool = new(() => new());

        public static async ValueTask<TextChangeRange?> ComputeSyntacticChangeRangeAsync(
            Document oldDocument, Document newDocument, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // If they're the same doc, there is no change.
            if (oldDocument == newDocument)
                return new TextChangeRange();

            var stopwatch = SharedStopwatch.StartNew();

            var oldRoot = await oldDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // If we ran out of time, we have to assume both are completely different.
            if (stopwatch.Elapsed > timeout)
                return null;

            var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (stopwatch.Elapsed > timeout)
                return null;

            return ComputeSyntacticChangeRange(oldRoot, newRoot, timeout, stopwatch, cancellationToken);
        }

        public static TextChangeRange ComputeSyntacticChangeRange(SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
            => ComputeSyntacticChangeRange(oldRoot, newRoot, timeout, SharedStopwatch.StartNew(), cancellationToken);

        private static TextChangeRange ComputeSyntacticChangeRange(
            SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, SharedStopwatch stopwatch, CancellationToken cancellationToken)
        {
            if (oldRoot == newRoot)
                return default;

            using var leftOldStack = s_pool.GetPooledObject();
            using var leftNewStack = s_pool.GetPooledObject();
            using var rightOldStack = s_pool.GetPooledObject();
            using var rightNewStack = s_pool.GetPooledObject();

            leftOldStack.Object.Push(oldRoot);
            leftNewStack.Object.Push(newRoot);
            rightOldStack.Object.Push(oldRoot);
            rightNewStack.Object.Push(newRoot);

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

            var commonLeftWidth = ComputeCommonLeftWidth(leftOldStack.Object, leftNewStack.Object);
            if (commonLeftWidth == null)
            {
                // The trees were effectively identical (even if the children were different).  Return that there was no
                // text change.
                return default;
            }

            // Only compute the right side if we have time for it.  Otherwise, assume there is nothing in common there.
            var commonRightWidth = 0;
            if (stopwatch.Elapsed < timeout)
                commonRightWidth = ComputeCommonRightWidth(rightOldStack.Object, rightNewStack.Object);

            var oldRootWidth = oldRoot.FullWidth();
            var newRootWidth = newRoot.FullWidth();

            Contract.ThrowIfTrue(commonLeftWidth > oldRootWidth);
            Contract.ThrowIfTrue(commonLeftWidth > newRootWidth);
            Contract.ThrowIfTrue(commonRightWidth > oldRootWidth);
            Contract.ThrowIfTrue(commonRightWidth > newRootWidth);

            return new TextChangeRange(
                TextSpan.FromBounds(start: commonLeftWidth.Value, end: oldRootWidth - commonRightWidth),
                newRootWidth - commonLeftWidth.Value - commonRightWidth);

            int? ComputeCommonLeftWidth(
                Stack<SyntaxNodeOrToken> oldStack,
                Stack<SyntaxNodeOrToken> newStack)
            {
                while (oldStack.Count > 0 && newStack.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var currentOld = oldStack.Pop();
                    var currentNew = newStack.Pop();
                    Contract.ThrowIfFalse(currentOld.FullSpan.Start == currentNew.FullSpan.Start);

                    // If the two nodes/tokens were the same just skip past them.  They're part of the common left width.
                    if (currentOld.IsIncrementallyIdenticalTo(currentNew))
                        continue;

                    // if we reached a token for either of these, then we can't break things down any further, and we hit
                    // the furthest point they are common.
                    if (currentOld.IsToken || currentNew.IsToken)
                        return currentOld.FullSpan.Start;

                    // Similarly, if we've run out of time, just return what we've computed so far.  It's not as accurate as
                    // we could be.  But the caller wants the results asap.
                    if (stopwatch.Elapsed > timeout)
                        return currentOld.FullSpan.Start;

                    // we've got two nodes, but they weren't the same.  For example, say we made an edit in a method in the
                    // class, the class node would be new, but there might be many member nodes that were the same that we'd
                    // want to see and skip.  Crumble the node and deal with its left side.
                    //
                    // Reverse so that we process the leftmost child first and walk left to right.
                    foreach (var nodeOrToken in currentOld.AsNode()!.ChildNodesAndTokens().Reverse())
                        oldStack.Push(nodeOrToken);

                    foreach (var nodeOrToken in currentNew.AsNode()!.ChildNodesAndTokens().Reverse())
                        newStack.Push(nodeOrToken);
                }

                // If we consumed all of 'new', then the length of the new doc is what we have in common.
                if (oldStack.Count > 0)
                    return newRoot.FullSpan.Length;

                // If we consumed all of 'old', then the length of the old doc is what we have in common.
                if (newStack.Count > 0)
                    return oldRoot.FullSpan.Length;

                // We consumed both stacks entirely.  That means the trees were identical (though the root was different). Return null to signify no change to the doc.
                return null;
            }

            int ComputeCommonRightWidth(
                Stack<SyntaxNodeOrToken> oldStack,
                Stack<SyntaxNodeOrToken> newStack)
            {
                while (oldStack.Count > 0 && newStack.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var currentOld = oldStack.Pop();
                    var currentNew = newStack.Pop();

                    // The width on the right we've moved past on both old/new should be the same.
                    Contract.ThrowIfFalse((oldRoot.FullSpan.End - currentOld.FullSpan.End) ==
                                          (newRoot.FullSpan.End - currentNew.FullSpan.End));

                    // If the two nodes/tokens were the same just skip past them.  They're part of the common right width.
                    // Critically though, we can only skip past if this wasn't already something we consumed when determining
                    // the common left width.  If this was common the left side, we can't consider it common to the right,
                    // otherwise we could end up with overlapping regions of commonality.
                    //
                    // This can occur in incremental settings when the similar tokens are written successsively.
                    // Because the parser can reuse underlying token data, it may end up with many incrementally
                    // identical tokens in a row.
                    if (currentOld.IsIncrementallyIdenticalTo(currentNew) &&
                        currentOld.FullSpan.Start >= commonLeftWidth &&
                        currentNew.FullSpan.Start >= commonLeftWidth)
                    {
                        continue;
                    }

                    // if we reached a token for either of these, then we can't break things down any further, and we hit
                    // the furthest point they are common.
                    if (currentOld.IsToken || currentNew.IsToken)
                        return oldRoot.FullSpan.End - currentOld.FullSpan.End;

                    // Similarly, if we've run out of time, just return what we've computed so far.  It's not as accurate as
                    // we could be.  But the caller wants the results asap.
                    if (stopwatch.Elapsed > timeout)
                        return oldRoot.FullSpan.End - currentOld.FullSpan.End;

                    // we've got two nodes, but they weren't the same.  For example, say we made an edit in a method in the
                    // class, the class node would be new, but there might be many member nodes following the edited node
                    // that were the same that we'd want to see and skip.  Crumble the node and deal with its right side.
                    //
                    // Do not reverse the children.  We want to process the rightmost child first and walk right to left.
                    foreach (var nodeOrToken in currentOld.AsNode()!.ChildNodesAndTokens())
                        oldStack.Push(nodeOrToken);

                    foreach (var nodeOrToken in currentNew.AsNode()!.ChildNodesAndTokens())
                        newStack.Push(nodeOrToken);
                }

                // If we consumed all of 'new', then the length of the new doc is what we have in common.
                if (oldStack.Count > 0)
                    return newRoot.FullSpan.Length;

                // If we consumed all of 'old', then the length of the old doc is what we have in common.
                if (newStack.Count > 0)
                    return oldRoot.FullSpan.Length;

                // We consumed both stacks entirely.  That means the trees were identical (though the root was
                // different). We should never get here.  If we were the same, then walking from the left should have
                // consumed everything and already bailed out.
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
