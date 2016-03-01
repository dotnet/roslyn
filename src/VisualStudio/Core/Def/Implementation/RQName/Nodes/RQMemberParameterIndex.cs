// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.RQName.SimpleTree;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName.Nodes
{
    internal class RQMemberParameterIndex : RQNode<IParameterSymbol>
    {
        public readonly RQMember ContainingMember;
        public readonly int ParameterIndex;

        public RQMemberParameterIndex(
            RQMember containingMember,
            int parameterIndex)
        {
            this.ContainingMember = containingMember;
            this.ParameterIndex = parameterIndex;
        }

        protected override string RQKeyword => RQNameStrings.MemberParamIndex;

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            childList.Add(ContainingMember.ToSimpleTree());
            childList.Add(new SimpleLeafNode(ParameterIndex.ToString()));
            childList.Add(new SimpleLeafNode(RQNameStrings.NotPartial));
        }
    }
}
