// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes;

internal abstract class RQMethodOrProperty(
    RQUnconstructedType containingType,
    RQMethodPropertyOrEventName memberName,
    int typeParameterCount,
    IList<RQParameter> parameters) : RQMethodPropertyOrEvent(containingType, memberName)
{
    public readonly int TypeParameterCount = typeParameterCount;
    public readonly ReadOnlyCollection<RQParameter> Parameters = new(parameters);

    protected override void AppendChildren(List<SimpleTreeNode> childList)
    {
        base.AppendChildren(childList);
        childList.Add(new SimpleGroupNode(RQNameStrings.TypeVarCnt, TypeParameterCount.ToString()));
        var paramNodes = Parameters.Select(param => param.ToSimpleTree()).ToList();
        childList.Add(new SimpleGroupNode(RQNameStrings.Params, paramNodes));
    }
}
