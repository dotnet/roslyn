// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class SyntaxListBuilder : AbstractSyntaxListBuilder<CSharpSyntaxNode>
    {
        public SyntaxListBuilder(int size) : base(size)
        {
        }

        public void Add(CSharpSyntaxNode item)
        {
            if (item == null) return;

            if (item.IsList)
            {
                int slotCount = item.SlotCount;

                // Necessary, but not sufficient (e.g. for nested lists).
                EnsureAdditionalCapacity(slotCount);

                for (int i = 0; i < slotCount; i++)
                {
                    this.Add((CSharpSyntaxNode)item.GetSlot(i));
                }
            }
            else
            {
                EnsureAdditionalCapacity(1);

                Nodes[Count++].Value = item;
            }
        }

        public void AddRange(CSharpSyntaxNode[] items)
        {
            this.AddRange(items, 0, items.Length);
        }

        public void AddRange(CSharpSyntaxNode[] items, int offset, int length)
        {
            // Necessary, but not sufficient (e.g. for nested lists).
            EnsureAdditionalCapacity(length - offset);

            int oldCount = this.Count;

            for (int i = offset; i < length; i++)
            {
                Add(items[i]);
            }

            Validate(oldCount, this.Count);
        }

        public void AddRange(SyntaxList<CSharpSyntaxNode> list)
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxList<CSharpSyntaxNode> list, int offset, int length)
        {
            // Necessary, but not sufficient (e.g. for nested lists).
            EnsureAdditionalCapacity(length - offset);

            int oldCount = this.Count;

            for (int i = offset; i < length; i++)
            {
                Add(list[i]);
            }

            Validate(oldCount, this.Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list) where TNode : CSharpSyntaxNode
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list, int offset, int length) where TNode : CSharpSyntaxNode
        {
            this.AddRange(new SyntaxList<CSharpSyntaxNode>(list.Node), offset, length);
        }

        internal void RemoveLast()
        {
            Count--;
            Nodes[Count].Value = null;
        }

        private void EnsureAdditionalCapacity(int additionalCount)
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

        public bool Any(SyntaxKind kind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Nodes[i].Value.Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public CSharpSyntaxNode[] ToArray()
        {
            var array = new CSharpSyntaxNode[this.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Nodes[i];
            }

            return array;
        }

        internal CSharpSyntaxNode ToListNode()
        {
            switch (this.Count)
            {
                case 0:
                    return null;
                case 1:
                    return Nodes[0];
                case 2:
                    return SyntaxList.List(Nodes[0], Nodes[1]);
                case 3:
                    return SyntaxList.List(Nodes[0], Nodes[1], Nodes[2]);
                default:
                    var tmp = new ArrayElement<CSharpSyntaxNode>[this.Count];
                    Array.Copy(Nodes, tmp, this.Count);
                    return SyntaxList.List(tmp);
            }
        }

        public static implicit operator SyntaxList<CSharpSyntaxNode>(SyntaxListBuilder builder)
        {
            if (builder == null)
            {
                return default(SyntaxList<CSharpSyntaxNode>);
            }

            return builder.ToList();
        }
    }
}
