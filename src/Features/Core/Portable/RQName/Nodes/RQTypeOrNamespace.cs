// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQTypeOrNamespace : RQNode
    {
        public readonly ReadOnlyCollection<string> NamespaceNames;

        protected RQTypeOrNamespace(IList<string> namespaceNames)
            => NamespaceNames = new ReadOnlyCollection<string>(namespaceNames);

        protected override void AppendChildren(List<SimpleTreeNode> childList)
            => childList.AddRange(NamespaceNames.Select(name => (SimpleTreeNode)new SimpleGroupNode(RQNameStrings.NsName, name)));
    }
}
