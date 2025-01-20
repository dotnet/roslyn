// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Differencing;

/// <summary>
/// Represents an edit operation on a tree or a sequence of nodes.
/// </summary>
/// <typeparam name="TNode">Tree node.</typeparam>
[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
public readonly struct Edit<TNode> : IEquatable<Edit<TNode>>
{
    private readonly TreeComparer<TNode> _comparer;

    internal Edit(EditKind kind, TreeComparer<TNode> comparer, TNode oldNode, TNode newNode)
    {
        Debug.Assert((oldNode == null || oldNode.Equals(null)) == (kind == EditKind.Insert));
        Debug.Assert((newNode == null || newNode.Equals(null)) == (kind == EditKind.Delete));

        Debug.Assert(comparer == null ||
                     oldNode == null || oldNode.Equals(null) ||
                     newNode == null || newNode.Equals(null) ||
                     !comparer.TreesEqual(oldNode, newNode));

        _comparer = comparer;
        Kind = kind;
        OldNode = oldNode;
        NewNode = newNode;
    }

    public EditKind Kind { get; }

    /// <summary>
    /// Insert: 
    /// default(TNode).
    /// 
    /// Delete: 
    /// Deleted node.
    /// 
    /// Move, Update: 
    /// Node in the old tree/sequence.
    /// </summary>
    public TNode OldNode { get; }

    /// <summary>
    /// Insert: 
    /// Inserted node.
    /// 
    /// Delete: 
    /// default(TNode)
    /// 
    /// Move, Update:
    /// Node in the new tree/sequence.
    /// </summary>
    public TNode NewNode { get; }

    public override bool Equals(object obj)
        => obj is Edit<TNode> && Equals((Edit<TNode>)obj);

    public bool Equals(Edit<TNode> other)
    {
        return Kind == other.Kind
            && (OldNode == null) ? other.OldNode == null : OldNode.Equals(other.OldNode)
            && (NewNode == null) ? other.NewNode == null : NewNode.Equals(other.NewNode);
    }

    public override int GetHashCode()
    {
        var hash = (int)Kind;
        if (OldNode != null)
        {
            hash = Hash.Combine(OldNode.GetHashCode(), hash);
        }

        if (NewNode != null)
        {
            hash = Hash.Combine(NewNode.GetHashCode(), hash);
        }

        return hash;
    }

    // Has to be 'internal' for now as it's used by EnC test tool
    internal string GetDebuggerDisplay()
    {
        var result = Kind.ToString();
        switch (Kind)
        {
            case EditKind.Delete:
                return result + " [" + OldNode.ToString() + "]" + DisplayPosition(OldNode);

            case EditKind.Insert:
                return result + " [" + NewNode.ToString() + "]" + DisplayPosition(NewNode);

            case EditKind.Update:
                return result + " [" + OldNode.ToString() + "]" + DisplayPosition(OldNode) + " -> [" + NewNode.ToString() + "]" + DisplayPosition(NewNode);

            case EditKind.Move:
            case EditKind.Reorder:
                return result + " [" + OldNode.ToString() + "]" + DisplayPosition(OldNode) + " -> " + DisplayPosition(NewNode);
        }

        return result;
    }

    private string DisplayPosition(TNode node)
        => (_comparer != null) ? "@" + _comparer.GetSpan(node).Start : "";
}
