// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQArrayType : RQArrayOrPointerType
    {
        public readonly int Rank;

        public RQArrayType(int rank, RQType elementType)
            : base(elementType)
        {
            Rank = rank;
        }

        public override SimpleTreeNode ToSimpleTree()
        {
            var rankNode = new SimpleLeafNode(Rank.ToString());
            return new SimpleGroupNode(RQNameStrings.Array, rankNode, ElementType.ToSimpleTree());
        }
    }
}
