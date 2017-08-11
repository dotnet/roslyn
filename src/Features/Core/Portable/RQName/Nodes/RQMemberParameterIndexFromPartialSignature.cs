// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQMemberParameterIndexFromPartialSignature : RQMemberParameterIndex
    {
        public RQMemberParameterIndexFromPartialSignature(
            RQMember containingMember,
            int parameterIndex)
            : base(containingMember, parameterIndex)
        {
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            childList.Add(ContainingMember.ToSimpleTree());
            childList.Add(new SimpleLeafNode(ParameterIndex.ToString()));
            childList.Add(new SimpleLeafNode(RQNameStrings.PartialSignature));
        }
    }
}
