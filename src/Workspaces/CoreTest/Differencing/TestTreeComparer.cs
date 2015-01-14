// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Differencing.UnitTests
{
    public class TestTreeComparer : TreeComparer<TestNode>
    {
        public static readonly TestTreeComparer Instance = new TestTreeComparer();

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
        {
            return Math.Abs((double)left.Value - right.Value) / TestNode.MaxValue;
        }

        public override bool ValuesEqual(TestNode oldNode, TestNode newNode)
        {
            return oldNode.Value == newNode.Value;
        }

        protected internal override IEnumerable<TestNode> GetChildren(TestNode node)
        {
            return node.Children;
        }

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
        {
            return node.Label;
        }

        protected internal override TextSpan GetSpan(TestNode node)
        {
            return new TextSpan(0, 10);
        }

        protected internal override int TiedToAncestor(int label)
        {
            return 0;
        }

        protected internal override bool TreesEqual(TestNode left, TestNode right)
        {
            return left.Root == right.Root;
        }

        protected internal override bool TryGetParent(TestNode node, out TestNode parent)
        {
            parent = node.Parent;
            return parent != null;
        }
    }
}
