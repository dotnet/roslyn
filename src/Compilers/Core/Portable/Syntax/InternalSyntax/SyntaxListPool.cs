// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define LOG

using System;
#if LOG
using System.Collections.Generic;
using System.Diagnostics;
#endif
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal class SyntaxListPool
    {
        private static readonly ObjectPool<SyntaxListPool> s_listPool = new ObjectPool<SyntaxListPool>(() => new SyntaxListPool());

        private const int InitialFreeListSize = 16;
        private const int InitialBuilderCapacity = 32;

        private ArrayElement<SyntaxListBuilder?>[] _freeList = new ArrayElement<SyntaxListBuilder?>[InitialFreeListSize];
        private int _freeIndex;

#if LOG
        private readonly List<SyntaxListBuilder> _allocated = new List<SyntaxListBuilder>();
#endif

        private SyntaxListPool()
        {
        }

        public static SyntaxListPool GetInstance()
        {
            return s_listPool.Allocate();
        }

        public void Free()
        {
            s_listPool.Free(this);
        }

        internal SyntaxListBuilder Allocate()
        {
            SyntaxListBuilder item;
            if (_freeIndex > 0)
            {
                _freeIndex--;
                item = _freeList[_freeIndex].Value!;
                _freeList[_freeIndex].Value = null;
            }
            else
            {
                item = new SyntaxListBuilder(InitialBuilderCapacity);
            }

#if LOG
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

        internal void Free<TNode>(in SeparatedSyntaxListBuilder<TNode> item) where TNode : GreenNode
        {
            Free(item.UnderlyingBuilder);
        }

        internal void Free(SyntaxListBuilder? item)
        {
            if (item is null)
                return;

#if LOG
            Debug.Assert(_allocated.Contains(item));

            _allocated.Remove(item);
#endif

            // Don't add the builder back to _freelist if the builder has grown too large.
            if (item.Capacity > InitialBuilderCapacity * 2)
                return;

            if (_freeIndex >= _freeList.Length)
            {
                // Don't add the builder back to _freelist if the cache has grown too large.
                if (_freeIndex >= InitialFreeListSize * 2)
                    return;

                this.Grow();
            }

            item.Clear();
            _freeList[_freeIndex].Value = item;
            _freeIndex++;
        }

        private void Grow()
        {
            var tmp = new ArrayElement<SyntaxListBuilder?>[_freeList.Length * 2];
            Array.Copy(_freeList, tmp, _freeList.Length);
            _freeList = tmp;
        }

        public SyntaxList<TNode> ToListAndFree<TNode>(SyntaxListBuilder<TNode> item)
            where TNode : GreenNode
        {
            if (item.IsNull)
                return default;

            var list = item.ToList();
            Free(item);
            return list;
        }

        public SeparatedSyntaxList<TNode> ToListAndFree<TNode>(in SeparatedSyntaxListBuilder<TNode> item)
            where TNode : GreenNode
        {
            var list = item.ToList();
            Free(item);
            return list;
        }
    }
}
