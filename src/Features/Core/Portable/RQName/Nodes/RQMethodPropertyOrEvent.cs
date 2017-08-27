// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQMethodPropertyOrEvent : RQMember
    {
        public readonly RQMethodPropertyOrEventName RqMemberName;

        public RQMethodPropertyOrEvent(RQUnconstructedType containingType, RQMethodPropertyOrEventName memberName)
            : base(containingType)
        {
            this.RqMemberName = memberName;
        }

        public override string MemberName
        {
            get { return this.RqMemberName.OrdinaryNameValue; }
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            base.AppendChildren(childList);
            childList.Add(this.RqMemberName.ToSimpleTree());
        }
    }
}
