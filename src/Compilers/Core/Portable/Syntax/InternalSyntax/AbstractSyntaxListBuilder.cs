// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public TGreenNode this[int index]
        {
            get
            {
                return Nodes[index];
            }

            set
            {
                Nodes[index].Value = value;
            }
        }

        public void Add(TGreenNode item)
        {
            if (item == null) return;

            if (item.IsList)
            {
                int slotCount = item.SlotCount;

                // Necessary, but not sufficient (e.g. for nested lists).
                EnsureAdditionalCapacity(slotCount);

                for (int i = 0; i < slotCount; i++)
                {
                    this.Add((TGreenNode)item.GetSlot(i));
                }
            }
            else
            {
                EnsureAdditionalCapacity(1);

                Nodes[Count++].Value = item;
            }
        }

        protected void EnsureAdditionalCapacity(int additionalCount)
        {
            int currentSize = Nodes.Length;
            int requiredSize = this.Count + additionalCount;

            if (requiredSize <= currentSize) return;

            int newSize =
                requiredSize < 8 ? 8 :
                requiredSize >= (int.MaxValue / 2) ? int.MaxValue :
                Math.Max(requiredSize, currentSize * 2); // NB: Size will *at least* double.
            Debug.Assert(newSize >= requiredSize);

            Array.Resize(ref Nodes, newSize);
        }

        [Conditional("DEBUG")]
        protected void Validate(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                Debug.Assert(Nodes[i].Value != null);
            }
        }
    }
}