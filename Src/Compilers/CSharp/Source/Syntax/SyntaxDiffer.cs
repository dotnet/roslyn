using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal class SyntaxDiffer
    {
        private const int MaxSearchLength = 8;
        private readonly List<SyntaxNodeOrToken> oldNodes;
        private readonly List<SyntaxNodeOrToken> newNodes;
        private readonly List<ChangeRecord> changes;
        private readonly TextSpan oldSpan;
        private readonly bool computeNewText;

        private SyntaxDiffer(SyntaxNode oldNode, SyntaxNode newNode, bool computeNewText)
        {
            this.oldNodes = new List<SyntaxNodeOrToken>();
            this.oldNodes.Add(oldNode);
            this.newNodes = new List<SyntaxNodeOrToken>();
            this.newNodes.Add(newNode);
            this.changes = new List<ChangeRecord>();
            this.oldSpan = oldNode.FullSpan;
            this.computeNewText = computeNewText;
        }

        // return a set of text changes that when applied to the old document produces the new document
        internal static IList<TextChange> GetTextChanges(SyntaxNode oldNode, SyntaxNode newNode)
        {
            var differ = new SyntaxDiffer(oldNode, newNode, true);
            differ.ComputeChanges();
            return ConvertToTextChangesFromOld(differ.changes);
        }

        // return which spans of text in the new document are possibly different than text in the old document
        internal static IList<TextSpan> GetPossiblyDifferentTextSpans(SyntaxNode oldNode, SyntaxNode newNode)
        {
            var differ = new SyntaxDiffer(oldNode, newNode, false);
            differ.ComputeChanges();
            return ConvertToSpansInNew(differ.changes);
        }

        private void ComputeChanges()
        {
            while (true)
            {
                // first check end-of-lists termination cases...
                if (newNodes.Count == 0)
                {
                    // remaining old nodes are deleted
                    if (this.oldNodes.Count > 0)
                    {
                        RecordDeleteOld(this.oldNodes.Count);
                    }
                    break;
                }
                else if (oldNodes.Count == 0)
                {
                    // remaining nodes were inserted
                    if (this.newNodes.Count > 0)
                    {
                        RecordInsertNew(this.newNodes.Count);
                    }
                    break;
                }
                else
                {
                    var action = GetNextAction();
                    switch (action.Operation)
                    {
                        case DiffOp.SkipBoth:
                            RemoveFirst(this.oldNodes, action.Count);
                            RemoveFirst(this.newNodes, action.Count);
                            break;
                        case DiffOp.ReduceOld:
                            ReplaceFirstWithChildren(this.oldNodes);
                            break;
                        case DiffOp.ReduceNew:
                            ReplaceFirstWithChildren(this.newNodes);
                            break;
                        case DiffOp.ReduceBoth:
                            ReplaceFirstWithChildren(this.oldNodes);
                            ReplaceFirstWithChildren(this.newNodes);
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

        enum DiffOp
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

        struct DiffAction
        {
            public readonly DiffOp Operation;
            public readonly int Count;

            public DiffAction(DiffOp operation, int count = 0)
            {
                this.Operation = operation;
                this.Count = count;
            }
        }

        private DiffAction GetNextAction()
        {
            bool oldIsToken = oldNodes[0].IsToken;
            bool newIsToken = newNodes[0].IsToken;

            // look for exact match
            int indexOfOldInNew;
            int similarityOfOldInNew;
            int indexOfNewInOld;
            int similarityOfNewInOld;

            FindBestMatch(newNodes, oldNodes[0], out indexOfOldInNew, out similarityOfOldInNew);
            FindBestMatch(oldNodes, newNodes[0], out indexOfNewInOld, out similarityOfNewInOld);

            if (indexOfOldInNew == 0 && indexOfNewInOld == 0)
            {
                if (AreIdentical(oldNodes[0], newNodes[0]))
                {
                    return new DiffAction(DiffOp.SkipBoth, 1);
                }
                else if (!oldIsToken && !newIsToken)
                {
                    return new DiffAction(DiffOp.ReduceBoth, 1);
                }
                else 
                {
                    return new DiffAction(DiffOp.ReplaceOldWithNew, 1);
                }
            }
            else if (indexOfOldInNew >= 0 || indexOfNewInOld >= 0)
            {
                if (indexOfNewInOld < 0 || similarityOfOldInNew >= similarityOfNewInOld)
                {
                    if (indexOfOldInNew > 0)
                    {
                        return new DiffAction(DiffOp.InsertNew, indexOfOldInNew);
                    }
                    else if (!newIsToken)
                    {
                        if (AreSimilar(oldNodes[0], newNodes[0]))
                        {
                            return new DiffAction(DiffOp.ReduceBoth);
                        }
                        else
                        {
                            return new DiffAction(DiffOp.ReduceNew);
                        }
                    }
                    else
                    {
                        return new DiffAction(DiffOp.ReplaceOldWithNew);
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
                        if (AreSimilar(oldNodes[0], newNodes[0]))
                        {
                            return new DiffAction(DiffOp.ReduceBoth);
                        }
                        else
                        {
                            return new DiffAction(DiffOp.ReduceOld, 1);
                        }
                    }
                    else
                    {
                        return new DiffAction(DiffOp.ReplaceOldWithNew);
                    }
                }
            }
            else
            {
                if (!oldIsToken && !newIsToken)
                {
                    // check similarity anyway
                    var sim = GetSimilarity(oldNodes[0], newNodes[0]);
                    if (sim >= Math.Max(oldNodes[0].FullWidth, newNodes[0].FullWidth))
                    {
                        return new DiffAction(DiffOp.ReduceBoth, 1);
                    }
                }
                return new DiffAction(DiffOp.ReplaceOldWithNew, 1);
            }
        }

        private static void ReplaceFirstWithChildren(List<SyntaxNodeOrToken> list)
        {
            var node = list[0];
            list.RemoveAt(0);
            foreach (var child in node.Children.Reverse())
            {
                if (child.FullWidth > 0)
                {
                    list.Insert(0, child);
                }
            }
        }

        private void FindBestMatch(List<SyntaxNodeOrToken> list, SyntaxNodeOrToken node, out int index, out int similarity)
        {
            index = -1;
            similarity = -1;

            for (int i = 0, n = list.Count; i < n; i++)
            {
                if (i >= MaxSearchLength)
                {
                    break;
                }

                var listNode = list[i];
                if (AreIdentical(listNode, node))
                {
                    index = i;
                    similarity = node.FullWidth;
                    return;
                }
                else if (AreSimilar(listNode, node))
                {
                    var sim = GetSimilarity(listNode, node);
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
                    foreach (var child in listNode.Children)
                    {
                        if (j >= MaxSearchLength)
                        {
                            break;
                        }

                        j++;

                        if (AreIdentical(child, node))
                        {
                            index = i;
                            similarity = node.FullWidth;
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
        }

        private HashSet<InternalSyntax.SyntaxNode> similaritySet = new HashSet<InternalSyntax.SyntaxNode>();

        private int GetSimilarity(SyntaxNodeOrToken node1, SyntaxNodeOrToken node2)
        {
            // count the characters in the common/identical nodes
            int w = 0;
            similaritySet.Clear();

            foreach (var n1 in node1.Children)
            {
                similaritySet.Add(n1.UnderlyingNode.ToGreen());
            }

            foreach (var n2 in node2.Children)
            {
                if (similaritySet.Contains(n2.UnderlyingNode.ToGreen()))
                {
                    w += n2.FullWidth;
                }
            }

            return w;
        }

        private static bool AreIdentical(SyntaxNodeOrToken node1, SyntaxNodeOrToken node2)
        {
            return SyntaxNodeOrToken.AreIdentical(node1, node2);
        }

        private static bool AreSimilar(SyntaxNodeOrToken node1, SyntaxNodeOrToken node2)
        {
            return node1.Kind == node2.Kind;
        }

        struct ChangeRecord
        {
            public readonly TextChangeRange Range;
            public readonly List<SyntaxNodeOrToken> NewNodes;

            internal ChangeRecord(TextChangeRange range, List<SyntaxNodeOrToken> newNodes)
            {
                this.Range = range;
                this.NewNodes = newNodes;
            }
        }

        private void RecordDeleteOld(int oldNodeCount)
        {
            var oldSpan = GetSpan(this.oldNodes, 0, oldNodeCount);
            RemoveFirst(this.oldNodes, oldNodeCount);
            RecordChange(new ChangeRecord(new TextChangeRange(oldSpan, 0), null));
        }

        private void RecordReplaceOldWithNew(int oldNodeCount, int newNodeCount)
        {
            var oldSpan = GetSpan(this.oldNodes, 0, oldNodeCount);
            RemoveFirst(this.oldNodes, oldNodeCount);
            var newSpan = GetSpan(this.newNodes, 0, newNodeCount);
            var newList = computeNewText ? CopyFirst(this.newNodes, newNodeCount) : null;
            RemoveFirst(this.newNodes, newNodeCount);
            RecordChange(new ChangeRecord(new TextChangeRange(oldSpan, newSpan.Length), newList));
        }

        private void RecordInsertNew(int newNodeCount)
        {
            var newSpan = GetSpan(this.newNodes, 0, newNodeCount);
            var newList = computeNewText ? CopyFirst(this.newNodes, newNodeCount) : null;
            RemoveFirst(this.newNodes, newNodeCount);
            int start = this.oldNodes.Count > 0 ? this.oldNodes[0].FullSpan.Start : this.oldSpan.End;
            RecordChange(new ChangeRecord(new TextChangeRange(new TextSpan(start, 0), newSpan.Length), newList));
        }

        private void RecordChange(ChangeRecord change)
        {
            if (this.changes.Count > 0)
            {
                var last = this.changes[this.changes.Count - 1];
                if (last.Range.Span.End == change.Range.Span.Start)
                {
                    // merge changes...
                    this.changes[this.changes.Count - 1] = new ChangeRecord(
                        new TextChangeRange(new TextSpan(last.Range.Span.Start, last.Range.Span.Length + change.Range.Span.Length), last.Range.NewLength + change.Range.NewLength),
                        Combine(last.NewNodes, change.NewNodes)
                        );
                    return;
                }
                System.Diagnostics.Debug.Assert(change.Range.Span.Start >= last.Range.Span.End);
            }
            this.changes.Add(change);
        }

        private static TextSpan GetSpan(List<SyntaxNodeOrToken> list, int first, int length)
        {
            int start = list[first].FullSpan.Start;
            int end = list[first + length - 1].FullSpan.End;
            return new TextSpan(start, end - start);
        }

        private static List<SyntaxNodeOrToken> Combine(List<SyntaxNodeOrToken> first, List<SyntaxNodeOrToken> next)
        {
            if (first == null)
                return next;
            if (next == null)
                return first;
            first.AddRange(next);
            return first;
        }

        private static List<SyntaxNodeOrToken> CopyFirst(List<SyntaxNodeOrToken> list, int n)
        {
            if (n == 0)
                return null;
            var newList = new List<SyntaxNodeOrToken>(n);
            for (int i = 0; i < n; i++)
            {
                newList.Add(list[i]);
            }
            return newList;
        }

        private static void RemoveFirst(List<SyntaxNodeOrToken> list, int count)
        {
            while (count > 0)
            {
                list.RemoveAt(0);
                count--;
            }
        }

        private static IList<TextChange> ConvertToTextChangesFromOld(List<ChangeRecord> changes)
        {
            return changes.Select(c => new TextChange(c.Range.Span, GetText(c.NewNodes))).ToList();
        }

        private static string GetText(List<SyntaxNodeOrToken> list)
        {
            if (list == null || list.Count == 0)
            {
                return string.Empty;
            }
            var span = GetSpan(list, 0, list.Count);
            var builder = new StringBuilder(span.Length);
            for (int i = 0; i < list.Count; i++)
            {
                builder.Append(list[i].GetFullText());
            }
            return builder.ToString();
        }

        private static IList<TextSpan> ConvertToSpansInNew(List<ChangeRecord> changesToOld)
        {
            // this algorithm assumes changes are in non-overlapping document order
            var newSpans = new List<TextSpan>();
            int delta = 0; // difference between old & new start positions
            foreach (var change in changesToOld)
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
    }
}