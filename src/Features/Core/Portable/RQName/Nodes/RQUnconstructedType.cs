﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQUnconstructedType : RQTypeOrNamespace<ITypeSymbol>
    {
        public readonly ReadOnlyCollection<RQUnconstructedTypeInfo> TypeInfos;

        public RQUnconstructedType(IList<string> namespaceNames, IList<RQUnconstructedTypeInfo> typeInfos)
            : base(namespaceNames)
        {
            TypeInfos = new ReadOnlyCollection<RQUnconstructedTypeInfo>(typeInfos);
        }

        protected override string RQKeyword
        {
            get { return RQNameStrings.Agg; }
        }

        protected override void AppendChildren(List<SimpleTreeNode> childList)
        {
            base.AppendChildren(childList);

            var typeNodes = from typeInfo in TypeInfos
                            let typeParamCountNode = new SimpleGroupNode(RQNameStrings.TypeVarCnt, typeInfo.TypeVariableCount.ToString())
                            let nameLeaf = new SimpleLeafNode(typeInfo.TypeName)
                            select (SimpleTreeNode)new SimpleGroupNode(RQNameStrings.AggName, nameLeaf, typeParamCountNode);
            childList.AddRange(typeNodes);
        }
    }

    internal readonly struct RQUnconstructedTypeInfo
    {
        public readonly string TypeName;
        public readonly int TypeVariableCount;

        public RQUnconstructedTypeInfo(string typeName, int typeVariableCount)
        {
            TypeName = typeName;
            TypeVariableCount = typeVariableCount;
        }
    }
}
