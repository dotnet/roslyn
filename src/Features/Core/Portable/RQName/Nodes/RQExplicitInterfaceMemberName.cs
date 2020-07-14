// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
            => new SimpleGroupNode(RQNameStrings.IntfExplName, InterfaceType.ToSimpleTree(), Name.ToSimpleTree());
    }
}
