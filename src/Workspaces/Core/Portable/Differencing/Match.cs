// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Differencing
{
    public sealed partial class Match<TNode>
    {
        private const double ExactMatchDistance = 0.0;
        private const double EpsilonDistance = 0.00001;
        private const double MatchingDistance1 = 0.25;
        private const double MatchingDistance2 = 0.5;
        private const double MatchingDistance3 = 0.75;
        private const double MaxDistance = 1.0;

        private readonly TreeComparer<TNode> _comparer;
        private readonly TNode _root1;
        private readonly TNode _root2;

        private readonly Dictionary<TNode, TNode> _oneToTwo = new();
        private readonly Dictionary<TNode, TNode> _twoToOne = new();

        internal Match(TNode root1, TNode root2, TreeComparer<TNode> comparer, IEnumerable<KeyValuePair<TNode, TNode>> knownMatches)
        {
            _root1 = root1;
            _root2 = root2;
            _comparer = comparer;

            var labelCount = comparer.LabelCount;
            CategorizeNodesByLabels(comparer, root1, labelCount, out var nodes1, out _);
            CategorizeNodesByLabels(comparer, root2, labelCount, out var nodes2, out _);

            // Root nodes always match. Add them before adding known matches to make sure we always have root mapping.
            TryAdd(root1, root2);

            if (knownMatches != null)
            {
                foreach (var knownMatch in knownMatches)
                {
                    if (comparer.GetLabel(knownMatch.Key) != comparer.GetLabel(knownMatch.Value))
                    {
                        throw new ArgumentException(string.Format(WorkspacesResources.Matching_nodes_0_and_1_must_have_the_same_label, knownMatch.Key, knownMatch.Value), nameof(knownMatches));
                    }

                    if (!comparer.TreesEqual(knownMatch.Key, root1))
                    {
                        throw new ArgumentException(string.Format(WorkspacesResources.Node_0_must_be_contained_in_the_old_tree, knownMatch.Key), nameof(knownMatches));
                    }

                    if (!comparer.TreesEqual(knownMatch.Value, root2))
                    {
                        throw new ArgumentException(string.Format(WorkspacesResources.Node_0_must_be_contained_in_the_new_tree, knownMatch.Value), nameof(knownMatches));
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
            var count = 0;

            // It is important that we add the nodes in depth-first prefix order.
            // This order ensures that a node of a certain kind can have a parent of the same kind 
            // and we can still use tied-to-parent for that kind. That's because the parent will always
            // be processed earlier than the child due to depth-first prefix ordering.
            foreach (var node in comparer.GetDescendants(root))
            {
                var label = comparer.GetLabel(node);
                if (label < 0 || label >= labelCount)
                {
                    throw new InvalidOperationException(string.Format(WorkspacesResources.Label_for_node_0_is_invalid_it_must_be_within_bracket_0_1, node, labelCount));
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

            for (var l = 0; l < nodes1.Length; l++)
            {
                if (nodes1[l] != null && nodes2[l] != null)
                {
                    ComputeMatchForLabel(l, nodes1[l], nodes2[l]);
                }
            }
        }

        private void ComputeMatchForLabel(int label, List<TNode> s1, List<TNode> s2)
        {
            var tiedToAncestor = _comparer.TiedToAncestor(label);

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

            Debug.Assert(maxAcceptableDistance is >= ExactMatchDistance and <= MaxDistance);
            var count1 = s1.Count;
            var count2 = s2.Count;
            var firstNonMatch2 = 0;

            for (var i1 = 0; i1 < count1; i1++)
            {
                var node1 = s1[i1];

                // Skip this guy if it already has a partner
                if (HasPartnerInTree2(node1))
                {
                    continue;
                }

                // Find node2 that matches node1 the best, i.e. has minimal distance.

                var bestDistance = MaxDistance * 2;
                TNode bestMatch = default;
                var matched = false;
                int i2;
                for (i2 = firstNonMatch2; i2 < count2; i2++)
                {
                    var node2 = s2[i2];

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

                        // Check if nodes that are configured to be tied to their ancestor have the respective ancestor matching.
                        // In cases when we compare substrees rooted below both of these ancestors we assume the ancestors are
                        // matching since the roots of the subtrees must match and therefore their ancestors must match as well.
                        // If one node's ancestor is present in the subtree and the other isn't then we are not in the scenario
                        // of comparing subtrees with matching roots and thus we consider the nodes not matching.

                        var hasAncestor1 = _comparer.TryGetAncestor(node1, tiedToAncestor, out var ancestor1);
                        var hasAncestor2 = _comparer.TryGetAncestor(node2, tiedToAncestor, out var ancestor2);
                        if (hasAncestor1 != hasAncestor2)
                        {
                            continue;
                        }

                        if (hasAncestor1)
                        {
                            // Since CategorizeNodesByLabels added nodes to the s1/s2 lists in depth-first prefix order,
                            // we can also accept equality in the following condition. That's because we find the partner 
                            // of the parent node before we get to finding it for the child node of the same kind.
                            Debug.Assert(_comparer.GetLabel(ancestor1) <= _comparer.GetLabel(node1));

                            if (!Contains(ancestor1, ancestor2))
                            {
                                continue;
                            }
                        }
                    }

                    // We know that
                    // 1. (node1, node2) not in M
                    // 2. Both of their parents are matched to the same parent (or are not matched)
                    //
                    // Now, we have no other choice than comparing the node "values"
                    // and looking for the one with the smaller distance.

                    var distance = _comparer.GetDistance(node1, node2);
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
                    var added = TryAdd(node1, bestMatch);

                    // We checked above that node1 doesn't have a partner. 
                    // The map is a bijection by construction, so we should be able to add the mapping.
                    Debug.Assert(added);

                    // If we exactly matched to firstNonMatch2 we can advance it.
                    if (i2 == firstNonMatch2)
                    {
                        firstNonMatch2 = i2 + 1;
                    }

                    if (firstNonMatch2 == count2)
                    {
                        return;
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
            var result = _twoToOne.TryGetValue(node2, out partner1);
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
            var result = _oneToTwo.TryGetValue(node1, out partner2);
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
            return TryGetPartnerInTree2(node1, out var partner2) && node2.Equals(partner2);
        }

        public TreeComparer<TNode> Comparer => _comparer;

        public TNode OldRoot => _root1;

        public TNode NewRoot => _root2;

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
            => _oneToTwo.TryGetValue(oldNode, out newNode);

        public bool TryGetOldNode(TNode newNode, out TNode oldNode)
            => _twoToOne.TryGetValue(newNode, out oldNode);

        /// <summary>
        /// Returns an edit script (a sequence of edits) that transform <see cref="OldRoot"/> subtree 
        /// to <see cref="NewRoot"/> subtree.
        /// </summary>
        public EditScript<TNode> GetTreeEdits()
            => new(this);

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
