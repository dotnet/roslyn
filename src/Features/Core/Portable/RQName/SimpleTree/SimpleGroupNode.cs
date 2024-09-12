// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

internal class SimpleGroupNode(string text, IList<SimpleTreeNode> children) : SimpleTreeNode(text)
{
    private readonly IList<SimpleTreeNode> _children = children;

    public SimpleGroupNode(string text, string singleLeafChildText) : this(text, new SimpleLeafNode(singleLeafChildText)) { }

    public SimpleGroupNode(string text, params SimpleTreeNode[] children) : this(text, children.ToList()) { }

    public IList<SimpleTreeNode> Children { get { return _children; } }

    public SimpleTreeNode this[int index] { get { return Children[index]; } }

    public int Count { get { return Children.Count; } }
}
