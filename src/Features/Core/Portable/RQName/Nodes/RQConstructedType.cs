// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes;

internal class RQConstructedType(RQUnconstructedType definingType, IList<RQType> typeArguments) : RQType
{
    public readonly RQUnconstructedType DefiningType = definingType;
    public readonly ReadOnlyCollection<RQType> TypeArguments = new ReadOnlyCollection<RQType>(typeArguments);

    public override SimpleTreeNode ToSimpleTree()
    {
        var typeArgumentNodes = TypeArguments.Select(node => node.ToSimpleTree()).ToList();
        var typeParamsNode = new SimpleGroupNode(RQNameStrings.TypeParams, typeArgumentNodes);
        return new SimpleGroupNode(RQNameStrings.AggType, DefiningType.ToSimpleTree(), typeParamsNode);
    }
}
