// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQErrorType : RQType
    {
        public readonly string Name;

        public RQErrorType(string name)
        {
            Name = name;
        }

        public override SimpleTreeNode ToSimpleTree()
        {
            return new SimpleGroupNode(RQNameStrings.Error, Name);
        }
    }
}
