// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class SyntaxDiffer
    {
        private const int InitialStackSize = 8;
        private const int MaxSearchLength = 8;
        private readonly Stack<SyntaxNodeOrToken> _oldNodes = new Stack<SyntaxNodeOrToken>(InitialStackSize);
        private readonly Stack<SyntaxNodeOrToken> _newNodes = new Stack<SyntaxNodeOrToken>(InitialStackSize);
        private readonly List<ChangeRecord> _changes = new List<ChangeRecord>();
        private readonly TextSpan _oldSpan;
        private readonly bool _computeNewText;
        private readonly HashSet<GreenNode> _nodeSimilaritySet = new HashSet<GreenNode>();
        private readonly HashSet<string> _tokenTextSimilaritySet = new HashSet<string>();

        private SyntaxDiffer(SyntaxNode oldNode, SyntaxNode newNode, bool computeNewText)
        {
            _oldNodes.Push((SyntaxNodeOrToken)oldNode);
            _newNodes.Push((SyntaxNodeOrToken)newNode);

            _oldSpan = oldNode.FullSpan;
            _computeNewText = computeNewText;
        }

        // return a set of text changes that when applied to the old document produces the new document
        internal static IList<TextChange> GetTextChanges(SyntaxTree before, SyntaxTree after)
        {
            if (before == after)
            {
                return SpecializedCollections.EmptyList<TextChange>();
            }
            else if (before == null)
            {
                return new[] { new TextChange(new TextSpan(0, 0), after.GetText().ToString()) };
            }
            else if (after == null)
            {
                throw new ArgumentNullException(nameof(after));
            }
            else
            {
                return GetTextChanges(before.GetRoot(), after.GetRoot());
            }
        }

        // return a set of text changes that when applied to the old document produces the new document
        internal static IList<TextChange> GetTextChanges(SyntaxNode oldNode, SyntaxNode newNode)
        {
            return new SyntaxDiffer(oldNode, newNode, computeNewText: true).ComputeTextChangesFromOld();
        }

        private IList<TextChange> ComputeTextChangesFromOld()
        {
            this.ComputeChangeRecords();
            var reducedChanges = this.ReduceChanges(_changes);

            return reducedChanges.Select(c => new TextChange(c.Range.Span, c.NewText)).ToList();
        }

        internal static IList<TextSpan> GetPossiblyDifferentTextSpans(SyntaxTree before, SyntaxTree after)
        {
            if (before == after)
            {
                // They're the same, so nothing changed.
                return SpecializedCollections.EmptyList<TextSpan>();
            }
            else if (before == null)
            {
                // The tree is completely new, everything has changed.
                return new[] { new TextSpan(0, after.GetText().Length) };
            }
            else if (after == null)
            {
                throw new ArgumentNullException(nameof(after));
            }
            else
            {
                return GetPossiblyDifferentTextSpans(before.GetRoot(), after.GetRoot());
            }
        }

        // return which spans of text in the new document are possibly different than text in the old document
        internal static IList<TextSpan> GetPossiblyDifferentTextSpans(SyntaxNode oldNode, SyntaxNode newNode)
        {
            return new SyntaxDiffer(oldNode, newNode, computeNewText: false).ComputeSpansInNew();
        }

        private IList<TextSpan> ComputeSpansInNew()
        {
            this.ComputeChangeRecords();
            var reducedChanges = ReduceChanges(_changes);

            // this algorithm assumes changes are in non-overlapping document order
            var newSpans = new List<TextSpan>();
            int delta = 0; // difference between old & new start positions
            foreach (var change in reducedChanges)
            {
                if (change.Range.NewLength > 0) // delete-only ranges cannot be expressed as part of new text
                {
                    int start = change.Range.Span.Start + delta;
                    newSpans.Add(new TextSpan(start, change.Range.NewLength));
                }

                delta += change.Range.NewLength - change.Range.Span.Length;
            }

            return newSpans;
        }

        private void ComputeChangeRecords()
        {
            while (true)
            {
                // first check end-of-lists termination cases...
                if (_newNodes.Count == 0)
                {
                    // remaining old nodes are deleted
                    if (_oldNodes.Count > 0)
                    {
                        RecordDeleteOld(_oldNodes.Count);
                    }
                    break;
                }
                else if (_oldNodes.Count == 0)
                {
                    // remaining nodes were inserted
                    if (_newNodes.Count > 0)
                    {
                        RecordInsertNew(_newNodes.Count);
                    }
                    break;
                }
                else
                {
                    var action = GetNextAction();
                    switch (action.Operation)
                    {
                        case DiffOp.SkipBoth:
                            RemoveFirst(_oldNodes, action.Count);
                            RemoveFirst(_newNodes, action.Count);
                            break;
                        case DiffOp.ReduceOld:
                            ReplaceFirstWithChildren(_oldNodes);
                            break;
                        case DiffOp.ReduceNew:
                            ReplaceFirstWithChildren(_newNodes);
                            break;
                        case DiffOp.ReduceBoth:
                            ReplaceFirstWithChildren(_oldNodes);
                            ReplaceFirstWithChildren(_newNodes);
                            break;
                        case DiffOp.InsertNew:
                            RecordInsertNew(action.Count);
                            break;
                        case DiffOp.DeleteOld:
                            RecordDeleteOld(action.Count);
                            break;
                        case DiffOp.ReplaceOldWithNew:
                            RecordReplaceOldWithNew(action.Count, action.Count);
                            break;
                    }
                }
            }
        }

        private enum DiffOp
        {
            None = 0,
            SkipBoth,
            ReduceOld,
            ReduceNew,
            ReduceBoth,
            InsertNew,
            DeleteOld,
            ReplaceOldWithNew
        }

        private struct DiffAction
        {
            public readonly DiffOp Operation;
            public readonly int Count;

            public DiffAction(DiffOp operation, int count)
            {
                System.Diagnostics.Debug.Assert(count >= 0);
                this.Operation = operation;
                this.Count = count;
            }
        }

        private DiffAction GetNextAction()
        {
            bool oldIsToken = _oldNodes.Peek().IsToken;
            bool newIsToken = _newNodes.Peek().IsToken;

            // look for exact match
            int indexOfOldInNew;
            int similarityOfOldInNew;
            int indexOfNewInOld;
            int similarityOfNewInOld;

            FindBestMatch(_newNodes, _oldNodes.Peek(), out indexOfOldInNew, out similarityOfOldInNew);
            FindBestMatch(_oldNodes, _newNodes.Peek(), out indexOfNewInOld, out similarityOfNewInOld);

            if (indexOfOldInNew == 0 && indexOfNewInOld == 0)
            {
                // both first nodes are somewhat similar to each other

                if (AreIdentical(_oldNodes.Peek(), _newNodes.Peek()))
                {
                    // they are identical, so just skip over both first new and old nodes.
                    return new DiffAction(DiffOp.SkipBoth, 1);
                }
                else if (!oldIsToken && !newIsToken)
                {
                    // neither are tokens, so replace each first node with its child nodes
                    return new DiffAction(DiffOp.ReduceBoth, 1);
                }
                else
                {
                    // otherwise just claim one's text replaces the other.. 
                    // NOTE: possibly we can improve this by reducing the side that may not be token?
                    return new DiffAction(DiffOp.ReplaceOldWithNew, 1);
                }
            }
            else if (indexOfOldInNew >= 0 || indexOfNewInOld >= 0)
            {
                // either the first old-node is similar to some node in the new-list or
                // the first new-node is similar to some node in the old-list

                if (indexOfNewInOld < 0 || similarityOfOldInNew >= similarityOfNewInOld)
                {
                    // either there is no match for the first new-node in the old-list or the 
                    // the similarity of the first old-node in the new-list is much greater

                    // if we find a match for the old node in the new list, that probably means nodes were inserted before it.
                    if (indexOfOldInNew > 0)
                    {
                        // look ahead to see if the old node also appears again later in its own list
                        int indexOfOldInOld;
                        int similarityOfOldInOld;
                        FindBestMatch(_oldNodes, _oldNodes.Peek(), out indexOfOldInOld, out similarityOfOldInOld, 1);

                        // don't declare an insert if the node also appeared later in the original list
                        var oldHasSimilarSibling = (indexOfOldInOld >= 1 && similarityOfOldInOld >= similarityOfOldInNew);
                        if (!oldHasSimilarSibling)
                        {
                            return new DiffAction(DiffOp.InsertNew, indexOfOldInNew);
                        }
                    }

                    if (!newIsToken)
                    {
                        if (AreSimilar(_oldNodes.Peek(), _newNodes.Peek()))
                        {
                            return new DiffAction(DiffOp.ReduceBoth, 1);
                        }
                        else
                        {
                            return new DiffAction(DiffOp.ReduceNew, 1);
                        }
                    }
                    else
                    {
                        return new DiffAction(DiffOp.ReplaceOldWithNew, 1);
                    }
                }
                else
                {
                    if (indexOfNewInOld > 0)
                    {
                        return new DiffAction(DiffOp.DeleteOld, indexOfNewInOld);
                    }
                    else if (!oldIsToken)
                    {
                        if (AreSimilar(_oldNodes.Peek(), _newNodes.Peek()))
                        {
                            return new DiffAction(DiffOp.ReduceBoth, 1);
                        }
                        else
                        {
                            return new DiffAction(DiffOp.ReduceOld, 1);
                        }
                    }
                    else
                    {
                        return new DiffAction(DiffOp.ReplaceOldWithNew, 1);
                    }
                }
            }
            else
            {
                // no similarities between first node of old-list in new-list or between first new-node in old-list

                if (!oldIsToken && !newIsToken)
                {
                    // check similarity anyway
                    var sim = GetSimilarity(_oldNodes.Peek(), _newNodes.Peek());
                    if (sim >= Math.Max(_oldNodes.Peek().FullSpan.Length, _newNodes.Peek().FullSpan.Length))
                    {
                        return new DiffAction(DiffOp.ReduceBoth, 1);
                    }
                }

                return new DiffAction(DiffOp.ReplaceOldWithNew, 1);
            }
        }

        private static void ReplaceFirstWithChildren(Stack<SyntaxNodeOrToken> stack)
        {
            var node = stack.Pop();

            int c = 0;
            var children = new SyntaxNodeOrToken[node.ChildNodesAndTokens().Count];
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.FullSpan.Length > 0)
                {
                    children[c] = child;
                    c++;
                }
            }

            for (int i = c - 1; i >= 0; i--)
            {
                stack.Push(children[i]);
            }
        }

        private void FindBestMatch(Stack<SyntaxNodeOrToken> stack, in SyntaxNodeOrToken node, out int index, out int similarity, int startIndex = 0)
        {
            index = -1;
            similarity = -1;

            int i = 0;
            foreach (var stackNode in stack)
            {
                if (i >= MaxSearchLength)
                {
                    break;
                }

                if (i >= startIndex)
                {
                    if (AreIdentical(stackNode, node))
                    {
                        var sim = node.FullSpan.Length;
                        if (sim > similarity)
                        {
                            index = i;
                            similarity = sim;
                            return;
                        }
                    }
                    else if (AreSimilar(stackNode, node))
                    {
                        var sim = GetSimilarity(stackNode, node);

                        // Are these really the same? This may be expensive so only check this if 
                        // similarity is rated equal to them being identical.
                        if (sim == node.FullSpan.Length && node.IsToken)
                        {
                            if (stackNode.ToFullString() == node.ToFullString())
                            {
                                index = i;
                                similarity = sim;
                                return;
                            }
                        }

                        if (sim > similarity)
                        {
                            index = i;
                            similarity = sim;
                        }
                    }
                    else
                    {
                        // check one level deep inside list node's children
                        int j = 0;
                        foreach (var child in stackNode.ChildNodesAndTokens())
                        {
                            if (j >= MaxSearchLength)
                            {
                                break;
                            }

                            j++;

                            if (AreIdentical(child, node))
                            {
                                index = i;
                                similarity = node.FullSpan.Length;
                                return;
                            }
                            else if (AreSimilar(child, node))
                            {
                                var sim = GetSimilarity(child, node);
                                if (sim > similarity)
                                {
                                    index = i;
                                    similarity = sim;
                                }
                            }
                        }
                    }
                }

                i++;
            }
        }

        private int GetSimilarity(in SyntaxNodeOrToken node1, in SyntaxNodeOrToken node2)
        {
            // count the characters in the common/identical nodes
            int w = 0;
            _nodeSimilaritySet.Clear();
            _tokenTextSimilaritySet.Clear();

            if (node1.IsToken && node2.IsToken)
            {
                var text1 = node1.ToString();
                var text2 = node2.ToString();

                if (text1 == text2)
                {
                    // main text of token is the same
                    w += text1.Length;
                }

                foreach (var tr in node1.GetLeadingTrivia())
                {
                    _nodeSimilaritySet.Add(tr.UnderlyingNode);
                }

                foreach (var tr in node1.GetTrailingTrivia())
                {
                    _nodeSimilaritySet.Add(tr.UnderlyingNode);
                }

                foreach (var tr in node2.GetLeadingTrivia())
                {
                    if (_nodeSimilaritySet.Contains(tr.UnderlyingNode))
                    {
                        w += tr.FullSpan.Length;
                    }
                }

                foreach (var tr in node2.GetTrailingTrivia())
                {
                    if (_nodeSimilaritySet.Contains(tr.UnderlyingNode))
                    {
                        w += tr.FullSpan.Length;
                    }
                }
            }
            else
            {
                foreach (var n1 in node1.ChildNodesAndTokens())
                {
                    _nodeSimilaritySet.Add(n1.UnderlyingNode);

                    if (n1.IsToken)
                    {
                        _tokenTextSimilaritySet.Add(n1.ToString());
                    }
                }

                foreach (var n2 in node2.ChildNodesAndTokens())
                {
                    if (_nodeSimilaritySet.Contains(n2.UnderlyingNode))
                    {
                        w += n2.FullSpan.Length;
                    }
                    else if (n2.IsToken)
                    {
                        var tokenText = n2.ToString();
                        if (_tokenTextSimilaritySet.Contains(tokenText))
                        {
                            w += tokenText.Length;
                        }
                    }
                }
            }

            return w;
        }

        private static bool AreIdentical(in SyntaxNodeOrToken node1, in SyntaxNodeOrToken node2)
        {
            return node1.UnderlyingNode == node2.UnderlyingNode;
        }

        private static bool AreSimilar(in SyntaxNodeOrToken node1, in SyntaxNodeOrToken node2)
        {
            return node1.RawKind == node2.RawKind;
        }

        private struct ChangeRecord
        {
            public readonly TextChangeRange Range;
            public readonly Queue<SyntaxNodeOrToken> OldNodes;
            public readonly Queue<SyntaxNodeOrToken> NewNodes;

            internal ChangeRecord(TextChangeRange range, Queue<SyntaxNodeOrToken> oldNodes, Queue<SyntaxNodeOrToken> newNodes)
            {
                this.Range = range;
                this.OldNodes = oldNodes;
                this.NewNodes = newNodes;
            }
        }

        private void RecordDeleteOld(int oldNodeCount)
        {
            var oldSpan = GetSpan(_oldNodes, 0, oldNodeCount);
            var removedNodes = CopyFirst(_oldNodes, oldNodeCount);
            RemoveFirst(_oldNodes, oldNodeCount);
            RecordChange(new ChangeRecord(new TextChangeRange(oldSpan, 0), removedNodes, null));
        }

        private void RecordReplaceOldWithNew(int oldNodeCount, int newNodeCount)
        {
            if (oldNodeCount == 1 && newNodeCount == 1)
            {
                // Avoid creating a Queue<T> which we immediately discard in the most common case for old/new counts
                var removedNode = _oldNodes.Pop();
                var oldSpan = removedNode.FullSpan;

                var insertedNode = _newNodes.Pop();
                var newSpan = insertedNode.FullSpan;

                RecordChange(new TextChangeRange(oldSpan, newSpan.Length), removedNode, insertedNode);
            }
            else
            {
                var oldSpan = GetSpan(_oldNodes, 0, oldNodeCount);
                var removedNodes = CopyFirst(_oldNodes, oldNodeCount);
                RemoveFirst(_oldNodes, oldNodeCount);
                var newSpan = GetSpan(_newNodes, 0, newNodeCount);
                var insertedNodes = CopyFirst(_newNodes, newNodeCount);
                RemoveFirst(_newNodes, newNodeCount);
                RecordChange(new ChangeRecord(new TextChangeRange(oldSpan, newSpan.Length), removedNodes, insertedNodes));
            }
        }

        private void RecordInsertNew(int newNodeCount)
        {
            var newSpan = GetSpan(_newNodes, 0, newNodeCount);
            var insertedNodes = CopyFirst(_newNodes, newNodeCount);
            RemoveFirst(_newNodes, newNodeCount);
            int start = _oldNodes.Count > 0 ? _oldNodes.Peek().Position : _oldSpan.End;
            RecordChange(new ChangeRecord(new TextChangeRange(new TextSpan(start, 0), newSpan.Length), null, insertedNodes));
        }

        private void RecordChange(ChangeRecord change)
        {
            if (_changes.Count > 0)
            {
                var last = _changes[_changes.Count - 1];
                if (last.Range.Span.End == change.Range.Span.Start)
                {
                    // merge changes...
                    _changes[_changes.Count - 1] = new ChangeRecord(
                        new TextChangeRange(new TextSpan(last.Range.Span.Start, last.Range.Span.Length + change.Range.Span.Length), last.Range.NewLength + change.Range.NewLength),
                        Combine(last.OldNodes, change.OldNodes),
                        Combine(last.NewNodes, change.NewNodes));
                    return;
                }

                Debug.Assert(change.Range.Span.Start >= last.Range.Span.End);
            }

            _changes.Add(change);
        }

        private void RecordChange(TextChangeRange textChangeRange, in SyntaxNodeOrToken removedNode, SyntaxNodeOrToken insertedNode)
        {
            if (_changes.Count > 0)
            {
                var last = _changes[_changes.Count - 1];
                if (last.Range.Span.End == textChangeRange.Span.Start)
                {
                    // merge changes...
                    last.OldNodes?.Enqueue(removedNode);
                    last.NewNodes?.Enqueue(insertedNode);
                    _changes[_changes.Count - 1] = new ChangeRecord(
                        new TextChangeRange(new TextSpan(last.Range.Span.Start, last.Range.Span.Length + textChangeRange.Span.Length), last.Range.NewLength + textChangeRange.NewLength),
                        last.OldNodes ?? CreateQueue(removedNode),
                        last.NewNodes ?? CreateQueue(insertedNode));
                    return;
                }

                Debug.Assert(textChangeRange.Span.Start >= last.Range.Span.End);
            }

            _changes.Add(new ChangeRecord(textChangeRange, CreateQueue(removedNode), CreateQueue(insertedNode)));

            // Local Functions
            Queue<SyntaxNodeOrToken> CreateQueue(SyntaxNodeOrToken nodeOrToken)
            {
                var queue = new Queue<SyntaxNodeOrToken>();
                queue.Enqueue(nodeOrToken);
                return queue;
            }
        }

        private static TextSpan GetSpan(Stack<SyntaxNodeOrToken> stack, int first, int length)
        {
            int start = -1, end = -1, i = 0;
            foreach (var n in stack)
            {
                if (i == first)
                {
                    start = n.Position;
                }

                if (i == first + length - 1)
                {
                    end = n.EndPosition;
                    break;
                }

                i++;
            }

            Debug.Assert(start >= 0);
            Debug.Assert(end >= 0);

            return TextSpan.FromBounds(start, end);
        }

        private static TextSpan GetSpan(Queue<SyntaxNodeOrToken> queue, int first, int length)
        {
            int start = -1, end = -1, i = 0;
            foreach (var n in queue)
            {
                if (i == first)
                {
                    start = n.Position;
                }

                if (i == first + length - 1)
                {
                    end = n.EndPosition;
                    break;
                }

                i++;
            }

            Debug.Assert(start >= 0);
            Debug.Assert(end >= 0);

            return TextSpan.FromBounds(start, end);
        }

        private static Queue<SyntaxNodeOrToken> Combine(Queue<SyntaxNodeOrToken> first, Queue<SyntaxNodeOrToken> next)
        {
            if (first == null || first.Count == 0)
            {
                return next;
            }

            if (next == null || next.Count == 0)
            {
                return first;
            }

            foreach (var nodeOrToken in next)
            {
                first.Enqueue(nodeOrToken);
            }

            return first;
        }

        private static Queue<SyntaxNodeOrToken> CopyFirst(Stack<SyntaxNodeOrToken> stack, int n)
        {
            if (n == 0)
            {
                return null;
            }

            var queue = new Queue<SyntaxNodeOrToken>(n);

            int remaining = n;
            foreach (var node in stack)
            {
                if (remaining == 0)
                {
                    break;
                }

                queue.Enqueue(node);
                remaining--;
            }

            return queue;
        }

        private static SyntaxNodeOrToken[] ToArray(Stack<SyntaxNodeOrToken> stack, int n)
        {
            var nodes = new SyntaxNodeOrToken[n];
            int i = n - 1;
            foreach (var node in stack)
            {
                nodes[i] = node;
                i--;

                if (i < 0)
                {
                    break;
                }
            }
            return nodes;
        }

        private static void RemoveFirst(Stack<SyntaxNodeOrToken> stack, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                stack.Pop();
            }
        }

        private struct ChangeRangeWithText
        {
            public readonly TextChangeRange Range;
            public readonly string NewText;

            public ChangeRangeWithText(TextChangeRange range, string newText)
            {
                this.Range = range;
                this.NewText = newText;
            }
        }

        private List<ChangeRangeWithText> ReduceChanges(List<ChangeRecord> changeRecords)
        {
            var textChanges = new List<ChangeRangeWithText>(changeRecords.Count);

            var oldText = new StringBuilder();
            var newText = new StringBuilder();

            foreach (var cr in changeRecords)
            {
                // try to reduce change range by finding common characters
                if (cr.Range.Span.Length > 0 && cr.Range.NewLength > 0)
                {
                    var range = cr.Range;

                    CopyText(cr.OldNodes, oldText);
                    CopyText(cr.NewNodes, newText);

                    int commonLeadingCount;
                    int commonTrailingCount;
                    GetCommonEdgeLengths(oldText, newText, out commonLeadingCount, out commonTrailingCount);

                    // did we have any common leading or trailing characters between the strings?
                    if (commonLeadingCount > 0 || commonTrailingCount > 0)
                    {
                        range = new TextChangeRange(
                            new TextSpan(range.Span.Start + commonLeadingCount, range.Span.Length - (commonLeadingCount + commonTrailingCount)),
                            range.NewLength - (commonLeadingCount + commonTrailingCount));

                        if (commonTrailingCount > 0)
                        {
                            newText.Remove(newText.Length - commonTrailingCount, commonTrailingCount);
                        }

                        if (commonLeadingCount > 0)
                        {
                            newText.Remove(0, commonLeadingCount);
                        }
                    }

                    // only include adjusted change if there is still a change 
                    if (range.Span.Length > 0 || range.NewLength > 0)
                    {
                        textChanges.Add(new ChangeRangeWithText(range, _computeNewText ? newText.ToString() : null));
                    }
                }
                else
                {
                    // pure inserts and deletes
                    textChanges.Add(new ChangeRangeWithText(cr.Range, _computeNewText ? GetText(cr.NewNodes) : null));
                }
            }

            return textChanges;
        }

        private static void GetCommonEdgeLengths(StringBuilder oldText, StringBuilder newText, out int commonLeadingCount, out int commonTrailingCount)
        {
            int maxChars = Math.Min(oldText.Length, newText.Length);

            commonLeadingCount = 0;
            for (; commonLeadingCount < maxChars; commonLeadingCount++)
            {
                if (oldText[commonLeadingCount] != newText[commonLeadingCount])
                {
                    break;
                }
            }

            // don't double count the chars we matched at the start of the strings
            maxChars = maxChars - commonLeadingCount;

            commonTrailingCount = 0;
            for (; commonTrailingCount < maxChars; commonTrailingCount++)
            {
                if (oldText[oldText.Length - commonTrailingCount - 1] != newText[newText.Length - commonTrailingCount - 1])
                {
                    break;
                }
            }
        }

        private static string GetText(Queue<SyntaxNodeOrToken> queue)
        {
            if (queue == null || queue.Count == 0)
            {
                return string.Empty;
            }

            var span = GetSpan(queue, 0, queue.Count);
            var builder = new StringBuilder(span.Length);

            CopyText(queue, builder);

            return builder.ToString();
        }

        private static void CopyText(Queue<SyntaxNodeOrToken> queue, StringBuilder builder)
        {
            builder.Length = 0;

            if (queue != null && queue.Count > 0)
            {
                var writer = new System.IO.StringWriter(builder);

                foreach (var n in queue)
                {
                    n.WriteTo(writer);
                }

                writer.Flush();
            }
        }
    }
}
