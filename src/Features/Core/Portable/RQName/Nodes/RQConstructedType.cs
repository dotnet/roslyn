// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQConstructedType : RQType
    {
        public readonly RQUnconstructedType DefiningType;
        public readonly ReadOnlyCollection<RQType> TypeArguments;

        public RQConstructedType(RQUnconstructedType definingType, IList<RQType> typeArguments)
        {
            this.DefiningType = definingType;
            this.TypeArguments = new ReadOnlyCollection<RQType>(typeArguments);
        }

        public override SimpleTreeNode ToSimpleTree()
        {
            var typeArgumentNodes = this.TypeArguments.Select(node => node.ToSimpleTree()).ToList();
            var typeParamsNode = new SimpleGroupNode(RQNameStrings.TypeParams, typeArgumentNodes);
            return new SimpleGroupNode(RQNameStrings.AggType, this.DefiningType.ToSimpleTree(), typeParamsNode);
        }
    }
}
