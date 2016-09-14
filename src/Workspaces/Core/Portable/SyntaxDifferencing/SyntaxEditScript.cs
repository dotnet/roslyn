// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    /// <summary>
    /// Represents a sequence of tree edits.
    /// </summary>
    internal sealed partial class SyntaxEditScript
    {
        private readonly SyntaxMatch _match;
        private readonly ImmutableArray<SyntaxEdit> _edits;

        internal SyntaxEditScript(SyntaxMatch match)
        {
            _match = match;

            var edits = new List<SyntaxEdit>();
            AddUpdatesInsertsMoves(edits);
            AddDeletes(edits);

            _edits = edits.AsImmutable();
        }

        public ImmutableArray<SyntaxEdit> Edits => _edits;

        public SyntaxMatch Match => _match;

        private TreeComparer Comparer => _match.Comparer;

        private SyntaxNode Root1 => _match.OldRoot;

        private SyntaxNode Root2 => _match.NewRoot;

        private void AddUpdatesInsertsMoves(List<SyntaxEdit> edits)
        {
            // Breadth-first traversal.
            ProcessNode(edits, Root2);

            var rootChildren = Comparer.GetChildren(Root2);
            if (rootChildren == null)
            {
                return;
            }

            var queue = new Queue<IEnumerable<SyntaxNode>>();
            queue.Enqueue(rootChildren);

            do
            {
                IEnumerable<SyntaxNode> children = queue.Dequeue();
                foreach (SyntaxNode child in children)
                {
                    ProcessNode(edits, child);

                    var grandChildren = Comparer.GetChildren(child);
                    if (grandChildren != null)
                    {
                        queue.Enqueue(grandChildren);
                    }
                }
            }
            while (queue.Count > 0);
        }

        private void ProcessNode(List<SyntaxEdit> edits, SyntaxNode x)
        {
            Debug.Assert(Comparer.TreesEqual(x, Root2));

            // NOTE:  
            // Our implementation differs from the algorithm described in the paper in following:
            // - We don't update M' and T1 since we don't need the final matching and the transformed tree.
            // - Insert and Move edits don't need to store the offset of the nodes relative to their parents,
            //   so we don't calculate those. Thus we don't need to implement FindPos.
            // - We don't mark nodes "in order" since the marks are only needed by FindPos.

            // a) 
            // Let x be the current node in the breadth-first search of T2. 
            // Let y = parent(x).
            // Let z be the partner of parent(x) in M'.  (note: we don't need z for insert)
            //
            // NOTE:
            // If we needed z then we would need to be updating M' as we encounter insertions.

            SyntaxNode w;
            bool hasPartner = _match.TryGetPartnerInTree1(x, out w);

            SyntaxNode y;
            bool hasParent = Comparer.TryGetParent(x, out y);

            if (!hasPartner)
            {
                // b) If x has no partner in M'.
                //   i. k := FindPos(x)
                //  ii. Append INS((w, a, value(x)), z, k) to E for a new identifier w.
                // iii. Add (w, x) to M' and apply INS((w, a, value(x)), z, k) to T1.          
                edits.Add(new SyntaxEdit(SyntaxEditKind.Insert, Comparer, oldNode: default(SyntaxNode), newNode: x));

                // NOTE:
                // We don't update M' here.
            }
            else if (hasParent)
            {
                // c) else if x is not a root
                // i. Let w be the partner of x in M', and let v = parent(w) in T1.
                SyntaxNode v = Comparer.GetParent(w);

                // ii. if value(w) != value(x)
                // A. Append UPD(w, value(x)) to E
                // B. Apply UPD(w, value(x) to T1   

                // Let the Comparer decide whether an update should be added to the edit list.
                // The Comparer defines what changes in node values it cares about.
                if (!Comparer.ValuesEqual(w, x))
                {
                    edits.Add(new SyntaxEdit(SyntaxEditKind.Update, Comparer, oldNode: w, newNode: x));
                }

                // If parents of w and x don't match, it's a move.
                // iii. if not (v, y) in M'             
                // NOTE: The paper says (y, v) but that seems wrong since M': T1 -> T2 and w,v in T1 and x,y in T2.
                if (!_match.Contains(v, y))
                {
                    // A. Let z be the partner of y in M'. (NOTE: z not needed)
                    // B. k := FindPos(x)
                    // C. Append MOV(w, z, k)
                    // D. Apply MOV(w, z, k) to T1
                    edits.Add(new SyntaxEdit(SyntaxEditKind.Move, Comparer, oldNode: w, newNode: x));
                }
            }

            // d) AlignChildren(w, x)

            // NOTE: If we just applied an INS((w, a, value(x)), z, k) operation on tree T1 
            // the newly created node w would have no children. So there is nothing to align.
            if (hasPartner)
            {
                AlignChildren(edits, w, x);
            }
        }

        private void AddDeletes(List<SyntaxEdit> edits)
        {
            // 3. Do a post-order traversal of T1.
            //    a) Let w be the current node in the post-order traversal of T1.
            //    b) If w has no partner in M' then append DEL(w) to E and apply DEL(w) to T1.
            //
            // NOTE: The fact that we haven't updated M' during the Insert phase 
            // doesn't affect Delete phase. The original algorithm inserted new node n1 into T1
            // when an insertion INS(n1, n2) was detected. It also added (n1, n2) to M'.
            // Then in Delete phase n1 is visited but nothing is done since it has a partner n2 in M'.
            // Since we don't add n1 into T1, not adding (n1, n2) to M' doesn't affect the Delete phase.

            foreach (var w in Comparer.GetDescendants(Root1))
            {
                if (!_match.HasPartnerInTree2(w))
                {
                    edits.Add(new SyntaxEdit(SyntaxEditKind.Delete, Comparer, oldNode: w, newNode: default(SyntaxNode)));
                }
            }
        }

        private void AlignChildren(List<SyntaxEdit> edits, SyntaxNode w, SyntaxNode x)
        {
            Debug.Assert(Comparer.TreesEqual(w, Root1));
            Debug.Assert(Comparer.TreesEqual(x, Root2));

            IEnumerable<SyntaxNode> wChildren, xChildren;
            if ((wChildren = Comparer.GetChildren(w)) == null || (xChildren = Comparer.GetChildren(x)) == null)
            {
                return;
            }

            // Step 1
            //  Make all children of w and all children x "out of order"
            //  NOTE: We don't need to mark nodes "in order".

            // Step 2
            //  Let S1 be the sequence of children of w whose partner are children
            //  of x and let S2 be the sequence of children of x whose partner are
            //  children of w.
            List<SyntaxNode> s1 = null;
            foreach (var e in wChildren)
            {
                SyntaxNode pw;
                if (_match.TryGetPartnerInTree2(e, out pw) && Comparer.GetParent(pw).Equals(x))
                {
                    if (s1 == null)
                    {
                        s1 = new List<SyntaxNode>();
                    }

                    s1.Add(e);
                }
            }

            List<SyntaxNode> s2 = null;
            foreach (var e in xChildren)
            {
                SyntaxNode px;
                if (_match.TryGetPartnerInTree1(e, out px) && Comparer.GetParent(px).Equals(w))
                {
                    if (s2 == null)
                    {
                        s2 = new List<SyntaxNode>();
                    }

                    s2.Add(e);
                }
            }

            if (s1 == null || s2 == null)
            {
                return;
            }

            // Step 3, 4
            //  Define the function Equal(a,b) to be true if and only if  (a,c) in M'
            //  Let S <- LCS(S1, S2, Equal)
            var lcs = new SyntaxMatch.LongestCommonSubsequence(_match);
            var s = lcs.GetMatchingNodes(s1, s2);

            // Step 5
            //  For each (a,b) in S, mark nodes a and b "in order"
            //  NOTE: We don't need to mark nodes "in order".

            // Step 6
            //  For each a in S1, b in S2 such that (a,b) in M but (a,b) not in S
            //   (a) k <- FindPos(b)
            //   (b) Append MOV(a,w,k) to E and apply MOV(a,w,k) to T1
            //   (c) Mark a and b "in order"
            //       NOTE: We don't mark nodes "in order".
            foreach (var a in s1)
            {
                SyntaxNode b;

                // (a,b) in M
                // => b in S2 since S2 == { b | parent(b) == x && parent(partner(b)) == w }
                // (a,b) not in S
                if (_match.TryGetPartnerInTree2(a, out b) &&
                    Comparer.GetParent(b).Equals(x) &&
                    !ContainsPair(s, a, b))
                {
                    Debug.Assert(Comparer.TreesEqual(a, Root1));
                    Debug.Assert(Comparer.TreesEqual(b, Root2));

                    edits.Add(new SyntaxEdit(SyntaxEditKind.Reorder, Comparer, oldNode: a, newNode: b));
                }
            }
        }

        private static bool ContainsPair(Dictionary<SyntaxNode, SyntaxNode> dict, SyntaxNode a, SyntaxNode b)
        {
            SyntaxNode value;
            return dict.TryGetValue(a, out value) && value.Equals(b);
        }
    }
}
