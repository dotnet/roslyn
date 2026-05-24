// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal static partial class LegacySyntaxNodeExtensions
{
    private class SpanData
    {
        public SyntaxNodeOrToken? Previous;
        public bool PreviousComputed;
        public SyntaxNodeOrToken? Next;
        public bool NextComputed;
    }

    /// <summary>
    ///  Caches previous/next span result for a particular node. A conditional weak table
    ///  is used to avoid adding fields to all syntax nodes.
    /// </summary>
    private static readonly ConditionalWeakTable<SyntaxNode, SpanData> s_spanDataTable = new();

    private static readonly FrozenSet<SyntaxKind> s_transitionSpanKinds = FrozenSet.Create(
        SyntaxKind.CSharpTransition,
        SyntaxKind.MarkupTransition);

    private static readonly FrozenSet<SyntaxKind> s_commentSpanKinds = FrozenSet.Create(
        SyntaxKind.RazorCommentTransition,
        SyntaxKind.RazorCommentStar,
        SyntaxKind.RazorCommentLiteral);

    private static readonly FrozenSet<SyntaxKind> s_codeSpanKinds = FrozenSet.Create(
        SyntaxKind.CSharpStatementLiteral,
        SyntaxKind.CSharpExpressionLiteral,
        SyntaxKind.CSharpEphemeralTextLiteral);

    private static readonly FrozenSet<SyntaxKind> s_markupSpanKinds = FrozenSet.Create(
        SyntaxKind.MarkupTextLiteral,
        SyntaxKind.MarkupEphemeralTextLiteral);

    private static readonly FrozenSet<SyntaxKind> s_allSpanKinds = CreateAllSpanKindsSet();

    private static FrozenSet<SyntaxKind> CreateAllSpanKindsSet()
    {
        var set = new HashSet<SyntaxKind>();

        set.UnionWith(s_transitionSpanKinds);
        set.Add(SyntaxKind.RazorMetaCode);
        set.UnionWith(s_commentSpanKinds);
        set.UnionWith(s_codeSpanKinds);
        set.UnionWith(s_markupSpanKinds);
        set.Add(SyntaxKind.UnclassifiedTextLiteral);

        return set.ToFrozenSet();
    }

    internal static ISpanChunkGenerator? GetChunkGenerator(this SyntaxNode node)
     => (node as ILegacySyntax)?.ChunkGenerator;

    public static SpanEditHandler? GetEditHandler(this SyntaxNode node)
        => (node as ILegacySyntax)?.EditHandler;

    [Obsolete("Use FindToken or FindInnermostNode instead", error: false)]
    public static SyntaxNode? LocateOwner(this SyntaxNode node, SourceChange change)
    {
        ArgHelper.ThrowIfNull(node);

        if (change.Span.AbsoluteIndex < node.Position)
        {
            // Early escape for cases where changes overlap multiple spans
            // In those cases, the span will return false, and we don't want to search the whole tree
            // So if the current span starts after the change, we know we've searched as far as we need to
            return null;
        }

        if (node.EndPosition < change.Span.AbsoluteIndex)
        {
            // no need to look into this node as it completely precedes the change
            return null;
        }

        if (node.IsSpanKind())
        {
            var editHandler = node.GetEditHandler() ?? SpanEditHandler.GetDefault(AcceptedCharactersInternal.Any);
            return editHandler.OwnsChange(node, change) ? node : null;
        }

        return node switch
        {
            MarkupStartTagSyntax startTag => LocateOwnerForSyntaxList(startTag.LegacyChildren, change),
            MarkupEndTagSyntax endTag => LocateOwnerForSyntaxList(endTag.LegacyChildren, change),
            MarkupTagHelperStartTagSyntax startTagHelper => LocateOwnerForSyntaxList(startTagHelper.LegacyChildren, change),
            MarkupTagHelperEndTagSyntax endTagHelper => LocateOwnerForSyntaxList(endTagHelper.LegacyChildren, change),
            _ => LocateOwnerForChildSyntaxList(node.ChildNodesAndTokens(), change)
        };

        static SyntaxNode? LocateOwnerForSyntaxList(in SyntaxList<RazorSyntaxNode> list, SourceChange change)
        {
            foreach (var child in list)
            {
                if (child.LocateOwner(change) is { } owner)
                {
                    return owner;
                }
            }

            return null;
        }

        static SyntaxNode? LocateOwnerForChildSyntaxList(in ChildSyntaxList list, SourceChange change)
        {
            foreach (var child in list)
            {
                if (child.AsNode(out var node) && node.LocateOwner(change) is { } owner)
                {
                    return owner;
                }
            }

            return null;
        }
    }

    public static bool IsTransitionSpanKind(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return s_transitionSpanKinds.Contains(node.Kind);
    }

    public static bool IsMetaCodeSpanKind(this SyntaxNodeOrToken nodeOrToken)
    {
        return nodeOrToken.AsNode(out var node) && node.IsMetaCodeSpanKind();
    }

    public static bool IsMetaCodeSpanKind(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return node.Kind is SyntaxKind.RazorMetaCode;
    }

    public static bool IsCommentSpanKind(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return s_commentSpanKinds.Contains(node.Kind);
    }

    public static bool IsCodeSpanKind(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return s_codeSpanKinds.Contains(node.Kind);
    }

    public static bool IsMarkupSpanKind(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return s_markupSpanKinds.Contains(node.Kind);
    }

    public static bool IsNoneSpanKind(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return node.Kind is SyntaxKind.UnclassifiedTextLiteral;
    }

    public static bool IsSpanKind(this SyntaxNode node)
        => s_allSpanKinds.Contains(node.Kind);

    public static bool IsSpanKind(this SyntaxNodeOrToken nodeOrToken)
        => s_allSpanKinds.Contains(nodeOrToken.Kind);

    private static IEnumerable<SyntaxNodeOrToken> FlattenSpansInReverse(this SyntaxNode node)
    {
        using var stack = new ChildSyntaxListReversedEnumeratorStack(node);

        while (stack.TryGetNextNodeOrToken(out var nextNode))
        {
            if (nextNode.AsNode() is MarkupStartTagSyntax startTag)
            {
                var children = startTag.LegacyChildren;

                for (var i = children.Count - 1; i >= 0; i--)
                {
                    var tagChild = children[i];
                    if (tagChild.IsSpanKind())
                    {
                        yield return tagChild;
                    }
                }
            }
            else if (nextNode.AsNode() is MarkupEndTagSyntax endTag)
            {
                var children = endTag.LegacyChildren;

                for (var i = children.Count - 1; i >= 0; i--)
                {
                    var tagChild = children[i];
                    if (tagChild.IsSpanKind())
                    {
                        yield return tagChild;
                    }
                }
            }
            else if (nextNode.IsSpanKind())
            {
                yield return nextNode;
            }
        }
    }

    public static IEnumerable<SyntaxNode> FlattenSpans(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        foreach (var child in node.DescendantNodes())
        {
            if (child is MarkupStartTagSyntax startTag)
            {
                foreach (var tagChild in startTag.LegacyChildren)
                {
                    if (tagChild.IsSpanKind())
                    {
                        yield return tagChild;
                    }
                }
            }
            else if (child is MarkupEndTagSyntax endTag)
            {
                foreach (var tagChild in endTag.LegacyChildren)
                {
                    if (tagChild.IsSpanKind())
                    {
                        yield return tagChild;
                    }
                }
            }
            else if (child.IsSpanKind())
            {
                yield return child;
            }
        }
    }

    public static SyntaxNodeOrToken? PreviousSpan(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var spanData = s_spanDataTable.GetOrCreateValue(node);

        lock (spanData)
        {
            if (spanData.PreviousComputed)
            {
                return spanData.Previous;
            }

            var parent = node.Parent;
            while (parent is not null)
            {
                foreach (var span in parent.FlattenSpansInReverse())
                {
                    if (span.EndPosition <= node.Position && span != node)
                    {
                        spanData.PreviousComputed = true;
                        spanData.Previous = span;

                        return span;
                    }
                }

                parent = parent.Parent;
            }

            spanData.PreviousComputed = true;
            spanData.Previous = default;

            return default;
        }
    }

    public static SyntaxNodeOrToken? NextSpan(this SyntaxNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var spanData = s_spanDataTable.GetOrCreateValue(node);

        lock (spanData)
        {
            if (spanData.NextComputed)
            {
                return spanData.Next;
            }

            var parent = node.Parent;
            while (parent is not null)
            {
                foreach (var span in parent.FlattenSpans())
                {
                    if (span.Position >= node.Position && span != node)
                    {
                        spanData.NextComputed = true;
                        spanData.Next = span;

                        return span;
                    }
                }

                parent = parent.Parent;
            }

            spanData.NextComputed = true;
            spanData.Next = default;

            return null;
        }
    }
}
