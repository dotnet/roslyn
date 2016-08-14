// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class SyntaxListPool
    {
        private ArrayElement<CommonSyntaxListBuilder>[] _freeList = new ArrayElement<CommonSyntaxListBuilder>[10];
        private int _freeIndex;

#if DEBUG
        private readonly List<CommonSyntaxListBuilder> _allocated = new List<CommonSyntaxListBuilder>();
#endif

        internal SyntaxListPool()
        {
        }

        internal CommonSyntaxListBuilder Allocate()
        {
            CommonSyntaxListBuilder item;
            if (_freeIndex > 0)
            {
                _freeIndex--;
                item = _freeList[_freeIndex].Value;
                _freeList[_freeIndex].Value = null;
            }
            else
            {
                item = new CommonSyntaxListBuilder(10);
            }

#if DEBUG
            Debug.Assert(!_allocated.Contains(item));
            _allocated.Add(item);
#endif
            return item;
        }

        internal CommonSyntaxListBuilder<TNode> Allocate<TNode>() where TNode : CSharpSyntaxNode
        {
            return new CommonSyntaxListBuilder<TNode>(this.Allocate());
        }

        internal CommonSeparatedSyntaxListBuilder<TNode> AllocateSeparated<TNode>() where TNode : CSharpSyntaxNode
        {
            return new CommonSeparatedSyntaxListBuilder<TNode>(this.Allocate());
        }

        internal void Free<TNode>(CommonSeparatedSyntaxListBuilder<TNode> item) where TNode : CSharpSyntaxNode
        {
            Free(item.UnderlyingBuilder);
        }

        internal void Free(CommonSyntaxListBuilder item)
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
            var tmp = new ArrayElement<CommonSyntaxListBuilder>[_freeList.Length * 2];
            Array.Copy(_freeList, tmp, _freeList.Length);
            _freeList = tmp;
        }
    }
}