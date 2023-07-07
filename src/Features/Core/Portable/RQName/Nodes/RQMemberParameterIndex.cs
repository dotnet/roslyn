﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQMemberParameterIndex : RQNode
    {
        public readonly RQMember ContainingMember;
        public readonly int ParameterIndex;

        public RQMemberParameterIndex(
            RQMember containingMember,
            int parameterIndex)
        {
            ContainingMember = containingMember;
            ParameterIndex = parameterIndex;
        }

        protected override string RQKeyword
        {
            get { return RQNameStrings.MemberParamIndex; }
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            childList.Add(ContainingMember.ToSimpleTree());
            childList.Add(new SimpleLeafNode(ParameterIndex.ToString()));
            childList.Add(new SimpleLeafNode(RQNameStrings.NotPartial));
        }
    }
}
