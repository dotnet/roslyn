// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Differencing
{
    public sealed partial class Match<TNode>
    {
        private const double ExactMatchDistance = 0.0;
        private const double EpsilonDistance = 0.00001;
        private const double MatchingDistance1 = 0.5;
        private const double MatchingDistance2 = 1.0;
        private const double MatchingDistance3 = 1.5;
        private const double MaxDistance = 2.0;

        private readonly TreeComparer<TNode> _comparer;
        private readonly TNode _root1;
        private readonly TNode _root2;

        private readonly Dictionary<TNode, TNode> _oneToTwo;
        private readonly Dictionary<TNode, TNode> _twoToOne;

        internal Match(TNode root1, TNode root2, TreeComparer<TNode> comparer, IEnumerable<KeyValuePair<TNode, TNode>> knownMatches)
        {
            _root1 = root1;
            _root2 = root2;
            _comparer = comparer;

            int labelCount = comparer.LabelCount;

            // Calculate chains (not including root node):
            int count1, count2;
            List<TNode>[] nodes1, nodes2;
            CategorizeNodesByLabels(comparer, root1, labelCount, out nodes1, out count1);
            CategorizeNodesByLabels(comparer, root2, labelCount, out nodes2, out count2);

            _oneToTwo = new Dictionary<TNode, TNode>();
            _twoToOne = new Dictionary<TNode, TNode>();

            // Root nodes always match. Add them before adding known matches to make sure we always have root mapping.
            TryAdd(root1, root2);

            if (knownMatches != null)
            {
                foreach (var knownMatch in knownMatches)
                {
                    if (comparer.GetLabel(knownMatch.Key) != comparer.GetLabel(knownMatch.Value))
                    {
                        throw new ArgumentException(string.Format(WorkspacesResources.MatchingNodesMustHaveTheSameLabel, knownMatch.Key, knownMatch.Value), nameof(knownMatches));
                    }

                    if (!comparer.TreesEqual(knownMatch.Key, root1))
                    {
                        throw new ArgumentException(string.Format(WorkspacesResources.NodeMustBeContainedInTheOldTree, knownMatch.Key), nameof(knownMatches));
                    }

                    if (!comparer.TreesEqual(knownMatch.Value, root2))
                    {
                        throw new ArgumentException(string.Format(WorkspacesResources.NodeMustBeContainedInTheNewTree, knownMatch.Value), nameof(knownMatches));
                    }

                    // skip pairs whose key or value is already mapped:
                    TryAdd(knownMatch.Key, knownMatch.Value);
                }
            }

            ComputeMatch(nodes1, nodes2);
        }

        private static void CategorizeNodesByLabels(
            TreeComparer<TNode> comparer,
            TNode root,
            int labelCount,
            out List<TNode>[] nodes,
            out int totalCount)
        {
            nodes = new List<TNode>[labelCount];
            int count = 0;

            // It is important that we add the nodes in depth-first prefix order.
            // This order ensures that a node of a certain kind can have a parent of the same kind 
            // and we can still use tied-to-parent for that kind. That's because the parent will always
            // be processed earlier than the child due to depth-first prefix ordering.
            foreach (TNode node in comparer.GetDescendants(root))
            {
                int label = comparer.GetLabel(node);
                if (label < 0 || label >= labelCount)
                {
                    throw new InvalidOperationException(string.Format(WorkspacesResources.LabelForNodeIsInvalid, node, labelCount));
                }

                var list = nodes[label];
                if (list == null)
                {
                    nodes[label] = list = new List<TNode>();
                }

                list.Add(node);

                count++;
            }

            totalCount = count;
        }

        private void ComputeMatch(List<TNode>[] nodes1, List<TNode>[] nodes2)
        {
            Debug.Assert(nodes1.Length == nodes2.Length);

            // --- The original FastMatch algorithm ---
            // 
            // For each leaf label l, and then for each internal node label l do:
            // a) S1 := chain T1(l)
            // b) S2 := chain T2(l)
            // c) lcs := LCS(S1, S2, Equal)
            // d) For each pair of nodes (x,y) in lcs add (x,y) to M.
            // e) Pair unmatched nodes with label l as in Algorithm Match, adding matches to M:
            //    For each unmatched node x in T1, if there is an unmatched node y in T2 such that equal(x,y) 
            //    then add (x,y) to M.
            //
            // equal(x,y) is defined as follows:
            //   x, y are leafs => equal(x,y) := label(x) == label(y) && compare(value(x), value(y)) <= f
            //   x, y are nodes => equal(x,y) := label(x) == label(y) && |common(x,y)| / max(|x|, |y|) > t 
            // where f, t are constants.
            //
            // --- Actual implementation ---
            //
            // We also categorize nodes by their labels, but then we proceed differently:
            // 
            // 1) A label may be marked "tied to parent". Let x, y have both label l and l is "tied to parent".
            //    Then (x,y) can be in M only if (parent(x), parent(y)) in M.
            //    Thus we require labels of children tied to a parent to be preceded by all their possible parent labels.
            //
            // 2) Rather than defining function equal in terms of constants f and t, which are hard to get right,
            //    we try to match multiple times with different threshold for node distance.
            //    The comparer defines the distance [0..1] between two nodes and it can do so by analyzing 
            //    the node structure and value. The comparer can tune the distance specifically for each node kind.
            //    We first try to match nodes of the same labels to the exactly matching or almost matching counterparts.
            //    The we keep increasing the threshold and keep adding matches. 

            for (int l = 0; l < nodes1.Length; l++)
            {
                if (nodes1[l] != null && nodes2[l] != null)
                {
                    ComputeMatchForLabel(l, nodes1[l], nodes2[l]);
                }
            }
        }

        private void ComputeMatchForLabel(int label, List<TNode> s1, List<TNode> s2)
        {
            int tiedToAncestor = _comparer.TiedToAncestor(label);

            ComputeMatchForLabel(s1, s2, tiedToAncestor, EpsilonDistance);     // almost exact match
            ComputeMatchForLabel(s1, s2, tiedToAncestor, MatchingDistance1);   // ok match
            ComputeMatchForLabel(s1, s2, tiedToAncestor, MatchingDistance2);   // ok match
            ComputeMatchForLabel(s1, s2, tiedToAncestor, MatchingDistance3);   // ok match 
            ComputeMatchForLabel(s1, s2, tiedToAncestor, MaxDistance);         // any match
        }

        private void ComputeMatchForLabel(List<TNode> s1, List<TNode> s2, int tiedToAncestor, double maxAcceptableDistance)
        {
            // Obviously, the algorithm below is O(n^2). However, in the common case, the 2 lists will
            // be sequences that exactly match. The purpose of "firstNonMatch2" is to reduce the complexity
            // to O(n) in this case. Basically, the pointer is the 1st non-matched node in the list of nodes of tree2
            // with the given label. 
            // Whenever we match to firstNonMatch2 we set firstNonMatch2 to the subsequent node.
            // So in the case of totally matching sequences, we process them in O(n) - 
            // both node1 and firstNonMatch2 will be advanced simultaneously.

            int count1 = s1.Count;
            int count2 = s2.Count;
            int firstNonMatch2 = 0;

            for (int i1 = 0; i1 < count1; i1++)
            {
                TNode node1 = s1[i1];

                // Skip this guy if it already has a partner
                if (HasPartnerInTree2(node1))
                {
                    continue;
                }

                // Find node2 that matches node1 the best, i.e. has minimal distance.

                double bestDistance = MaxDistance;
                TNode bestMatch = default(TNode);
                bool matched = false;
                int i2;
                for (i2 = firstNonMatch2; i2 < count2; i2++)
                {
                    TNode node2 = s2[i2];

                    // Skip this guy if it already has a partner
                    if (HasPartnerInTree1(node2))
                    {
                        continue;
                    }

                    // this requires parents to be processed before their children:
                    if (tiedToAncestor > 0)
                    {
                        // TODO (tomat): For nodes tied to their parents, 
                        // consider avoiding matching them to all other nodes of the same label.
                        // Rather we should only match them with their siblings that share the same parent.

                        var ancestor1 = _comparer.GetAncestor(node1, tiedToAncestor);
                        var ancestor2 = _comparer.GetAncestor(node2, tiedToAncestor);

                        // Since CategorizeNodesByLabels added nodes to the s1/s2 lists in depth-first prefix order,
                        // we can also accept equality in the following condition. That's because we find the partner 
                        // of the parent node before we get to finding it for the child node of the same kind.
                        Debug.Assert(_comparer.GetLabel(ancestor1) <= _comparer.GetLabel(node1));

                        if (!Contains(ancestor1, ancestor2))
                        {
                            continue;
                        }
                    }

                    // We know that
                    // 1. (node1, node2) not in M
                    // 2. Both of their parents are matched to the same parent (or are not matched)
                    //
                    // Now, we have no other choice than comparing the node "values"
                    // and looking for the one with the smaller distance.

                    double distance = _comparer.GetDistance(node1, node2);
                    if (distance < bestDistance)
                    {
                        matched = true;
                        bestMatch = node2;
                        bestDistance = distance;

                        // We only stop if we've got an exact match. This is to resolve the problem
                        // of entities with identical names(name is often used as the "value" of a
                        // node) but with different "sub-values" (e.g. two locals may have the same name
                        // but different types. Since the type is not part of the value, we don't want
                        // to stop looking for the best match if we don't have an exact match).
                        if (distance == ExactMatchDistance)
                        {
                            break;
                        }
                    }
                }

                if (matched && bestDistance <= maxAcceptableDistance)
                {
                    bool added = TryAdd(node1, bestMatch);

                    // We checked above that node1 doesn't have a partner. 
                    // The map is a bijection by construction, so we should be able to add the mapping.
                    Debug.Assert(added);

                    // If we exactly matched to firstNonMatch2 we can advance it.
                    if (i2 == firstNonMatch2)
                    {
                        firstNonMatch2 = i2 + 1;
                    }
                }
            }
        }

        internal bool TryAdd(TNode node1, TNode node2)
        {
            Debug.Assert(_comparer.TreesEqual(node1, _root1));
            Debug.Assert(_comparer.TreesEqual(node2, _root2));

            if (_oneToTwo.ContainsKey(node1) || _twoToOne.ContainsKey(node2))
            {
                return false;
            }

            _oneToTwo.Add(node1, node2);
            _twoToOne.Add(node2, node1);
            return true;
        }

        internal bool TryGetPartnerInTree1(TNode node2, out TNode partner1)
        {
            bool result = _twoToOne.TryGetValue(node2, out partner1);
            Debug.Assert(_comparer.TreesEqual(node2, _root2));
            Debug.Assert(!result || _comparer.TreesEqual(partner1, _root1));
            return result;
        }

        internal bool HasPartnerInTree1(TNode node2)
        {
            Debug.Assert(_comparer.TreesEqual(node2, _root2));
            return _twoToOne.ContainsKey(node2);
        }

        internal bool TryGetPartnerInTree2(TNode node1, out TNode partner2)
        {
            bool result = _oneToTwo.TryGetValue(node1, out partner2);
            Debug.Assert(_comparer.TreesEqual(node1, _root1));
            Debug.Assert(!result || _comparer.TreesEqual(partner2, _root2));
            return result;
        }

        internal bool HasPartnerInTree2(TNode node1)
        {
            Debug.Assert(_comparer.TreesEqual(node1, _root1));
            return _oneToTwo.ContainsKey(node1);
        }

        internal bool Contains(TNode node1, TNode node2)
        {
            Debug.Assert(_comparer.TreesEqual(node2, _root2));

            TNode partner2;
            return TryGetPartnerInTree2(node1, out partner2) && node2.Equals(partner2);
        }

        public TreeComparer<TNode> Comparer
        {
            get
            {
                return _comparer;
            }
        }

        public TNode OldRoot
        {
            get
            {
                return _root1;
            }
        }

        public TNode NewRoot
        {
            get
            {
                return _root2;
            }
        }

        public IReadOnlyDictionary<TNode, TNode> Matches
        {
            get
            {
                return new ReadOnlyDictionary<TNode, TNode>(_oneToTwo);
            }
        }

        public IReadOnlyDictionary<TNode, TNode> ReverseMatches
        {
            get
            {
                return new ReadOnlyDictionary<TNode, TNode>(_twoToOne);
            }
        }

        public bool TryGetNewNode(TNode oldNode, out TNode newNode)
        {
            return _oneToTwo.TryGetValue(oldNode, out newNode);
        }

        public bool TryGetOldNode(TNode newNode, out TNode oldNode)
        {
            return _twoToOne.TryGetValue(newNode, out oldNode);
        }

        /// <summary>
        /// Returns an edit script (a sequence of edits) that transform <see cref="OldRoot"/> subtree 
        /// to <see cref="NewRoot"/> subtree.
        /// </summary>
        public EditScript<TNode> GetTreeEdits()
        {
            return new EditScript<TNode>(this);
        }

        /// <summary>
        /// Returns an edit script (a sequence of edits) that transform a sequence of nodes <paramref name="oldNodes"/>
        /// to a sequence of nodes <paramref name="newNodes"/>. 
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="oldNodes"/> or <paramref name="newNodes"/> is a null reference.</exception>
        public IEnumerable<Edit<TNode>> GetSequenceEdits(IEnumerable<TNode> oldNodes, IEnumerable<TNode> newNodes)
        {
            if (oldNodes == null)
            {
                throw new ArgumentNullException(nameof(oldNodes));
            }

            if (newNodes == null)
            {
                throw new ArgumentNullException(nameof(newNodes));
            }

            var oldList = (oldNodes as IReadOnlyList<TNode>) ?? oldNodes.ToList();
            var newList = (newNodes as IReadOnlyList<TNode>) ?? newNodes.ToList();

            return new LongestCommonSubsequence(this).GetEdits(oldList, newList);
        }
    }
}
