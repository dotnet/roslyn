// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal class PooledSyntaxListBuilder<TNode> : IDisposable where TNode : GreenNode 
    {
        public readonly SyntaxListBuilder<TNode> Builder;
        public readonly SyntaxListPool Pool;
        internal PooledSyntaxListBuilder(SyntaxListPool Pool, SyntaxListBuilder<TNode> Builder)
        {
            this.Builder = Builder;
            this.Pool = Pool;
        }
        public bool Any()
        {
            return this.Builder.ToListNode() != null;
        }

        public static void SwapWith(ref PooledSyntaxListBuilder<TNode> thisnode, ref PooledSyntaxListBuilder<TNode> withThis)
        {
            var tmp = thisnode;
            thisnode = withThis;
            withThis = tmp;
        }
        public static implicit operator SyntaxList<TNode>(PooledSyntaxListBuilder<TNode> pslb)
        {
            Debug.Assert(pslb != null);
            return pslb.Builder.ToList();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    this.Pool.Free(this.Builder);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        public static implicit operator SyntaxListBuilder(PooledSyntaxListBuilder<TNode> pslb) 
        {
            return pslb.Builder;
        }
       

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~PooledSyntaxListBuilder() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
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

        internal PooledSyntaxListBuilder<TNode> PoolAllocate<TNode>() where TNode : GreenNode
        {
            return new PooledSyntaxListBuilder<TNode>(this,this.Allocate<TNode>());
        }
        internal  SyntaxListBuilder<TNode> Allocate<TNode>() where TNode : GreenNode
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
