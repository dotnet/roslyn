// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundTreeRewriter : BoundTreeVisitor
    {
        public virtual TypeSymbol VisitType(TypeSymbol type)
        {
            return type;
        }

        public ImmutableArray<T> VisitList<T>(ImmutableArray<T> list) where T : BoundNode
        {
            if (list.IsDefault)
            {
                return list;
            }

            return DoVisitList(list);
        }

        private ImmutableArray<T> DoVisitList<T>(ImmutableArray<T> list) where T : BoundNode
        {
            ArrayBuilder<T> newList = null;
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                System.Diagnostics.Debug.Assert(item != null);

                var visited = this.Visit(item);
                if (item != visited)
                {
                    if (newList == null)
                    {
                        newList = ArrayBuilder<T>.GetInstance();
                        if (i > 0)
                        {
                            newList.AddRange(list, i);
                        }
                    }
                }

                if (newList != null && visited != null)
                {
                    newList.Add((T)visited);
                }
            }

            if (newList != null)
            {
                return newList.ToImmutableAndFree();
            }

            return list;
        }
    }
}