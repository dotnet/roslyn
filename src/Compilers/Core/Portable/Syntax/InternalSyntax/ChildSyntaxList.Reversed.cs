// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial struct ChildSyntaxList
    {
        internal partial struct Reversed
        {
            private readonly GreenNode _node;

            internal Reversed(GreenNode node)
            {
                _node = node;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_node);
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
