// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQTypeVariableType : RQType
    {
        public readonly string Name;

        public RQTypeVariableType(string name)
            => Name = name;

        public override SimpleTreeNode ToSimpleTree()
            => new SimpleGroupNode(RQNameStrings.TyVar, Name);
    }
}
