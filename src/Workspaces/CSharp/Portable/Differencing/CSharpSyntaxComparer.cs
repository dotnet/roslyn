// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Differencing;

namespace Microsoft.CodeAnalysis.CSharp.Differencing
{
    internal abstract class CSharpSyntaxComparer : SyntaxComparer
    {
        protected CSharpSyntaxComparer() : base(CSharpSyntaxEquivalent.Instance)
        {
        }

        internal static double ComputeValueDistance(SyntaxNode oldNode, SyntaxNode newNode)
        {
            return ComputeValueDistance(oldNode, newNode, CSharpSyntaxEquivalent.Instance);
        }

        internal static double ComputeDistance(SyntaxNodeOrToken oldNodeOrToken, SyntaxNodeOrToken newNodeOrToken)
        {
            return ComputeDistance(oldNodeOrToken, newNodeOrToken, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the distance between two syntax nodes, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the nodes are. 
        /// </remarks>
        public static double ComputeDistance(SyntaxNode oldNode, SyntaxNode newNode)
        {
            return ComputeDistance(oldNode, newNode, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(IEnumerable<SyntaxToken> oldTokens, IEnumerable<SyntaxToken> newTokens)
        {
            return ComputeDistance(oldTokens, newTokens, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(ImmutableArray<SyntaxToken> oldTokens, ImmutableArray<SyntaxToken> newTokens)
        {
            return ComputeDistance(oldTokens, newTokens, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the distance between two sequences of syntax nodes, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(IEnumerable<SyntaxNode> oldNodes, IEnumerable<SyntaxNode> newNodes)
        {
            return ComputeDistance(oldNodes, newNodes, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        /// </summary>
        /// <remarks>
        /// Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        /// </remarks>
        public static double ComputeDistance(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes)
        {
            return ComputeDistance(oldNodes, newNodes, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(IEnumerable<SyntaxNode> oldNodes, IEnumerable<SyntaxNode> newNodes)
        {
            return GetSequenceEdits(oldNodes, newNodes, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes)
        {
            return GetSequenceEdits(oldNodes, newNodes, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(IEnumerable<SyntaxToken> oldTokens, IEnumerable<SyntaxToken> newTokens)
        {
            return GetSequenceEdits(oldTokens, newTokens, CSharpSyntaxEquivalent.Instance);
        }

        /// <summary>
        /// Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        /// </summary>
        public static IEnumerable<SequenceEdit> GetSequenceEdits(ImmutableArray<SyntaxToken> oldTokens, ImmutableArray<SyntaxToken> newTokens)
        {
            return GetSequenceEdits(oldTokens, newTokens, CSharpSyntaxEquivalent.Instance);
        }

        internal class CSharpSyntaxEquivalent : ISyntaxEquivalentChecker
        {
            public static readonly CSharpSyntaxEquivalent Instance = new CSharpSyntaxEquivalent();

            public LcsNodes LcsNodes { get; } = new LcsNodes((l, r) => SyntaxFactory.AreEquivalent(l, r));
            public LcsTokens LcsTokens { get; } = new LcsTokens((l, r) => SyntaxFactory.AreEquivalent(l, r));

            public bool AreEquivalent(SyntaxToken left, SyntaxToken right)
            {
                return SyntaxFactory.AreEquivalent(left, right);
            }

            public bool AreEquivalent(SyntaxNode left, SyntaxNode right)
            {
                return SyntaxFactory.AreEquivalent(left, right);
            }
        }
    }
}
