﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQNormalParameter : RQParameter
    {
        public RQNormalParameter(RQType type) : base(type) { }

        public override SimpleTreeNode CreateSimpleTreeForType()
            => Type.ToSimpleTree();
    }
}
