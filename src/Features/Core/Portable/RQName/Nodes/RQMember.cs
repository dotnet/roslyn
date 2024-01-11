// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQMember(RQUnconstructedType containingType) : RQNode
    {
        public readonly RQUnconstructedType ContainingType = containingType;

        public abstract string MemberName { get; }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
            => childList.Add(ContainingType.ToSimpleTree());
    }
}
