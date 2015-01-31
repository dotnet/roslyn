// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServices.Implementation.RQName.SimpleTree;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName.Nodes
{
    internal class RQMemberVariable : RQMember
    {
        public readonly string Name;

        public RQMemberVariable(RQUnconstructedType containingType, string name)
            : base(containingType)
        {
            this.Name = name;
        }

        public override string MemberName
        {
            get { return this.Name; }
        }

        protected override string RQKeyword
        {
            get { return RQNameStrings.MembVar; }
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            base.AppendChildren(childList);
            childList.Add(new SimpleGroupNode(RQNameStrings.MembVarName, this.Name));
        }
    }
}
