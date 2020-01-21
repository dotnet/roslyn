// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQMember : RQNode<IFieldSymbol>
    {
        public readonly RQUnconstructedType ContainingType;

        public RQMember(RQUnconstructedType containingType)
        {
            ContainingType = containingType;
        }

        public abstract string MemberName { get; }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            childList.Add(ContainingType.ToSimpleTree());
        }
    }
}
