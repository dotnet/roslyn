// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Differencing;

internal abstract class AbstractSyntaxComparer : TreeComparer<SyntaxNode>
{
    protected const double ExactMatchDist = 0.0;
    protected const double EpsilonDist = 0.00001;

    internal const int IgnoredNode = -1;

    protected readonly SyntaxNode? _oldRoot;
    protected readonly SyntaxNode? _newRoot;
    private readonly IEnumerable<SyntaxNode>? _oldRootChildren;
    private readonly IEnumerable<SyntaxNode>? _newRootChildren;

    // This comparer can operate in two modes:
    // * Top level syntax, which looks at member declarations, but doesn't look inside method bodies etc.
    // * Statement syntax, which looks into member bodies and descends through all statements and expressions
    // This flag is used where there needs to be a distinction made between how these are treated
    protected readonly bool _compareStatementSyntax;

    internal AbstractSyntaxComparer(
        SyntaxNode? oldRoot,
        SyntaxNode? newRoot,
        IEnumerable<SyntaxNode>? oldRootChildren,
        IEnumerable<SyntaxNode>? newRootChildren,
        bool compareStatementSyntax)
    {
        _compareStatementSyntax = compareStatementSyntax;

        _oldRoot = oldRoot;
        _newRoot = newRoot;
        _oldRootChildren = oldRootChildren;
        _newRootChildren = newRootChildren;
    }

    protected internal sealed override bool TreesEqual(SyntaxNode oldNode, SyntaxNode newNode)
        => oldNode.SyntaxTree == newNode.SyntaxTree;

    protected internal sealed override TextSpan GetSpan(SyntaxNode node)
        => node.Span;

    /// <summary>
    /// Calculates distance of two nodes based on their significant parts.
    /// Returns false if the nodes don't have any significant parts and should be compared as a whole.
    /// </summary>
    protected abstract bool TryComputeWeightedDistance(SyntaxNode oldNode, SyntaxNode newNode, out double distance);

    protected abstract bool IsLambdaBodyStatementOrExpression(SyntaxNode node);

    protected internal override bool TryGetParent(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? parent)
    {
        if (node == _oldRoot || node == _newRoot)
        {
            parent = null;
            return false;
        }

        parent = node.Parent;
        while (parent != null && !HasLabel(parent))
        {
            parent = parent.Parent;
        }

        return parent != null;
    }

    protected internal override IEnumerable<SyntaxNode>? GetChildren(SyntaxNode node)
    {
        if (node == _oldRoot)
        {
            return _oldRootChildren;
        }

        if (node == _newRoot)
        {
            return _newRootChildren;
        }

        return HasChildren(node) ? EnumerateChildren(node) : null;
    }

    private IEnumerable<SyntaxNode> EnumerateChildren(SyntaxNode node)
    {
        foreach (var child in node.ChildNodes())
        {
            if (IsLambdaBodyStatementOrExpression(child))
            {
                continue;
            }

            if (HasLabel(child))
            {
                yield return child;
            }
            else if (_compareStatementSyntax)
            {
                foreach (var descendant in child.DescendantNodes(DescendIntoChildren))
                {
                    if (HasLabel(descendant))
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }
    private bool DescendIntoChildren(SyntaxNode node)
        => !IsLambdaBodyStatementOrExpression(node) && !HasLabel(node);

    protected internal sealed override IEnumerable<SyntaxNode> GetDescendants(SyntaxNode node)
    {
        var rootChildren = (node == _oldRoot) ? _oldRootChildren : (node == _newRoot) ? _newRootChildren : null;
        return (rootChildren != null) ? EnumerateDescendants(rootChildren) : EnumerateDescendants(node);
    }

    private IEnumerable<SyntaxNode> EnumerateDescendants(IEnumerable<SyntaxNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (HasLabel(node))
            {
                yield return node;
            }

            foreach (var descendant in EnumerateDescendants(node))
            {
                if (HasLabel(descendant))
                {
                    yield return descendant;
                }
            }
        }
    }

    private IEnumerable<SyntaxNode> EnumerateDescendants(SyntaxNode node)
    {
        foreach (var descendant in node.DescendantNodesAndTokens(
            descendIntoChildren: ShouldEnumerateChildren,
            descendIntoTrivia: false))
        {
            var descendantNode = descendant.AsNode();
            if (descendantNode != null && HasLabel(descendantNode))
            {
                if (!IsLambdaBodyStatementOrExpression(descendantNode))
                {
                    yield return descendantNode;
                }
            }
        }

        bool ShouldEnumerateChildren(SyntaxNode child)
        {
            // if we don't want to consider this nodes children, then don't
            if (!HasChildren(child))
            {
                return false;
            }

            // Always descend into the children of the node we were asked about
            if (child == node)
            {
                return true;
            }

            // otherwise, as long as we don't descend into lambdas
            return !IsLambdaBodyStatementOrExpression(child);
        }
    }

    protected bool HasChildren(SyntaxNode node)
    {
        // Leaves are labeled statements that don't have a labeled child.
        // We also return true for non-labeled statements.
        var label = Classify(node.RawKind, node, out var isLeaf);

        // ignored should always be reported as leaves for top syntax, but for statements
        // we want to look at all child nodes, because almost anything could have a lambda
        if (!_compareStatementSyntax)
        {
            Debug.Assert(label != IgnoredNode || isLeaf);
        }

        return !isLeaf;
    }

    internal bool HasLabel(SyntaxNode node)
        => Classify(node.RawKind, node, out _) != IgnoredNode;

    internal abstract int Classify(int kind, SyntaxNode? node, out bool isLeaf);

    protected internal override int GetLabel(SyntaxNode node)
        => Classify(node.RawKind, node, out _);

}
