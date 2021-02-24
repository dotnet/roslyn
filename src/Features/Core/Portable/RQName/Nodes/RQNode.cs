// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQNode
    {
        protected abstract string RQKeyword { get; }

        protected abstract void AppendChildren(List<SimpleTreeNode> childList);

        public SimpleGroupNode ToSimpleTree()
        {
            var childList = new List<SimpleTreeNode>();
            AppendChildren(childList);
            return new SimpleGroupNode(RQKeyword, childList);
        }
    }
}
