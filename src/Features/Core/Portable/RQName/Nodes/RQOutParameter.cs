// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQOutParameter : RQParameter
    {
        public RQOutParameter(RQType type) : base(type) { }

        public override SimpleTreeNode CreateSimpleTreeForType()
        {
            return new SimpleGroupNode(RQNameStrings.ParamMod, new SimpleLeafNode(RQNameStrings.Out), Type.ToSimpleTree());
        }
    }
}
