// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Highlighting;

internal abstract class AbstractKeywordHighlighter<TNode>(bool findInsideTrivia = true)
    : AbstractKeywordHighlighter(findInsideTrivia)
    where TNode : SyntaxNode
{
    protected sealed override bool IsHighlightableNode(SyntaxNode node)
        => node is TNode;

    protected sealed override void AddHighlightsForNode(SyntaxNode node, List<TextSpan> highlights, CancellationToken cancellationToken)
        => AddHighlights((TNode)node, highlights, cancellationToken);

    protected abstract void AddHighlights(TNode node, List<TextSpan> highlights, CancellationToken cancellationToken);
}

internal abstract class AbstractKeywordHighlighter(bool findInsideTrivia = true) : IHighlighter
{
    private readonly bool _findInsideTrivia = findInsideTrivia;

    private static readonly ObjectPool<List<TextSpan>> s_textSpanListPool = new(() => []);

    protected abstract bool IsHighlightableNode(SyntaxNode node);

    protected virtual bool ContainsHighlightableToken(ref TemporaryArray<SyntaxToken> tokens)
        => true;

    public void AddHighlights(
        SyntaxNode root, int position, List<TextSpan> highlights, CancellationToken cancellationToken)
    {
        // We only look at a max of 4 tokens (two trivia, and two non-trivia), so a temp-array is ideal here
        using var touchingTokens = TemporaryArray<SyntaxToken>.Empty;
        AddTouchingTokens(root, position, ref touchingTokens.AsRef());
        if (!ContainsHighlightableToken(ref touchingTokens.AsRef()))
            return;

        using var _2 = s_textSpanListPool.GetPooledObject(out var highlightsBuffer);
        foreach (var token in touchingTokens)
        {
            for (var parent = token.Parent; parent != null; parent = parent.Parent)
            {
                if (IsHighlightableNode(parent))
                {
                    highlightsBuffer.Clear();
                    AddHighlightsForNode(parent, highlightsBuffer, cancellationToken);

                    if (AnyIntersects(position, highlightsBuffer))
                    {
                        highlights.AddRange(highlightsBuffer);
                        return;
                    }
                }
            }
        }
    }

    private static bool AnyIntersects(int position, List<TextSpan> highlights)
    {
        foreach (var highlight in highlights)
        {
            if (highlight.IntersectsWith(position))
                return true;
        }

        return false;
    }

    protected abstract void AddHighlightsForNode(SyntaxNode node, List<TextSpan> highlights, CancellationToken cancellationToken);

    protected static TextSpan EmptySpan(int position)
        => new(position, 0);

    internal void AddTouchingTokens(SyntaxNode root, int position, ref TemporaryArray<SyntaxToken> tokens)
    {
        AddTouchingTokens(root, position, ref tokens, findInsideTrivia: true);
        if (_findInsideTrivia)
            AddTouchingTokens(root, position, ref tokens, findInsideTrivia: false);
    }

    private static void AddTouchingTokens(
        SyntaxNode root, int position, ref TemporaryArray<SyntaxToken> tokens, bool findInsideTrivia)
    {
        var token = root.FindToken(position, findInsideTrivia);
        if (!tokens.Contains(token))
            tokens.Add(token);

        if (position == 0)
            return;

        var previous = root.FindToken(position - 1, findInsideTrivia);
        if (previous.Span.End == position && !tokens.Contains(previous))
            tokens.Add(previous);
    }
}
