// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification;

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
    private static readonly ObjectPool<Stack<ChildSyntaxList.Enumerator>> s_enumeratorPool = new(() => new());
    private static readonly ObjectPool<Stack<ChildSyntaxList.Reversed.Enumerator>> s_reversedEnumeratorPool = new(() => new());

    public static TextChangeRange ComputeSyntacticChangeRange(SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (oldRoot == newRoot)
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

        var commonLeftWidth = ComputeCommonLeftWidth();
        if (commonLeftWidth == null)
        {
            // The trees were effectively identical (even if the children were different).  Return that there was no
            // text change.
            return default;
        }

        // Only compute the right side if we have time for it.  Otherwise, assume there is nothing in common there.
        var commonRightWidth = 0;
        if (stopwatch.Elapsed < timeout)
            commonRightWidth = ComputeCommonRightWidth();

        var oldRootWidth = oldRoot.FullWidth();
        var newRootWidth = newRoot.FullWidth();

        Contract.ThrowIfTrue(commonLeftWidth > oldRootWidth);
        Contract.ThrowIfTrue(commonLeftWidth > newRootWidth);
        Contract.ThrowIfTrue(commonRightWidth > oldRootWidth);
        Contract.ThrowIfTrue(commonRightWidth > newRootWidth);

        // it's possible for the common left/right to overlap.  This can happen as some tokens
        // in the parser have a true shared underlying state, so they may get consumed both on 
        // a leftward and rightward walk.  Cap the right width so that it never overlaps hte left
        // width in either the old or new tree.
        commonRightWidth = Math.Min(commonRightWidth, oldRootWidth - commonLeftWidth.Value);
        commonRightWidth = Math.Min(commonRightWidth, newRootWidth - commonLeftWidth.Value);

        return new TextChangeRange(
            TextSpan.FromBounds(start: commonLeftWidth.Value, end: oldRootWidth - commonRightWidth),
            newRootWidth - commonLeftWidth.Value - commonRightWidth);

        int? ComputeCommonLeftWidth()
        {
            if (oldRoot.IsIncrementallyIdenticalTo(newRoot))
                return null;

            using var _1 = s_enumeratorPool.GetPooledObject(out var oldStack);
            using var _2 = s_enumeratorPool.GetPooledObject(out var newStack);

            oldStack.Push(oldRoot.ChildNodesAndTokens().GetEnumerator());
            newStack.Push(newRoot.ChildNodesAndTokens().GetEnumerator());

            while (oldStack.Count > 0 && newStack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (TryGetStackTopNodeOrToken(oldStack, out var currentOld)
                    && TryGetStackTopNodeOrToken(newStack, out var currentNew))
                {
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
                    oldStack.Push(currentOld.AsNode()!.ChildNodesAndTokens().GetEnumerator());
                    newStack.Push(currentNew.AsNode()!.ChildNodesAndTokens().GetEnumerator());
                }
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

        int ComputeCommonRightWidth()
        {
            using var rightOldStack = s_reversedEnumeratorPool.GetPooledObject();
            using var rightNewStack = s_reversedEnumeratorPool.GetPooledObject();

            var oldStack = rightOldStack.Object;
            var newStack = rightNewStack.Object;
            Contract.ThrowIfTrue(oldRoot.IsIncrementallyIdenticalTo(newRoot));

            oldStack.Push(oldRoot.ChildNodesAndTokens().Reverse().GetEnumerator());
            newStack.Push(newRoot.ChildNodesAndTokens().Reverse().GetEnumerator());

            while (oldStack.Count > 0 && newStack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryGetStackTopNodeOrToken(oldStack, out var currentOld)
                    && TryGetStackTopNodeOrToken(newStack, out var currentNew))
                {
                    // The width on the right we've moved past on both old/new should be the same.
                    Contract.ThrowIfFalse((oldRoot.FullSpan.End - currentOld.FullSpan.End) ==
                                          (newRoot.FullSpan.End - currentNew.FullSpan.End));

                    // If the two nodes/tokens were the same just skip past them.  They're part of the common right width.
                    // Critically though, we can only skip past if this wasn't already something we consumed when determining
                    // the common left width.  If this was common the left side, we can't consider it common to the right,
                    // otherwise we could end up with overlapping regions of commonality.
                    //
                    // This can occur in incremental settings when the similar tokens are written successively.
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
                    // Reverse the enumerator to visit the right child first
                    oldStack.Push(currentOld.AsNode()!.ChildNodesAndTokens().Reverse().GetEnumerator());
                    newStack.Push(currentNew.AsNode()!.ChildNodesAndTokens().Reverse().GetEnumerator());
                }
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
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static bool TryGetStackTopNodeOrToken(Stack<ChildSyntaxList.Enumerator> stack, out SyntaxNodeOrToken syntaxNodeOrToken)
    {
        while (stack.TryPop(out var topEnumerator))
        {
            if (topEnumerator.MoveNext())
            {
                syntaxNodeOrToken = topEnumerator.Current;
                // Enumerator is struct, and it is using an int to track the index of the current item. So pop & push to update the enumerator
                stack.Push(topEnumerator);
                return true;
            }
        }

        syntaxNodeOrToken = default;
        return false;
    }

    private static bool TryGetStackTopNodeOrToken(Stack<ChildSyntaxList.Reversed.Enumerator> stack, out SyntaxNodeOrToken syntaxNodeOrToken)
    {
        while (stack.TryPop(out var topEnumerator))
        {
            if (topEnumerator.MoveNext())
            {
                syntaxNodeOrToken = topEnumerator.Current;
                // Enumerator is struct, and it is using an int to track the index of the current item. So pop & push to update the enumerator
                stack.Push(topEnumerator);
                return true;
            }
        }

        syntaxNodeOrToken = default;
        return false;
    }
}
