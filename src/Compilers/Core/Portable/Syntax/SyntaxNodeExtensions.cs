// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    public static partial class SyntaxNodeExtensions
    {
        /// <summary>
        /// Creates a new tree of nodes with the specified nodes, tokens and trivia replaced.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="nodes">The nodes to be replaced.</param>
        /// <param name="computeReplacementNode">A function that computes a replacement node for the
        /// argument nodes. The first argument is the original node. The second argument is the same
        /// node potentially rewritten with replaced descendants.</param>
        /// <param name="tokens">The tokens to be replaced.</param>
        /// <param name="computeReplacementToken">A function that computes a replacement token for
        /// the argument tokens. The first argument is the original token. The second argument is
        /// the same token potentially rewritten with replaced trivia.</param>
        /// <param name="trivia">The trivia to be replaced.</param>
        /// <param name="computeReplacementTrivia">A function that computes replacement trivia for
        /// the specified arguments. The first argument is the original trivia. The second argument is
        /// the same trivia with potentially rewritten sub structure.</param>
        public static TRoot ReplaceSyntax<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxNode>? nodes,
            Func<SyntaxNode, SyntaxNode, SyntaxNode>? computeReplacementNode,
            IEnumerable<SyntaxToken>? tokens,
            Func<SyntaxToken, SyntaxToken, SyntaxToken>? computeReplacementToken,
            IEnumerable<SyntaxTrivia>? trivia,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia>? computeReplacementTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore(
                nodes: nodes, computeReplacementNode: computeReplacementNode,
                tokens: tokens, computeReplacementToken: computeReplacementToken,
                trivia: trivia, computeReplacementTrivia: computeReplacementTrivia);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <typeparam name="TNode">The type of the nodes being replaced.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="nodes">The nodes to be replaced; descendants of the root node.</param>
        /// <param name="computeReplacementNode">A function that computes a replacement node for the
        /// argument nodes. The first argument is the original node. The second argument is the same
        /// node potentially rewritten with replaced descendants.</param>
        public static TRoot ReplaceNodes<TRoot, TNode>(this TRoot root, IEnumerable<TNode> nodes, Func<TNode, TNode, SyntaxNode> computeReplacementNode)
            where TRoot : SyntaxNode
            where TNode : SyntaxNode
        {
            return (TRoot)root.ReplaceCore(nodes: nodes, computeReplacementNode: computeReplacementNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="oldNode">The node to be replaced; a descendant of the root node.</param>
        /// <param name="newNode">The new node to use in the new tree in place of the old node.</param>
        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, SyntaxNode newNode)
            where TRoot : SyntaxNode
        {
            if (oldNode == newNode)
            {
                return root;
            }

            return (TRoot)root.ReplaceCore(nodes: new[] { oldNode }, computeReplacementNode: (o, r) => newNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with specified old node replaced with a new nodes.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="oldNode">The node to be replaced; a descendant of the root node and an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to use in the tree in place of the old node.</param>
        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceNodeInListCore(oldNode, newNodes);
        }

        /// <summary>
        /// Creates a new tree of nodes with new nodes inserted before the specified node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="nodeInList">The node to insert before; a descendant of the root node an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to insert into the tree immediately before the specified node.</param>
        public static TRoot InsertNodesBefore<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertNodesInListCore(nodeInList, newNodes, insertBefore: true);
        }

        /// <summary>
        /// Creates a new tree of nodes with new nodes inserted after the specified node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="nodeInList">The node to insert after; a descendant of the root node an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to insert into the tree immediately after the specified node.</param>
        public static TRoot InsertNodesAfter<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertNodesInListCore(nodeInList, newNodes, insertBefore: false);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old token replaced with new tokens.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="tokenInList">The token to be replaced; a descendant of the root node and an element of a list member.</param>
        /// <param name="newTokens">A sequence of tokens to use in the tree in place of the specified token.</param>
        public static TRoot ReplaceToken<TRoot>(this TRoot root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceTokenInListCore(tokenInList, newTokens);
        }

        /// <summary>
        /// Creates a new tree of nodes with new tokens inserted before the specified token.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="tokenInList">The token to insert before; a descendant of the root node and an element of a list member.</param>
        /// <param name="newTokens">A sequence of tokens to insert into the tree immediately before the specified token.</param>
        public static TRoot InsertTokensBefore<TRoot>(this TRoot root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertTokensInListCore(tokenInList, newTokens, insertBefore: true);
        }

        /// <summary>
        /// Creates a new tree of nodes with new tokens inserted after the specified token.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="tokenInList">The token to insert after; a descendant of the root node and an element of a list member.</param>
        /// <param name="newTokens">A sequence of tokens to insert into the tree immediately after the specified token.</param>
        public static TRoot InsertTokensAfter<TRoot>(this TRoot root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertTokensInListCore(tokenInList, newTokens, insertBefore: false);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old trivia replaced with new trivia.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="oldTrivia">The trivia to be replaced; a descendant of the root node.</param>
        /// <param name="newTrivia">A sequence of trivia to use in the tree in place of the specified trivia.</param>
        public static TRoot ReplaceTrivia<TRoot>(this TRoot root, SyntaxTrivia oldTrivia, IEnumerable<SyntaxTrivia> newTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceTriviaInListCore(oldTrivia, newTrivia);
        }

        /// <summary>
        /// Creates a new tree of nodes with new trivia inserted before the specified trivia.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="trivia">The trivia to insert before; a descendant of the root node.</param>
        /// <param name="newTrivia">A sequence of trivia to insert into the tree immediately before the specified trivia.</param>
        public static TRoot InsertTriviaBefore<TRoot>(this TRoot root, SyntaxTrivia trivia, IEnumerable<SyntaxTrivia> newTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertTriviaInListCore(trivia, newTrivia, insertBefore: true);
        }

        /// <summary>
        /// Creates a new tree of nodes with new trivia inserted after the specified trivia.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="trivia">The trivia to insert after; a descendant of the root node.</param>
        /// <param name="newTrivia">A sequence of trivia to insert into the tree immediately after the specified trivia.</param>
        public static TRoot InsertTriviaAfter<TRoot>(this TRoot root, SyntaxTrivia trivia, IEnumerable<SyntaxTrivia> newTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertTriviaInListCore(trivia, newTrivia, insertBefore: false);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="tokens">The token to be replaced; descendants of the root node.</param>
        /// <param name="computeReplacementToken">A function that computes a replacement token for
        /// the argument tokens. The first argument is the original token. The second argument is
        /// the same token potentially rewritten with replaced trivia.</param>
        public static TRoot ReplaceTokens<TRoot>(this TRoot root, IEnumerable<SyntaxToken> tokens, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(tokens: tokens, computeReplacementToken: computeReplacementToken);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old token replaced with a new token.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="oldToken">The token to be replaced.</param>
        /// <param name="newToken">The new token to use in the new tree in place of the old
        /// token.</param>
        public static TRoot ReplaceToken<TRoot>(this TRoot root, SyntaxToken oldToken, SyntaxToken newToken)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(tokens: new[] { oldToken }, computeReplacementToken: (o, r) => newToken);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified trivia replaced with new trivia.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="trivia">The trivia to be replaced; descendants of the root node.</param>
        /// <param name="computeReplacementTrivia">A function that computes replacement trivia for
        /// the specified arguments. The first argument is the original trivia. The second argument is
        /// the same trivia with potentially rewritten sub structure.</param>
        public static TRoot ReplaceTrivia<TRoot>(this TRoot root, IEnumerable<SyntaxTrivia> trivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(trivia: trivia, computeReplacementTrivia: computeReplacementTrivia);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified trivia replaced with new trivia.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="trivia">The trivia to be replaced.</param>
        /// <param name="newTrivia">The new trivia to use in the new tree in place of the old trivia.</param>
        public static TRoot ReplaceTrivia<TRoot>(this TRoot root, SyntaxTrivia trivia, SyntaxTrivia newTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(trivia: new[] { trivia }, computeReplacementTrivia: (o, r) => newTrivia);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified node removed.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node from which to remove a descendant node from.</param>
        /// <param name="node">The node to remove.</param>
        /// <param name="options">Options that determine how the node's trivia is treated.</param>
        /// <returns>New root or null if the root node itself is removed.</returns>
        public static TRoot? RemoveNode<TRoot>(this TRoot root,
            SyntaxNode node,
            SyntaxRemoveOptions options)
            where TRoot : SyntaxNode
        {
            return (TRoot?)root.RemoveNodesCore(new[] { node }, options);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified nodes removed.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node from which to remove a descendant node from.</param>
        /// <param name="nodes">The nodes to remove.</param>
        /// <param name="options">Options that determine how the nodes' trivia is treated.</param>
        public static TRoot? RemoveNodes<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxNode> nodes,
            SyntaxRemoveOptions options)
            where TRoot : SyntaxNode
        {
            return (TRoot?)root.RemoveNodesCore(nodes, options);
        }

        internal const string DefaultIndentation = "    ";
        internal const string DefaultEOL = "\r\n";

        /// <summary>
        /// Creates a new syntax node with all whitespace and end of line trivia replaced with
        /// regularly formatted trivia.
        /// </summary>
        /// <typeparam name="TNode">The type of the node.</typeparam>
        /// <param name="node">The node to format.</param>
        /// <param name="indentation">A sequence of whitespace characters that defines a single level of indentation.</param>
        /// <param name="elasticTrivia">If true the replaced trivia is elastic trivia.</param>
        public static TNode NormalizeWhitespace<TNode>(this TNode node, string indentation, bool elasticTrivia)
            where TNode : SyntaxNode
        {
            return (TNode)node.NormalizeWhitespaceCore(indentation, DefaultEOL, elasticTrivia);
        }

        /// <summary>
        /// Creates a new syntax node with all whitespace and end of line trivia replaced with
        /// regularly formatted trivia.
        /// </summary>
        /// <typeparam name="TNode">The type of the node.</typeparam>
        /// <param name="node">The node to format.</param>
        /// <param name="indentation">An optional sequence of whitespace characters that defines a single level of indentation.</param>
        /// <param name="eol">An optional sequence of whitespace characters used for end of line.</param>
        /// <param name="elasticTrivia">If true the replaced trivia is elastic trivia.</param>
        public static TNode NormalizeWhitespace<TNode>(this TNode node, string indentation = DefaultIndentation, string eol = DefaultEOL, bool elasticTrivia = false)
            where TNode : SyntaxNode
        {
            return (TNode)node.NormalizeWhitespaceCore(indentation, eol, elasticTrivia);
        }

        /// <summary>
        /// Creates a new node from this node with both the leading and trailing trivia of the specified node.
        /// </summary>
        public static TSyntax WithTriviaFrom<TSyntax>(this TSyntax syntax, SyntaxNode node)
            where TSyntax : SyntaxNode
        {
            return syntax.WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
        }

        /// <summary>
        /// Creates a new node from this node without leading or trailing trivia.
        /// </summary>
        public static TSyntax WithoutTrivia<TSyntax>(this TSyntax syntax)
            where TSyntax : SyntaxNode
        {
            return syntax.WithoutLeadingTrivia().WithoutTrailingTrivia();
        }

        /// <summary>
        /// Creates a new token from this token without leading or trailing trivia.
        /// </summary>
        public static SyntaxToken WithoutTrivia(this SyntaxToken token)
            => token.WithTrailingTrivia(default(SyntaxTriviaList))
                    .WithLeadingTrivia(default(SyntaxTriviaList));

        /// <summary>
        /// Creates a new node from this node with the leading trivia replaced.
        /// </summary>
        public static TSyntax WithLeadingTrivia<TSyntax>(
            this TSyntax node,
            SyntaxTriviaList trivia) where TSyntax : SyntaxNode
        {
            var first = node.GetFirstToken(includeZeroWidth: true);
            var newFirst = first.WithLeadingTrivia(trivia);
            return node.ReplaceToken(first, newFirst);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia replaced.
        /// </summary>
        public static TSyntax WithLeadingTrivia<TSyntax>(
            this TSyntax node,
            IEnumerable<SyntaxTrivia>? trivia) where TSyntax : SyntaxNode
        {
            var first = node.GetFirstToken(includeZeroWidth: true);
            var newFirst = first.WithLeadingTrivia(trivia);
            return node.ReplaceToken(first, newFirst);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia removed.
        /// </summary>
        public static TSyntax WithoutLeadingTrivia<TSyntax>(
            this TSyntax node
            ) where TSyntax : SyntaxNode
        {
            return node.WithLeadingTrivia((IEnumerable<SyntaxTrivia>?)null);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia replaced.
        /// </summary>
        public static TSyntax WithLeadingTrivia<TSyntax>(
            this TSyntax node,
            params SyntaxTrivia[]? trivia) where TSyntax : SyntaxNode
        {
            return node.WithLeadingTrivia((IEnumerable<SyntaxTrivia>?)trivia);
        }

        /// <summary>
        /// Creates a new node from this node with the trailing trivia replaced.
        /// </summary>
        public static TSyntax WithTrailingTrivia<TSyntax>(
            this TSyntax node,
            SyntaxTriviaList trivia) where TSyntax : SyntaxNode
        {
            var last = node.GetLastToken(includeZeroWidth: true);
            var newLast = last.WithTrailingTrivia(trivia);
            return node.ReplaceToken(last, newLast);
        }

        /// <summary>
        /// Creates a new node from this node with the trailing trivia replaced.
        /// </summary>
        public static TSyntax WithTrailingTrivia<TSyntax>(
            this TSyntax node,
            IEnumerable<SyntaxTrivia>? trivia) where TSyntax : SyntaxNode
        {
            var last = node.GetLastToken(includeZeroWidth: true);
            var newLast = last.WithTrailingTrivia(trivia);
            return node.ReplaceToken(last, newLast);
        }

        /// <summary>
        /// Creates a new node from this node with the trailing trivia removed.
        /// </summary>
        public static TSyntax WithoutTrailingTrivia<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode
        {
            return node.WithTrailingTrivia((IEnumerable<SyntaxTrivia>?)null);
        }

        /// <summary>
        /// Creates a new node from this node with the trailing trivia replaced.
        /// </summary>
        public static TSyntax WithTrailingTrivia<TSyntax>(
            this TSyntax node,
            params SyntaxTrivia[]? trivia) where TSyntax : SyntaxNode
        {
            return node.WithTrailingTrivia((IEnumerable<SyntaxTrivia>?)trivia);
        }

        /// <summary>
        /// Attaches the node to a SyntaxTree that the same options as <paramref name="oldTree"/>
        /// </summary>
        [return: NotNullIfNotNull(nameof(node))]
        internal static SyntaxNode? AsRootOfNewTreeWithOptionsFrom(this SyntaxNode? node, SyntaxTree oldTree)
        {
            return node != null ? oldTree.WithRootAndOptions(node, oldTree.Options).GetRoot() : null;
        }
    }
}
