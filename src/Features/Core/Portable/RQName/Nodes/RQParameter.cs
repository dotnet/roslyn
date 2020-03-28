﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQParameter
    {
        public readonly RQType Type;
        public RQParameter(RQType type)
        {
            System.Diagnostics.Debug.Assert(type != null);
            Type = type;
        }

        public SimpleTreeNode ToSimpleTree()
        {
            return new SimpleGroupNode(RQNameStrings.Param, CreateSimpleTreeForType());
        }

        public abstract SimpleTreeNode CreateSimpleTreeForType();
    }
}
