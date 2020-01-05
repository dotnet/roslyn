// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
