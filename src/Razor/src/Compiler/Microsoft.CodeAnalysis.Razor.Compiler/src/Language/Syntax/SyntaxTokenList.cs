// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

/// <summary>
/// Represents a read-only list of <see cref="SyntaxToken"/>.
/// </summary>
[CollectionBuilder(typeof(SyntaxList), methodName: "Create")]
internal readonly partial struct SyntaxTokenList : IEquatable<SyntaxTokenList>, IReadOnlyList<SyntaxToken>
{
    public static SyntaxTokenList Empty => default;

    internal GreenNode? Node { get; }
    internal int Position { get; }

    private readonly SyntaxNode? _parent;
    private readonly int _index;

    internal SyntaxTokenList(SyntaxNode? parent, GreenNode? tokenOrList, int position, int index)
    {
        Debug.Assert(tokenOrList != null || (position == 0 && index == 0 && parent == null));
        Debug.Assert(position >= 0);
        Debug.Assert(tokenOrList == null || tokenOrList.IsToken || tokenOrList.IsList);

        _parent = parent;
        Node = tokenOrList;
        Position = position;
        _index = index;
    }

    public SyntaxTokenList(SyntaxToken token)
    {
        _parent = token.Parent;
        Node = token.Node;
        Position = token.Position;
        _index = 0;
    }

    public SyntaxTokenList(params ReadOnlySpan<SyntaxToken> tokens)
        : this(parent: null, CreateGreenListNode(tokens), position: 0, index: 0)
    {
    }

    public SyntaxTokenList(IEnumerable<SyntaxToken> tokens)
        : this(parent: null, CreateGreenListNode(tokens), position: 0, index: 0)
    {
    }

    private static GreenNode? CreateGreenListNode(ReadOnlySpan<SyntaxToken> tokens)
    {
        if (tokens.Length == 0)
        {
            return null;
        }

        using var builder = new PooledArrayBuilder<SyntaxToken>(tokens.Length);
        builder.AddRange(tokens);

        return builder.ToGreenListNode();
    }

    private static GreenNode? CreateGreenListNode(IEnumerable<SyntaxToken> tokens)
    {
        using var builder = new PooledArrayBuilder<SyntaxToken>();
        builder.AddRange(tokens);

        return builder.ToGreenListNode();
    }

    /// <summary>
    /// The number of nodes in the list.
    /// </summary>
    public int Count
        => Node == null ? 0 : (Node.IsList ? Node.SlotCount : 1);

    /// <summary>
    /// Gets the node at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the node to get or set.</param>
    /// <returns>The node at the specified index.</returns>
    public SyntaxToken this[int index]
    {
        get
        {
            if (Node != null)
            {
                if (Node.IsList)
                {
                    if (unchecked((uint)index < (uint)Node.SlotCount))
                    {
                        return new SyntaxToken(_parent, Node.GetSlot(index), Position + Node.GetSlotOffset(index), _index + index);
                    }
                }
                else if (index == 0)
                {
                    return new SyntaxToken(_parent, Node, Position, _index);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public TextSpan Span
       => Node == null ? default : TextSpan.FromBounds(Position, Position + Node.Width);

    public override string ToString()
        => Node != null ? Node.ToString() : string.Empty;

    public bool Any() => Node != null;
    public SyntaxToken First() => Any() ? this[0] : throw new InvalidOperationException();
    public SyntaxToken Last() => Any() ? this[^1] : throw new InvalidOperationException();

    private static GreenNode? GetGreenNodeAt(GreenNode node, int index)
    {
        Debug.Assert(node.IsList || index == 0);

        return node.IsList ? node.GetSlot(index) : node;
    }

    public int IndexOf(SyntaxToken tokenInList)
    {
        for (int i = 0, count = Count; i < count; i++)
        {
            if (this[i] == tokenInList)
            {
                return i;
            }
        }

        return -1;
    }

    internal int IndexOf(SyntaxKind kind)
    {
        for (int i = 0, count = Count; i < count; i++)
        {
            if (this[i].Kind == kind)
            {
                return i;
            }
        }

        return -1;
    }

    public SyntaxTokenList Add(SyntaxToken token)
        => Insert(Count, token);

    public SyntaxTokenList AddRange(ReadOnlySpan<SyntaxToken> tokens)
        => InsertRange(Count, tokens);

    public SyntaxTokenList AddRange(IEnumerable<SyntaxToken> tokens)
        => InsertRange(Count, tokens);

    public SyntaxTokenList Insert(int index, SyntaxToken token)
    {
        if (token == default)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(token));
        }

        return InsertRange(index, [token]);
    }

    public SyntaxTokenList InsertRange(int index, ReadOnlySpan<SyntaxToken> tokens)
    {
        var count = Count;

        ArgHelper.ThrowIfNegative(index);
        ArgHelper.ThrowIfGreaterThan(index, count);

        if (tokens.Length == 0)
        {
            return this;
        }

        using var builder = new PooledArrayBuilder<SyntaxToken>(count + tokens.Length);

        // Add current tokens up to 'index'
        builder.AddRange(this, 0, index);

        // Add new tokens
        builder.AddRange(tokens);

        // Add remaining tokens starting from 'index'
        builder.AddRange(this, index, count - index);

        Debug.Assert(builder.Count == count + tokens.Length);

        return builder.ToList();
    }

    public SyntaxTokenList InsertRange(int index, IEnumerable<SyntaxToken> tokens)
    {
        var count = Count;

        ArgHelper.ThrowIfNegative(index);
        ArgHelper.ThrowIfGreaterThan(index, count);
        ArgHelper.ThrowIfNull(tokens);

        if (tokens.TryGetCount(out var tokenCount))
        {
            return InsertRangeWithCount(index, tokens, tokenCount);
        }

        using var builder = new PooledArrayBuilder<SyntaxToken>(count);

        // Add current tokens up to 'index'
        builder.AddRange(this, 0, index);

        var oldCount = builder.Count;

        // Add new tokens
        builder.AddRange(tokens);

        // If builder.Count == oldCount, there weren't any tokens added.
        // So, there's no need to continue.
        if (builder.Count == oldCount)
        {
            return this;
        }

        // Add remaining tokens starting from 'index'
        builder.AddRange(this, index, count - index);

        return builder.ToList();
    }

    private SyntaxTokenList InsertRangeWithCount(int index, IEnumerable<SyntaxToken> tokens, int tokenCount)
    {
        if (tokenCount == 0)
        {
            return this;
        }

        var count = Count;

        using var builder = new PooledArrayBuilder<SyntaxToken>(count + tokenCount);

        // Add current tokens up to 'index'
        builder.AddRange(this, 0, index);

        // Add new tokens
        builder.AddRange(tokens);

        // Add remaining tokens starting from 'index'
        builder.AddRange(this, index, count - index);

        Debug.Assert(builder.Count == count + tokenCount);

        return builder.ToList();
    }

    public SyntaxTokenList RemoveAt(int index)
    {
        var count = Count;

        ArgHelper.ThrowIfNegative(index);
        ArgHelper.ThrowIfGreaterThanOrEqual(index, count);

        // count - 1 because we're removing an item.
        var newCount = count - 1;

        using var builder = new PooledArrayBuilder<SyntaxToken>(newCount);

        // Add current tokens up to 'index'
        builder.AddRange(this, 0, index);

        // Add remaining tokens starting *after* 'index'
        builder.AddRange(this, index + 1, newCount - index);

        return builder.ToList();
    }

    public SyntaxTokenList Remove(SyntaxToken tokenInList)
    {
        var index = IndexOf(tokenInList);
        return index >= 0 ? RemoveAt(index) : this;
    }

    public SyntaxTokenList Replace(SyntaxToken tokenInList, SyntaxToken newToken)
    {
        if (newToken == default)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(newToken));
        }

        return ReplaceRange(tokenInList, [newToken]);
    }

    public SyntaxTokenList ReplaceRange(SyntaxToken tokenInList, ReadOnlySpan<SyntaxToken> tokens)
    {
        var index = IndexOf(tokenInList);

        if (index < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(tokenInList));
        }

        if (tokens.Length == 0)
        {
            return RemoveAt(index);
        }

        // Count - 1 because we're removing an item.
        var newCount = Count - 1;

        using var builder = new PooledArrayBuilder<SyntaxToken>(newCount + tokens.Length);

        // Add current tokens up to 'index'
        builder.AddRange(this, 0, index);

        // Add new tokens
        builder.AddRange(tokens);

        // Add remaining tokens starting *after* 'index'
        builder.AddRange(this, index + 1, newCount - index);

        return builder.ToList();
    }

    public SyntaxTokenList ReplaceRange(SyntaxToken tokenInList, IEnumerable<SyntaxToken> tokens)
    {
        var index = IndexOf(tokenInList);

        if (index < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(tokenInList));
        }

        ArgHelper.ThrowIfNull(tokens);

        if (tokens.TryGetCount(out var tokenCount))
        {
            return ReplaceRangeWithCount(index, tokens, tokenCount);
        }

        // Count - 1 because we're removing an item.
        var newCount = Count - 1;

        using var builder = new PooledArrayBuilder<SyntaxToken>(newCount);

        // Add current tokens up to 'index'
        builder.AddRange(this, 0, index);

        // Add new tokens
        builder.AddRange(tokens);

        // Add remaining tokens starting *after* 'index'
        builder.AddRange(this, index + 1, newCount - index);

        return builder.ToList();
    }

    private SyntaxTokenList ReplaceRangeWithCount(int index, IEnumerable<SyntaxToken> tokens, int tokenCount)
    {
        if (tokenCount == 0)
        {
            return RemoveAt(index);
        }

        // Count - 1 because we're removing an item.
        var newCount = Count - 1;

        using var builder = new PooledArrayBuilder<SyntaxToken>(newCount + tokenCount);

        // Add current tokens up to 'index'
        builder.AddRange(this, 0, index);

        // Add new tokens
        builder.AddRange(tokens);

        Debug.Assert(builder.Count == index + tokenCount);

        // Add remaining tokens starting *after* 'index'
        builder.AddRange(this, index + 1, newCount - index);

        Debug.Assert(builder.Count == newCount + tokenCount);

        return builder.ToList();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SyntaxTokenList list && Equals(list);

    public bool Equals(SyntaxTokenList other)
        => Node == other.Node &&
           _parent == other._parent &&
           _index == other._index;

    public override int GetHashCode()
    {
        // Not call GHC on parent as it's expensive
        var hash = HashCodeCombiner.Start();
        hash.Add(Node);
        hash.Add(_index);

        return hash.CombinedHash;
    }

    public static bool operator ==(SyntaxTokenList left, SyntaxTokenList right)
        => left.Equals(right);

    public static bool operator !=(SyntaxTokenList left, SyntaxTokenList right)
        => !left.Equals(right);

    public Enumerator GetEnumerator()
        => new(in this);

    IEnumerator<SyntaxToken> IEnumerable<SyntaxToken>.GetEnumerator()
        => Node == null
            ? SpecializedCollections.EmptyEnumerator<SyntaxToken>()
            : new EnumeratorImpl(in this);

    IEnumerator IEnumerable.GetEnumerator()
        => Node == null
            ? SpecializedCollections.EmptyEnumerator<SyntaxToken>()
            : (IEnumerator)new EnumeratorImpl(in this);

    public Reversed Reverse()
        => new(this);
}
