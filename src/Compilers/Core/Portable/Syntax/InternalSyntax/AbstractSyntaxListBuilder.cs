// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal abstract class AbstractSyntaxListBuilder<TGreenNode> where TGreenNode : GreenNode
    {
        protected ArrayElement<TGreenNode>[] Nodes;
        public int Count { get; protected set; }

        protected AbstractSyntaxListBuilder(int size)
        {
            Nodes = new ArrayElement<TGreenNode>[size];
        }

        public void Clear()
        {
            this.Count = 0;
        }
    }
}