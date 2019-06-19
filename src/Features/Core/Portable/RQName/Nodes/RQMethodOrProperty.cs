// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
