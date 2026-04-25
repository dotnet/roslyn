// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly struct IntermediateNodeReference<T>
    where T : IntermediateNode
{
    public T Node { get; }
    public IntermediateNode Parent { get; }

    public IntermediateNodeReference(T node, IntermediateNode parent)
    {
        ArgHelper.ThrowIfNull(node);
        ArgHelper.ThrowIfNull(parent);

        Node = node;
        Parent = parent;
    }

    public void Deconstruct(out T node, out IntermediateNode parent)
    {
        node = Node;
        parent = Parent;
    }

    // Delegate to a non-generic version for mutation.
    private IntermediateNodeReference Worker => this;

    public IntermediateNodeReference<TNode> InsertAfter<TNode>(TNode node)
        where TNode : IntermediateNode
    {
        Worker.InsertAfter(node);

        return new(node, Parent);
    }

    public void InsertAfter(IEnumerable<IntermediateNode> nodes)
        => Worker.InsertAfter(nodes);

    public IntermediateNodeReference<TNode> InsertBefore<TNode>(TNode node)
        where TNode : IntermediateNode
    {
        Worker.InsertBefore(node);

        return new(node, Parent);
    }

    public void InsertBefore(IEnumerable<IntermediateNode> nodes)
        => Worker.InsertBefore(nodes);

    public void Remove()
        => Worker.Remove();

    public IntermediateNodeReference<TNode> Replace<TNode>(TNode node)
        where TNode : IntermediateNode
    {
        Worker.Replace(node);

        return new(node, Parent);
    }

    private string GetDebuggerDisplay()
        => $"ref: {Parent.GetDebuggerDisplay()} - {Node.GetDebuggerDisplay()}";

    public static implicit operator IntermediateNodeReference(IntermediateNodeReference<T> reference)
        => new(reference.Node, reference.Parent);
}
