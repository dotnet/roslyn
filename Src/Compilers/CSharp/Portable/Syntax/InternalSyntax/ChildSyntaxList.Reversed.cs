// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial struct ChildSyntaxList
    {
        internal partial struct Reversed
        {
            private readonly GreenNode node;

            internal Reversed(GreenNode node)
            {
                this.node = node;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(node);
            }

#if DEBUG
#pragma warning disable 618
            [Obsolete("For debugging", error: true)]
            private GreenNode[] Nodes
            {
                get
                {
                    var result = new List<GreenNode>();
                    foreach (var n in this)
                    {
                        result.Add(n);
                    }

                    return result.ToArray();
                }
            }

#pragma warning restore 618
#endif
        }
    }
}