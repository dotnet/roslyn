// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial struct ChildSyntaxList
    {
        internal readonly partial struct Reversed
        {
            private readonly GreenNode? _node;

            internal Reversed(GreenNode? node)
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
#pragma warning disable IDE0051 // Remove unused private members
            private GreenNode[] Nodes
#pragma warning restore IDE0051 // Remove unused private members
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
