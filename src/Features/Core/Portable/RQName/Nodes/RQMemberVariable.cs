﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQMemberVariable : RQMember
    {
        public readonly string Name;

        public RQMemberVariable(RQUnconstructedType containingType, string name)
            : base(containingType)
        {
            Name = name;
        }

        public override string MemberName
        {
            get { return Name; }
        }

        protected override string RQKeyword
        {
            get { return RQNameStrings.MembVar; }
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            base.AppendChildren(childList);
            childList.Add(new SimpleGroupNode(RQNameStrings.MembVarName, Name));
        }
    }
}
