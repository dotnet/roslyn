// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Differencing.UnitTests;

public sealed class TestTreeComparer : TreeComparer<TestNode>
{
    public static readonly TestTreeComparer Instance = new();

    private TestTreeComparer()
    {
    }

    protected internal override int LabelCount
    {
        get
        {
            return TestNode.MaxLabel + 1;
        }
    }

    public override double GetDistance(TestNode left, TestNode right)
        => Math.Abs((double)left.Value - right.Value) / TestNode.MaxValue;

    public override bool ValuesEqual(TestNode oldNode, TestNode newNode)
        => oldNode.Value == newNode.Value;

    protected internal override IEnumerable<TestNode> GetChildren(TestNode node)
        => node.Children;

    protected internal override IEnumerable<TestNode> GetDescendants(TestNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in GetDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    protected internal override int GetLabel(TestNode node)
        => node.Label;

    protected internal override TextSpan GetSpan(TestNode node)
        => new(0, 10);

    protected internal override int TiedToAncestor(int label)
        => 0;

    protected internal override bool TreesEqual(TestNode left, TestNode right)
        => left.Root == right.Root;

    protected internal override bool TryGetParent(TestNode node, out TestNode parent)
    {
        parent = node.Parent;
        return parent != null;
    }
}
