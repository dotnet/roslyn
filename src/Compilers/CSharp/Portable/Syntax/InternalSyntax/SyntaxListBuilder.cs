﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class SyntaxListBuilder : AbstractSyntaxListBuilder
    {
        private ArrayElement<CSharpSyntaxNode>[] _nodes;
        public int Count { get; private set; }

        public SyntaxListBuilder(int size)
        {
            _nodes = new ArrayElement<CSharpSyntaxNode>[size];
        }

        public void Clear()
        {
            this.Count = 0;
        }

        public CSharpSyntaxNode this[int index]
        {
            get
            {
                return _nodes[index];
            }

            set
            {
                _nodes[index].Value = value;
            }
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

                _nodes[Count++].Value = item;
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

        [Conditional("DEBUG")]
        private void Validate(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                Debug.Assert(_nodes[i].Value != null);
            }
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
            _nodes[Count].Value = null;
        }

        private void EnsureAdditionalCapacity(int additionalCount)
        {
            int currentSize = _nodes.Length;
            int requiredSize = this.Count + additionalCount;

            if (requiredSize <= currentSize) return;

            int newSize =
                requiredSize < 8 ? 8 :
                requiredSize >= (int.MaxValue / 2) ? int.MaxValue :
                Math.Max(requiredSize, currentSize * 2); // NB: Size will *at least* double.
            Debug.Assert(newSize >= requiredSize);

            Array.Resize(ref _nodes, newSize);
        }

        public bool Any(SyntaxKind kind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_nodes[i].Value.Kind == kind)
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
                array[i] = _nodes[i];
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
                    return _nodes[0];
                case 2:
                    return SyntaxList.List(_nodes[0], _nodes[1]);
                case 3:
                    return SyntaxList.List(_nodes[0], _nodes[1], _nodes[2]);
                default:
                    var tmp = new ArrayElement<CSharpSyntaxNode>[this.Count];
                    Array.Copy(_nodes, tmp, this.Count);
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
