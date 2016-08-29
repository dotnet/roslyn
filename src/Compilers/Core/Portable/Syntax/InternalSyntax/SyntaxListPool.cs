﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal class SyntaxListPool
    {
        private ArrayElement<SyntaxListBuilder>[] _freeList = new ArrayElement<SyntaxListBuilder>[10];
        private int _freeIndex;

#if DEBUG
        private readonly List<SyntaxListBuilder> _allocated = new List<SyntaxListBuilder>();
#endif

        internal SyntaxListPool()
        {
        }

        internal SyntaxListBuilder Allocate()
        {
            SyntaxListBuilder item;
            if (_freeIndex > 0)
            {
                _freeIndex--;
                item = _freeList[_freeIndex].Value;
                _freeList[_freeIndex].Value = null;
            }
            else
            {
                item = new SyntaxListBuilder(10);
            }

#if DEBUG
            Debug.Assert(!_allocated.Contains(item));
            _allocated.Add(item);
#endif
            return item;
        }

        internal SyntaxListBuilder<TNode> Allocate<TNode>() where TNode : GreenNode
        {
            return new SyntaxListBuilder<TNode>(this.Allocate());
        }

        internal SeparatedSyntaxListBuilder<TNode> AllocateSeparated<TNode>() where TNode : GreenNode
        {
            return new SeparatedSyntaxListBuilder<TNode>(this.Allocate());
        }

        internal void Free<TNode>(SeparatedSyntaxListBuilder<TNode> item) where TNode : GreenNode
        {
            Free(item.UnderlyingBuilder);
        }

        internal void Free(SyntaxListBuilder item)
        {
            item.Clear();
            if (_freeIndex >= _freeList.Length)
            {
                this.Grow();
            }
#if DEBUG
            Debug.Assert(_allocated.Contains(item));

            _allocated.Remove(item);
#endif
            _freeList[_freeIndex].Value = item;
            _freeIndex++;
        }

        private void Grow()
        {
            var tmp = new ArrayElement<SyntaxListBuilder>[_freeList.Length * 2];
            Array.Copy(_freeList, tmp, _freeList.Length);
            _freeList = tmp;
        }

        public SyntaxList<TNode> ToListAndFree<TNode>(SyntaxListBuilder<TNode> item)
            where TNode : GreenNode
        {
            var list = item.ToList();
            Free(item);
            return list;
        }
    }
}