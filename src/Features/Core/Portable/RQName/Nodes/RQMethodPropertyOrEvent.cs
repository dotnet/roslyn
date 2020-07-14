// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
            RqMemberName = memberName;
        }

        public override string MemberName
        {
            get { return RqMemberName.OrdinaryNameValue; }
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            base.AppendChildren(childList);
            childList.Add(RqMemberName.ToSimpleTree());
        }
    }
}
