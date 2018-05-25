// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Features.RQName.SimpleTree
{
    internal class SimpleGroupNode : SimpleTreeNode
    {
        private readonly IList<SimpleTreeNode> _children;

        public SimpleGroupNode(string text, IList<SimpleTreeNode> children) : base(text)
        {
            _children = children;
        }

        public SimpleGroupNode(string text, string singleLeafChildText) : this(text, new SimpleLeafNode(singleLeafChildText)) { }

        public SimpleGroupNode(string text, params SimpleTreeNode[] children) : this(text, children.ToList()) { }

        public IList<SimpleTreeNode> Children { get { return _children; } }

        public SimpleTreeNode this[int index] { get { return Children[index]; } }

        public int Count { get { return Children.Count; } }
    }
}
