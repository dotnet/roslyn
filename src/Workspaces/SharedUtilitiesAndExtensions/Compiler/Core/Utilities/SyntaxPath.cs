// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

/// <summary>
/// Stores the "path" from the root of a tree to a node, allowing the node to be recovered in a
/// later snapshot of the tree, under certain circumstances.
/// 
/// The implementation stores the child indices to represent the path, so any edit which affects
/// the child indices could render this object unable to recover its node.  NOTE: One thing C#
/// IDE has done in the past to do a better job of this is to store the fully qualified name of
/// the member to at least be able to descend into the same member.  We could apply the same sort
/// of logic here.
/// </summary>
internal sealed record class SyntaxPath
{
    // A path is made up of 'segments' that lead from root all the way down to the node. The
    // segment contains the index of the node in its parent, as well as the kind of the node.
    // The latter is not strictly necessary.  However, it ensures that resolving the path against
    // a different tree will either return the same type of node as the original, or will fail.  
    private readonly struct PathSegment
    {
        public int Ordinal { get; }
        public int Kind { get; }

        public PathSegment(int ordinal, int kind)
            : this()
        {
            this.Ordinal = ordinal;
            this.Kind = kind;
        }
    }

    private readonly List<PathSegment> _segments = [];
    private readonly int _kind;
    private readonly bool _trackKinds;

    public SyntaxPath(SyntaxNodeOrToken nodeOrToken, bool trackKinds = true)
    {
        _trackKinds = trackKinds;
        _kind = nodeOrToken.RawKind;
        AddSegment(nodeOrToken);
        _segments.TrimExcess();
    }

    private void AddSegment(SyntaxNodeOrToken nodeOrToken)
    {
        var parent = nodeOrToken.Parent;
        if (parent != null)
        {
            AddSegment(parent);

            // TODO(cyrusn): Is there any way to optimize this for large lists?  I would like to
            // be able to do a binary search.  However, there's no easy way to tell if a node
            // comes before or after another node when searching through a list.  To determine
            // the location of a node, a linear walk is still needed to find it in its parent
            // collection.

            var ordinal = 0;
            var kind = nodeOrToken.RawKind;
            foreach (var child in parent.ChildNodesAndTokens())
            {
                if (nodeOrToken == child)
                {
                    _segments.Add(new PathSegment(ordinal, nodeOrToken.RawKind));
                    return;
                }

                if (!_trackKinds || child.RawKind == kind)
                {
                    ordinal++;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Attempts to recover the node at this path in the provided tree.  If the node is found
    /// then 'true' is returned, otherwise the result is 'false' and 'node' will be null.
    /// </summary>
    public bool TryResolve(SyntaxNode? root, out SyntaxNodeOrToken nodeOrToken)
    {
        nodeOrToken = default;

        var current = (SyntaxNodeOrToken)root;
        foreach (var segment in _segments)
        {
            current = FindChild(current, segment);

            if (current.RawKind == 0)
            {
                return false;
            }
        }

        if (!_trackKinds || current.RawKind == _kind)
        {
            nodeOrToken = current;
            return true;
        }

        return false;
    }

    private SyntaxNodeOrToken FindChild(SyntaxNodeOrToken current, PathSegment segment)
    {
        var ordinal = segment.Ordinal;
        foreach (var child in current.ChildNodesAndTokens())
        {
            if (!_trackKinds || child.RawKind == segment.Kind)
            {
                if (ordinal == 0)
                {
                    return child;
                }
                else
                {
                    ordinal--;
                }
            }
        }

        return default;
    }

    public bool TryResolve<TNode>(SyntaxTree syntaxTree, CancellationToken cancellationToken, [NotNullWhen(true)] out TNode? node)
        where TNode : SyntaxNode
    {
        return TryResolve(syntaxTree.GetRoot(cancellationToken), out node);
    }

    public bool TryResolve<TNode>(SyntaxNode? root, [NotNullWhen(true)] out TNode? node)
        where TNode : SyntaxNode
    {
        if (TryResolve(root, out var nodeOrToken) &&
            nodeOrToken.IsNode &&
            nodeOrToken.AsNode() is TNode n)
        {
            node = n;
            return true;
        }

        node = null;
        return false;
    }

    public bool Equals(SyntaxPath? other)
    {
        if (other == null)
        {
            return false;
        }

        if (object.ReferenceEquals(this, other))
        {
            return true;
        }

        return
            _trackKinds == other._trackKinds &&
            _kind == other._kind &&
            _segments.SequenceEqual(other._segments, static (x, y) => x.Equals(y));
    }

    public override int GetHashCode()
        => Hash.Combine(_trackKinds, Hash.Combine(_kind, GetSegmentHashCode()));

    private int GetSegmentHashCode()
    {
        var hash = 1;

        foreach (var segment in _segments)
            hash = Hash.Combine(Hash.Combine(segment.Kind, segment.Ordinal), hash);

        return hash;
    }
}
