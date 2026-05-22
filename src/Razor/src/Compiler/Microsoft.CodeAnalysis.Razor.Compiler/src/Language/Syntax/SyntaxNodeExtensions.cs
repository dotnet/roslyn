// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxNodeExtensions
{
    public static TNode WithDiagnostics<TNode>(this TNode node, params RazorDiagnostic[] diagnostics) where TNode : SyntaxNode
    {
        return (TNode)node.Green.SetDiagnostics(diagnostics).CreateRed(node.Parent, node.Position);
    }

    public static TNode AppendDiagnostic<TNode>(this TNode node, params ReadOnlySpan<RazorDiagnostic> diagnostics) where TNode : SyntaxNode
    {
        RazorDiagnostic[] allDiagnostics = [
            .. node.GetDiagnostics(),
            .. diagnostics];

        return node.WithDiagnostics(allDiagnostics);
    }

    /// <summary>
    /// Gets top-level and nested diagnostics from the <paramref name="node"/>.
    /// </summary>
    /// <typeparam name="TNode">The type of syntax node.</typeparam>
    /// <param name="node">The syntax node.</param>
    /// <param name="list"></param>
    /// <returns>The list of <see cref="RazorDiagnostic">RazorDiagnostics</see>.</returns>
    public static void CollectAllDiagnostics<TNode>(this TNode node, List<RazorDiagnostic> list)
        where TNode : SyntaxNode
    {
        var walker = new DiagnosticSyntaxWalker(list);
        walker.Visit(node);
    }

    public static SourceLocation GetSourceLocation(this SyntaxNodeOrToken nodeOrToken, RazorSourceDocument source)
        => nodeOrToken.IsToken
            ? nodeOrToken.AsToken().GetSourceLocation(source)
            : nodeOrToken.AsNode()?.GetSourceLocation(source) ?? default;

    public static SourceLocation GetSourceLocation(this SyntaxNode node, RazorSourceDocument source)
    {
        try
        {
            if (source.Text.Length == 0)
            {
                // Just a marker symbol
                return new SourceLocation(source.FilePath, 0, 0, 0);
            }
            if (node.Position == source.Text.Length)
            {
                // E.g. Marker symbol at the end of the document
                var lastPosition = source.Text.Length - 1;
                var endsWithLineBreak = SyntaxFacts.IsNewLine(source.Text[lastPosition]);
                var lastLocation = source.Text.Lines.GetLinePosition(lastPosition);
                return new SourceLocation(
                    source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                    lastPosition + 1,
                    lastLocation.Line + (endsWithLineBreak ? 1 : 0),
                    endsWithLineBreak ? 0 : lastLocation.Character + 1);
            }

            var location = source.Text.Lines.GetLinePosition(node.Position);
            return new SourceLocation(
                source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                node.Position,
                location);
        }
        catch (IndexOutOfRangeException)
        {
            Debug.Assert(false, "Node position should stay within document length.");
            return new SourceLocation(source.FilePath, node.Position, 0, 0);
        }
    }

    public static SourceLocation GetSourceLocation(this SyntaxToken token, RazorSourceDocument source)
    {
        try
        {
            if (source.Text.Length == 0)
            {
                // Just a marker symbol
                return new SourceLocation(source.FilePath, 0, 0, 0);
            }
            if (token.Position == source.Text.Length)
            {
                // E.g. Marker symbol at the end of the document
                var lastPosition = source.Text.Length - 1;
                var endsWithLineBreak = SyntaxFacts.IsNewLine(source.Text[lastPosition]);
                var lastLocation = source.Text.Lines.GetLinePosition(lastPosition);
                return new SourceLocation(
                    source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                    lastPosition + 1,
                    lastLocation.Line + (endsWithLineBreak ? 1 : 0),
                    endsWithLineBreak ? 0 : lastLocation.Character + 1);
            }

            var location = source.Text.Lines.GetLinePosition(token.Position);
            return new SourceLocation(
                source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                token.Position,
                location);
        }
        catch (IndexOutOfRangeException)
        {
            Debug.Assert(false, "Node position should stay within document length.");
            return new SourceLocation(source.FilePath, token.Position, 0, 0);
        }
    }

    public static SourceSpan GetSourceSpan(this SyntaxNode node, RazorSourceDocument source)
    {
        var location = node.GetSourceLocation(source);
        var endLocation = source.Text.Lines.GetLinePosition(node.EndPosition);
        var lineCount = endLocation.Line - location.LineIndex;
        return new SourceSpan(location.FilePath, location.AbsoluteIndex, location.LineIndex, location.CharacterIndex, node.Width, lineCount, endLocation.Character);
    }

    public static SourceSpan GetSourceSpan(this SyntaxToken token, RazorSourceDocument source)
    {
        var location = token.GetSourceLocation(source);
        var endLocation = source.Text.Lines.GetLinePosition(token.EndPosition);
        var lineCount = endLocation.Line - location.LineIndex;
        return new SourceSpan(location.FilePath, location.AbsoluteIndex, location.LineIndex, location.CharacterIndex, token.Width, lineCount, endLocation.Character);
    }

    /// <summary>
    /// Creates a new tree of nodes with the specified nodes, tokens and trivia replaced.
    /// </summary>
    /// <typeparam name="TRoot">The type of the root node.</typeparam>
    /// <param name="root">The root node of the tree of nodes.</param>
    /// <param name="nodes">The nodes to be replaced.</param>
    /// <param name="computeReplacementNode">A function that computes a replacement node for the
    /// argument nodes. The first argument is the original node. The second argument is the same
    /// node potentially rewritten with replaced descendants.</param>
    public static TRoot ReplaceSyntax<TRoot>(
        this TRoot root,
        IEnumerable<SyntaxNode> nodes,
        Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode,
        IEnumerable<SyntaxToken> tokens,
        Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken)
        where TRoot : SyntaxNode
    {
        return (TRoot)root.ReplaceCore(
            nodes: nodes, computeReplacementNode: computeReplacementNode,
            tokens: tokens, computeReplacementToken: computeReplacementToken);
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
        return (TRoot)root.ReplaceCore<SyntaxNode>(tokens: [oldToken], computeReplacementToken: (o, r) => newToken);
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

    public static string GetContent<TNode>(this TNode node) where TNode : SyntaxNode
    {
        return node.Green.ToString();
    }

    private sealed class DiagnosticSyntaxWalker(List<RazorDiagnostic> diagnostics) : SyntaxWalker
    {
        private readonly List<RazorDiagnostic> _diagnostics = diagnostics ?? [];

        public override void Visit(SyntaxNode node)
        {
            if (node?.ContainsDiagnostics == true)
            {
                var diagnostics = node.GetDiagnostics();

                _diagnostics.AddRange(diagnostics);

                base.Visit(node);
            }
        }

        public override void VisitToken(SyntaxToken token)
        {
            if (token.ContainsDiagnostics == true)
            {
                var diagnostics = token.GetDiagnostics();

                _diagnostics.AddRange(diagnostics);

                base.VisitToken(token);
            }
        }
    }
}
