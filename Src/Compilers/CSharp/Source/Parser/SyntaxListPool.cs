// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class SyntaxListPool
    {
        private ArrayElement<SyntaxListBuilder>[] freeList = new ArrayElement<SyntaxListBuilder>[10];
        private int freeIndex = 0;

#if DEBUG
        private readonly List<SyntaxListBuilder> allocated = new List<SyntaxListBuilder>();
#endif

        internal SyntaxListPool()
        {
        }

        internal SyntaxListBuilder Allocate()
        {
            SyntaxListBuilder item;
            if (freeIndex > 0)
            {
                freeIndex--;
                item = freeList[freeIndex].Value;
                freeList[freeIndex].Value = null;
            }
            else
            {
                item = new SyntaxListBuilder(10);
            }

#if DEBUG
            Debug.Assert(!allocated.Contains(item));
            allocated.Add(item);
#endif
            return item;
        }

        internal SyntaxListBuilder<TNode> Allocate<TNode>() where TNode : CSharpSyntaxNode
        {
            return new SyntaxListBuilder<TNode>(this.Allocate());
        }

        internal SeparatedSyntaxListBuilder<TNode> AllocateSeparated<TNode>() where TNode : CSharpSyntaxNode
        {
            return new SeparatedSyntaxListBuilder<TNode>(this.Allocate());
        }

        internal void Free<TNode>(SeparatedSyntaxListBuilder<TNode> item) where TNode : CSharpSyntaxNode
        {
            Free(item.UnderlyingBuilder);
        }

        internal void Free(SyntaxListBuilder item)
        {
            item.Clear();
            if (freeIndex >= freeList.Length)
            {
                this.Grow();
            }
#if DEBUG
            Debug.Assert(allocated.Contains(item));

            allocated.Remove(item);
#endif
            freeList[freeIndex].Value = item;
            freeIndex++;
        }

        private void Grow()
        {
            var tmp = new ArrayElement<SyntaxListBuilder>[freeList.Length * 2];
            Array.Copy(freeList, tmp, freeList.Length);
            freeList = tmp;
        }
    }
}