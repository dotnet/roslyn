﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQType
    {
        public static readonly RQType ObjectType = new RQConstructedType(
            new RQUnconstructedType(["System"], [new RQUnconstructedTypeInfo("Object", 0)]),
            []);

        public abstract SimpleTreeNode ToSimpleTree();
    }
}
