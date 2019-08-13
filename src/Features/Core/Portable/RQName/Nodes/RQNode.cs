// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    // an unresolved but parsed representation of an RQ Name
    internal abstract class UnresolvedRQNode
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
