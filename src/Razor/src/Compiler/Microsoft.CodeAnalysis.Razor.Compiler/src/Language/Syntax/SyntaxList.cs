// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract class SyntaxList(InternalSyntax.SyntaxList green, SyntaxNode parent, int position) : SyntaxNode(green, parent, position)
{
    public static SyntaxList<TNode> Create<TNode>(params ReadOnlySpan<TNode> nodes)
        where TNode : SyntaxNode
    {
        if (nodes.Length == 0)
        {
            return default;
        }

        using var builder = new PooledArrayBuilder<TNode>(nodes.Length);
        builder.AddRange(nodes);

        return builder.ToList();
    }

    public static SyntaxList<TNode> Create<TNode>(IEnumerable<TNode> nodes)
        where TNode : SyntaxNode
    {
        using var builder = new PooledArrayBuilder<TNode>();
        builder.AddRange(nodes);

        return builder.ToList();
    }

    public static SyntaxTokenList Create(params ReadOnlySpan<SyntaxToken> tokens)
    {
        if (tokens.Length == 0)
        {
            return default;
        }

        using var builder = new PooledArrayBuilder<SyntaxToken>(tokens.Length);
        builder.AddRange(tokens);

        return builder.ToList();
    }

    public static SyntaxTokenList Create(IEnumerable<SyntaxToken> tokens)
    {
        using var builder = new PooledArrayBuilder<SyntaxToken>();
        builder.AddRange(tokens);

        return builder.ToList();
    }

    // For debugging
#pragma warning disable IDE0051 // Remove unused private members
    private string SerializedValue => $"List: {SlotCount} slots";
#pragma warning restore IDE0051 // Remove unused private members

    protected internal override SyntaxNode ReplaceCore<TNode>(
        IEnumerable<TNode>? nodes = null,
        Func<TNode, TNode, SyntaxNode>? computeReplacementNode = null,
        IEnumerable<SyntaxToken>? tokens = null,
        Func<SyntaxToken, SyntaxToken, SyntaxToken>? computeReplacementToken = null)
        => Assumed.Unreachable<SyntaxNode>();

    protected internal override SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes)
        => Assumed.Unreachable<SyntaxNode>();

    protected internal override SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        => Assumed.Unreachable<SyntaxNode>();

    protected internal override SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens)
        => Assumed.Unreachable<SyntaxNode>();

    protected internal override SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore)
        => Assumed.Unreachable<SyntaxNode>();

    internal sealed class WithTwoChildren : SyntaxList
    {
        private SyntaxNode? _child0;
        private SyntaxNode? _child1;

        internal WithTwoChildren(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        internal override SyntaxNode? GetNodeSlot(int index)
            => index switch
            {
                0 => GetRedElement(ref _child0, 0),
                1 => GetRedElement(ref _child1, 1),
                _ => null,
            };

        internal override SyntaxNode? GetCachedSlot(int index)
            => index switch
            {
                0 => _child0,
                1 => _child1,
                _ => null,
            };
    }

    internal sealed class WithThreeChildren : SyntaxList
    {
        private SyntaxNode? _child0;
        private SyntaxNode? _child1;
        private SyntaxNode? _child2;

        internal WithThreeChildren(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        internal override SyntaxNode? GetNodeSlot(int index)
            => index switch
            {
                0 => GetRedElement(ref _child0, 0),
                1 => GetRedElement(ref _child1, 1),
                2 => GetRedElement(ref _child2, 2),
                _ => null,
            };

        internal override SyntaxNode? GetCachedSlot(int index)
            => index switch
            {
                0 => _child0,
                1 => _child1,
                2 => _child2,
                _ => null,
            };
    }

    internal sealed class WithManyChildren : SyntaxList
    {
        private readonly ArrayElement<SyntaxNode?>[] _children;

        internal WithManyChildren(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
            _children = new ArrayElement<SyntaxNode?>[green.SlotCount];
        }

        internal override SyntaxNode? GetNodeSlot(int index)
            => GetRedElement(ref _children[index].Value, index);

        internal override SyntaxNode? GetCachedSlot(int index)
            => _children[index];
    }
}
