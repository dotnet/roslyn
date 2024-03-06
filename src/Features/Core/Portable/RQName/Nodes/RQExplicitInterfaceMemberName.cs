// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes;

internal class RQExplicitInterfaceMemberName(RQType interfaceType, RQOrdinaryMethodPropertyOrEventName name) : RQMethodPropertyOrEventName
{
    public readonly RQType InterfaceType = interfaceType;
    public readonly RQOrdinaryMethodPropertyOrEventName Name = name;

    public override string OrdinaryNameValue
    {
        get { return Name.OrdinaryNameValue; }
    }

    public override SimpleGroupNode ToSimpleTree()
        => new(RQNameStrings.IntfExplName, InterfaceType.ToSimpleTree(), Name.ToSimpleTree());
}
