// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQMethodOrProperty : RQMethodPropertyOrEvent
    {
        public readonly int TypeParameterCount;
        public readonly ReadOnlyCollection<RQParameter> Parameters;

        public RQMethodOrProperty(
            RQUnconstructedType containingType,
            RQMethodPropertyOrEventName memberName,
            int typeParameterCount,
            IList<RQParameter> parameters)
            : base(containingType, memberName)
        {
            TypeParameterCount = typeParameterCount;
            Parameters = new ReadOnlyCollection<RQParameter>(parameters);
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            base.AppendChildren(childList);
            childList.Add(new SimpleGroupNode(RQNameStrings.TypeVarCnt, TypeParameterCount.ToString()));
            var paramNodes = Parameters.Select(param => param.ToSimpleTree()).ToList();
            childList.Add(new SimpleGroupNode(RQNameStrings.Params, paramNodes));
        }
    }
}
