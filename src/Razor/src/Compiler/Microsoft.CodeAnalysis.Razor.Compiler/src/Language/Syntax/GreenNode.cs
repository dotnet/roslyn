// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
internal abstract partial class GreenNode
{
    private static readonly ConditionalWeakTable<GreenNode, RazorDiagnostic[]> s_diagnosticsTable = new();
    private static readonly RazorDiagnostic[] s_emptyDiagnostics = [];

    private int _width;
    private byte _slotCount;

    protected GreenNode(SyntaxKind kind)
    {
        Kind = kind;
    }

    protected GreenNode(SyntaxKind kind, int width)
        : this(kind)
    {
        _width = width;
    }

    protected GreenNode(SyntaxKind kind, RazorDiagnostic[]? diagnostics)
        : this(kind, 0, diagnostics)
    {
    }

    protected GreenNode(SyntaxKind kind, int width, RazorDiagnostic[]? diagnostics)
        : this(kind, width)
    {
        if (diagnostics?.Length > 0)
        {
            Flags |= NodeFlags.ContainsDiagnostics;
            s_diagnosticsTable.Add(this, diagnostics);
        }
    }

    protected void AdjustFlagsAndWidth(GreenNode node)
    {
        if (node == null)
        {
            return;
        }

        Flags |= node.Flags & NodeFlags.InheritMask;
        _width += node.Width;
    }

    #region Kind
    internal SyntaxKind Kind { get; }

    internal virtual bool IsList => false;

    internal virtual bool IsToken => false;
    #endregion

    public int Width => _width;

    #region Slots
    public int SlotCount
    {
        get
        {
            int count = _slotCount;
            if (count == byte.MaxValue)
            {
                count = GetSlotCount();
            }

            return count;
        }

        protected set
        {
            _slotCount = (byte)value;
        }
    }

    internal abstract GreenNode? GetSlot(int index);

    internal GreenNode GetRequiredSlot(int index)
    {
        var node = GetSlot(index);
        Debug.Assert(node is not null);

        return node;
    }

    // for slot counts >= byte.MaxValue
    protected virtual int GetSlotCount()
    {
        return _slotCount;
    }

    public virtual int GetSlotOffset(int index)
    {
        var offset = 0;
        for (var i = 0; i < index; i++)
        {
            var child = GetSlot(i);
            if (child != null)
            {
                offset += child.Width;
            }
        }

        return offset;
    }

    public virtual int FindSlotIndexContainingOffset(int offset)
    {
        Debug.Assert(0 <= offset && offset < Width);

        int i;
        var accumulatedWidth = 0;
        for (i = 0; ; i++)
        {
            Debug.Assert(i < SlotCount);
            var child = GetSlot(i);
            if (child != null)
            {
                accumulatedWidth += child.Width;
                if (offset < accumulatedWidth)
                {
                    break;
                }
            }
        }

        return i;
    }
    #endregion

    #region Flags
    public NodeFlags Flags { get; protected set; }

    internal void SetFlags(NodeFlags flags)
    {
        Flags |= flags;
    }

    internal void ClearFlags(NodeFlags flags)
    {
        Flags &= ~flags;
    }

    internal virtual bool IsMissing => (Flags & NodeFlags.IsMissing) != 0;

    public bool ContainsDiagnostics => (Flags & NodeFlags.ContainsDiagnostics) != 0;
    #endregion

    #region Diagnostics
    internal abstract GreenNode SetDiagnostics(RazorDiagnostic[]? diagnostics);

    internal RazorDiagnostic[] GetDiagnostics()
    {
        if (ContainsDiagnostics)
        {
            if (s_diagnosticsTable.TryGetValue(this, out var diagnostics))
            {
                return diagnostics;
            }
        }

        return s_emptyDiagnostics;
    }
    #endregion

    #region Text
    private string GetDebuggerDisplay()
        => $"{GetType().Name}<{Kind}>";

    public override string ToString()
    {
        // Simple case: Zero width is just an empty string.
        if (_width == 0)
        {
            return string.Empty;
        }

        // Special case: See if there's just a single non-zero-width descendant token.
        // If so, we can just return the content of that token rather than creating a new string for it.
        foreach (var token in Tokens())
        {
            if (token.Width == _width)
            {
                // If this token has the same width as this node, just return it's content.
                return token.Content;
            }

            // At this point, if this is a zero-width token, we know there must be more
            // non-zero-width tokens. Break out of the loop to allocate a new string with all
            // of the content.
            if (token.Width != 0)
            {
                break;
            }

            // This was just a zero-width token - continue looping.
        }

        // At this point, we know that we have multiple tokens and need to allocate a string.
        return string.Create(length: _width, this, static (span, node) =>
        {
            foreach (var token in node.Tokens())
            {
                var content = token.Content.AsSpan();

                if (content.Length > 0)
                {
                    content.CopyTo(span);
                    span = span[content.Length..];
                }
            }

            Debug.Assert(span.IsEmpty);
        });
    }
    #endregion

    #region Equivalence
    public virtual bool IsEquivalentTo([NotNullWhen(true)] GreenNode? other)
    {
        if (this == other)
        {
            return true;
        }

        if (other == null)
        {
            return false;
        }

        return EquivalentToInternal(this, other);
    }

    private static bool EquivalentToInternal(GreenNode node1, GreenNode node2)
    {
        if (node1.Kind != node2.Kind)
        {
            // A single-element list is usually represented as just a single node,
            // but can be represented as a List node with one child. Move to that
            // child if necessary.
            if (node1.IsList && node1.SlotCount == 1)
            {
                node1 = node1.GetRequiredSlot(0);
            }

            if (node2.IsList && node2.SlotCount == 1)
            {
                node2 = node2.GetRequiredSlot(0);
            }

            if (node1.Kind != node2.Kind)
            {
                return false;
            }
        }

        if (node1.Width != node2.Width)
        {
            return false;
        }

        var n = node1.SlotCount;
        if (n != node2.SlotCount)
        {
            return false;
        }

        for (var i = 0; i < n; i++)
        {
            var node1Child = node1.GetSlot(i);
            var node2Child = node2.GetSlot(i);
            if (node1Child != null && node2Child != null && !node1Child.IsEquivalentTo(node2Child))
            {
                return false;
            }
        }

        return true;
    }
    #endregion

    #region Factories
    public SyntaxNode CreateRed()
    {
        return CreateRed(null, 0);
    }

    internal abstract SyntaxNode CreateRed(SyntaxNode? parent, int position);
    #endregion

    public TokenEnumerable Tokens()
        => new(this);

    public Enumerator GetEnumerator()
        => new(this);

    public abstract TResult Accept<TResult>(InternalSyntax.SyntaxVisitor<TResult> visitor);

    public abstract void Accept(InternalSyntax.SyntaxVisitor visitor);
}
