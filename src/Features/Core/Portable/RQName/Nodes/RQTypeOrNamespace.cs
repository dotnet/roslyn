// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQTypeOrNamespace<ResolvedType> : RQNode<ResolvedType>
    {
        public readonly ReadOnlyCollection<string> NamespaceNames;

        protected RQTypeOrNamespace(IList<string> namespaceNames)
        {
            NamespaceNames = new ReadOnlyCollection<string>(namespaceNames);
        }

        public INamespaceSymbol NamespaceIdentifier
        {
            // TODO: C# Specific?
            get { return null; /*new CSharpNamespaceIdentifier(NamespaceNames);*/ }
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            childList.AddRange(NamespaceNames.Select(name => (SimpleTreeNode)new SimpleGroupNode(RQNameStrings.NsName, name)));
        }
    }
}
