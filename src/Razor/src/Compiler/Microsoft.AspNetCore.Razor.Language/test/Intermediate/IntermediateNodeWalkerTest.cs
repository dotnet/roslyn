// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public class IntermediateNodeWalkerTest
{
    [Fact]
    public void IntermediateNodeWalker_Visit_TraversesEntireGraph()
    {
        // Arrange
        var walker = new DerivedIntermediateNodeWalker();

        var nodes = new IntermediateNode[]
        {
            new BasicIntermediateNode("Root"),
                new BasicIntermediateNode("Root->A"),
                new BasicIntermediateNode("Root->B"),
                    new BasicIntermediateNode("Root->B->1"),
                    new BasicIntermediateNode("Root->B->2"),
                new BasicIntermediateNode("Root->C"),
        };

        var builder = new DefaultRazorIntermediateNodeBuilder();

        builder.Push(nodes[0]);
        builder.Add(nodes[1]);

        builder.Push(nodes[2]);
        builder.Add(nodes[3]);
        builder.Add(nodes[4]);
        builder.Pop();

        builder.Add(nodes[5]);

        var root = builder.Pop();

        // Act
        walker.Visit(root);

        // Assert
        Assert.Equal(nodes, walker.Visited);
    }

    [Fact]
    public void IntermediateNodeWalker_Visit_SetsParentAndAncestors()
    {
        // Arrange
        var walker = new DerivedIntermediateNodeWalker();

        var nodes = new IntermediateNode[]
        {
            new BasicIntermediateNode("Root"),
                new BasicIntermediateNode("Root->A"),
                new BasicIntermediateNode("Root->B"),
                    new BasicIntermediateNode("Root->B->1"),
                    new BasicIntermediateNode("Root->B->2"),
                new BasicIntermediateNode("Root->C"),
        };

        var ancestors = new Dictionary<string, string[]>()
        {
            { "Root", [] },
            { "Root->A", ["Root"] },
            { "Root->B", ["Root"] },
            { "Root->B->1", ["Root->B", "Root"] },
            { "Root->B->2", ["Root->B", "Root"] },
            { "Root->C", ["Root"] },
        };

        walker.OnVisiting = (n, a) =>
        {
            var basicNode = Assert.IsType<BasicIntermediateNode>(n);
            var parent = a.Length > 0 ? (BasicIntermediateNode)a[0] : null;

            Assert.Equal(ancestors[basicNode.Name], a.Cast<BasicIntermediateNode>().Select(b => b.Name));
            Assert.Equal(ancestors[basicNode.Name].FirstOrDefault(), parent?.Name);
        };

        var builder = new DefaultRazorIntermediateNodeBuilder();

        builder.Push(nodes[0]);
        builder.Add(nodes[1]);

        builder.Push(nodes[2]);
        builder.Add(nodes[3]);
        builder.Add(nodes[4]);
        builder.Pop();

        builder.Add(nodes[5]);

        var root = builder.Pop();

        // Act & Assert
        walker.Visit(root);
    }

    private sealed class DerivedIntermediateNodeWalker : IntermediateNodeWalker
    {
        public List<IntermediateNode> Visited { get; } = [];

        public Action<IntermediateNode, IntermediateNode[]>? OnVisiting { get; set; }

        public override void VisitDefault(IntermediateNode node)
        {
            Visited.Add(node);

            OnVisiting?.Invoke(node, Ancestors.ToArray());
            base.VisitDefault(node);
        }

        public void VisitBasic(BasicIntermediateNode node)
        {
            VisitDefault(node);
        }
    }

    private sealed class BasicIntermediateNode(string name) : IntermediateNode
    {
        public string Name { get; } = name;

        public override IntermediateNodeCollection Children { get; } = [];

        public override void Accept(IntermediateNodeVisitor visitor)
            => ((DerivedIntermediateNodeWalker)visitor).VisitBasic(this);

        public override string ToString()
            => Name;
    }
}
