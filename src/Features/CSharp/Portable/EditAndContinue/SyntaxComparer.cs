// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal abstract class SyntaxComparer : TreeComparer<SyntaxNode>
    {
        internal const int IgnoredNode = -1;

        protected const double ExactMatchDist = 0.0;
        protected const double EpsilonDist = 0.00001;

        protected SyntaxComparer()
        {
        }

        protected internal sealed override bool TreesEqual(SyntaxNode oldNode, SyntaxNode newNode)
            => oldNode.SyntaxTree == newNode.SyntaxTree;

        protected internal sealed override TextSpan GetSpan(SyntaxNode node)
            => node.Span;

        #region Comparison

        /// <summary>
        /// Calculates distance of two nodes based on their significant parts.
        /// Returns false if the nodes don't have any significant parts and should be compared as a whole.
        /// </summary>
        protected abstract bool TryComputeWeightedDistance(SyntaxNode oldNode, SyntaxNode newNode, out double distance);

        public sealed override double GetDistance(SyntaxNode oldNode, SyntaxNode newNode)
        {
            Debug.Assert(GetLabel(oldNode) == GetLabel(newNode) && GetLabel(oldNode) != IgnoredNode);

            if (oldNode == newNode)
            {
                return ExactMatchDist;
            }

            if (TryComputeWeightedDistance(oldNode, newNode, out var weightedDistance))
            {
                if (weightedDistance == ExactMatchDist && !SyntaxFactory.AreEquivalent(oldNode, newNode))
                {
                    weightedDistance = EpsilonDist;
                }

                return weightedDistance;
            }

            return ComputeValueDistance(oldNode, newNode);
        }

        internal static double ComputeValueDistance(SyntaxNode? oldNode, SyntaxNode? newNode)
        {
            if (SyntaxFactory.AreEquivalent(oldNode, newNode))
            {
                return ExactMatchDist;
            }

            var distance = ComputeDistance(oldNode, newNode);

            // We don't want to return an exact match, because there
            // must be something different, since we got here 
            return (distance == ExactMatchDist) ? EpsilonDist : distance;
        }

        internal static double ComputeDistance(SyntaxNodeOrToken oldNodeOrToken, SyntaxNodeOrToken newNodeOrToken)
        {
            Debug.Assert(newNodeOrToken.IsToken == oldNodeOrToken.IsToken);

            double distance;
            if (oldNodeOrToken.IsToken)
            {
                var leftToken = oldNodeOrToken.AsToken();
                var rightToken = newNodeOrToken.AsToken();

                distance = ComputeDistance(leftToken, rightToken);
                Debug.Assert(!SyntaxFactory.AreEquivalent(leftToken, rightToken) || distance == ExactMatchDist);
            }
            else
            {
                var leftNode = oldNodeOrToken.AsNode();
                var rightNode = newNodeOrToken.AsNode();

                distance = ComputeDistance(leftNode, rightNode);
                Debug.Assert(!SyntaxFactory.AreEquivalent(leftNode, rightNode) || distance == ExactMatchDist);
            }

            return distance;
        }

        /// <summary>
        /// Enumerates tokens of all nodes in the list. Doesn't include separators.
        /// </summary>
        internal static IEnumerable<SyntaxToken> GetDescendantTokensIgnoringSeparators<TSyntaxNode>(SeparatedSyntaxList<TSyntaxNode> list)
            where TSyntaxNode : SyntaxNode
        {
            foreach (var node in list)
            {
                foreach (var token in node.DescendantTokens())
                {
                    yield return token;
                }
            }
        }

        /// <summary>
        /// Calculates the distance between two syntax nodes, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the nodes are. 
        /// </remarks>
        public static double ComputeDistance(SyntaxNode? oldNode, SyntaxNode? newNode)
        {
            if (oldNode == null || newNode == null)
            {
                return (oldNode == newNode) ? 0.0 : 1.0;
            }

            return ComputeDistance(oldNode.DescendantTokens(), newNode.DescendantTokens());
        }

        /// <summary>
        /// Calculates the distance between two syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the tokens are. 
        /// </remarks>
        public static double ComputeDistance(SyntaxToken oldToken, SyntaxToken newToken)
            => LongestCommonSubstring.ComputeDistance(oldToken.Text, newToken.Text);

        /// <summary>
        /// Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(IEnumerable<SyntaxToken>? oldTokens, IEnumerable<SyntaxToken>? newTokens)
            => LcsTokens.Instance.ComputeDistance(oldTokens.AsImmutableOrEmpty(), newTokens.AsImmutableOrEmpty());

        /// <summary>
        /// Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(ImmutableArray<SyntaxToken> oldTokens, ImmutableArray<SyntaxToken> newTokens)
            => LcsTokens.Instance.ComputeDistance(oldTokens.NullToEmpty(), newTokens.NullToEmpty());

        /// <summary>
        /// Calculates the distance between two sequences of syntax nodes, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(IEnumerable<SyntaxNode>? oldNodes, IEnumerable<SyntaxNode>? newNodes)
            => LcsNodes.Instance.ComputeDistance(oldNodes.AsImmutableOrEmpty(), newNodes.AsImmutableOrEmpty());

        /// <summary>
        /// Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes)
            => LcsNodes.Instance.ComputeDistance(oldNodes.NullToEmpty(), newNodes.NullToEmpty());

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(IEnumerable<SyntaxNode>? oldNodes, IEnumerable<SyntaxNode>? newNodes)
            => LcsNodes.Instance.GetEdits(oldNodes.AsImmutableOrEmpty(), newNodes.AsImmutableOrEmpty());

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes)
            => LcsNodes.Instance.GetEdits(oldNodes.NullToEmpty(), newNodes.NullToEmpty());

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(IEnumerable<SyntaxToken>? oldTokens, IEnumerable<SyntaxToken>? newTokens)
            => LcsTokens.Instance.GetEdits(oldTokens.AsImmutableOrEmpty(), newTokens.AsImmutableOrEmpty());

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(ImmutableArray<SyntaxToken> oldTokens, ImmutableArray<SyntaxToken> newTokens)
            => LcsTokens.Instance.GetEdits(oldTokens.NullToEmpty(), newTokens.NullToEmpty());

        private sealed class LcsTokens : LongestCommonImmutableArraySubsequence<SyntaxToken>
        {
            internal static readonly LcsTokens Instance = new LcsTokens();

            protected override bool Equals(SyntaxToken oldElement, SyntaxToken newElement)
                => SyntaxFactory.AreEquivalent(oldElement, newElement);
        }

        private sealed class LcsNodes : LongestCommonImmutableArraySubsequence<SyntaxNode>
        {
            internal static readonly LcsNodes Instance = new LcsNodes();

            protected override bool Equals(SyntaxNode oldElement, SyntaxNode newElement)
                => SyntaxFactory.AreEquivalent(oldElement, newElement);
        }

        #endregion
    }
}
