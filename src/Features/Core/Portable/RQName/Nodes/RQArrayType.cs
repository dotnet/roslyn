// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes;

internal class RQArrayType(int rank, RQType elementType) : RQArrayOrPointerType(elementType)
{
    public readonly int Rank = rank;

    public override SimpleTreeNode ToSimpleTree()
    {
        var rankNode = new SimpleLeafNode(Rank.ToString());
        return new SimpleGroupNode(RQNameStrings.Array, rankNode, ElementType.ToSimpleTree());
    }
}
