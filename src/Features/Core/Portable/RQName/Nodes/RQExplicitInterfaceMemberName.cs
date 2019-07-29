// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQExplicitInterfaceMemberName : RQMethodPropertyOrEventName
    {
        public readonly RQType InterfaceType;
        public readonly RQOrdinaryMethodPropertyOrEventName Name;

        public RQExplicitInterfaceMemberName(RQType interfaceType, RQOrdinaryMethodPropertyOrEventName name)
        {
            InterfaceType = interfaceType;
            Name = name;
        }

        public override string OrdinaryNameValue
        {
            get { return Name.OrdinaryNameValue; }
        }

        public override SimpleGroupNode ToSimpleTree()
        {
            return new SimpleGroupNode(RQNameStrings.IntfExplName, InterfaceType.ToSimpleTree(), Name.ToSimpleTree());
        }
    }
}
