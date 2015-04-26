// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Differencing
{
    internal abstract class TreeNodeComparer : TreeComparer<TreeNode>
    {
        internal const int IgnoredNode = -1;

        protected const double ExactMatchDist = 0.0;
        protected const double EpsilonDist = 0.00001;

        private readonly ITreeNodeComparer _comparer;

        public TreeNodeComparer(ITreeNodeComparer comparer)
        {
            _comparer = comparer;
        }

        public override double GetDistance(TreeNode oldNode, TreeNode newNode)
        {
            Debug.Assert(GetLabel(oldNode) == GetLabel(newNode) && GetLabel(oldNode) != IgnoredNode);

            if (oldNode == newNode)
            {
                return ExactMatchDist;
            }

            return ComputeValueDistance(oldNode, newNode);
        }

        private double ComputeValueDistance(TreeNode oldNode, TreeNode newNode)
        {
            if (_comparer.AreEquivalent(oldNode, newNode))
            {
                return ExactMatchDist;
            }

            var distance = ComputeDistance(oldNode.GetDescendants(), newNode.GetDescendants());

            // We don't want to return an exact match, because there
            // must be something different, since we got here 
            return (distance == ExactMatchDist) ? EpsilonDist : distance;
        }

        private double ComputeDistance(IEnumerable<TreeNode> oldNodes, IEnumerable<TreeNode> newNodes)
        {
            return _comparer.LcsTreeNodes.ComputeDistance(oldNodes.ToImmutableArrayOrEmpty(), newNodes.ToImmutableArrayOrEmpty());
        }

        public override bool ValuesEqual(TreeNode oldNode, TreeNode newNode)
        {
            return _comparer.AreEquivalent(oldNode, newNode);
        }

        protected internal override IEnumerable<TreeNode> GetChildren(TreeNode node)
        {
            return node.GetChildren();
        }

        protected internal override IEnumerable<TreeNode> GetDescendants(TreeNode node)
        {
            return node.GetDescendants();
        }

        protected internal override int TiedToAncestor(int label)
        {
            return 0;
        }

        protected internal override bool TryGetParent(TreeNode node, out TreeNode parent)
        {
            parent = node.Parent;
            return parent.Valid;
        }

        protected internal override TextSpan GetSpan(TreeNode node)
        {
            return node.Span;
        }

        protected internal override bool TreesEqual(TreeNode oldNode, TreeNode newNode)
        {
            var oldTree = oldNode.SyntaxTree;
            var newTree = newNode.SyntaxTree;

            return oldTree != null && newTree != null && oldTree == newTree;
        }

        internal sealed class LcsTreeNodes : LongestCommonImmutableArraySubsequence<TreeNode>
        {
            private readonly Func<TreeNode, TreeNode, bool> _equivalentChecker;

            public LcsTreeNodes(Func<TreeNode, TreeNode, bool> equivalentChecker)
            {
                _equivalentChecker = equivalentChecker;
            }

            protected override bool Equals(TreeNode oldElement, TreeNode newElement)
            {
                return _equivalentChecker(oldElement, newElement);
            }
        }

        internal interface ITreeNodeComparer
        {
            bool AreEquivalent(TreeNode left, TreeNode right);

            LcsTreeNodes LcsTreeNodes { get; }
        }
    }
}
