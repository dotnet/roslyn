// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal abstract class AbstractSyntaxListBuilder
    {
        protected ArrayElement<GreenNode>[] Nodes;
        public int Count { get; protected set; }

        protected AbstractSyntaxListBuilder(int size)
        {
            Nodes = new ArrayElement<GreenNode>[size];
        }

        public void Clear()
        {
            this.Count = 0;
        }

        public bool Any(int kind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Nodes[i].Value.RawKind == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}