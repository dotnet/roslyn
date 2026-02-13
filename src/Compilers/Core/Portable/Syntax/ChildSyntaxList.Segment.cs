// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial struct ChildSyntaxList
    {
        internal readonly struct Segment
        {
            private readonly SyntaxNode? _node;
            private readonly int _count;
            private readonly int _initialIndex;

            internal Segment(SyntaxNode node, int initialIndex)
            {
                _node = node;
                _count = CountNodes(node.Green);
                _initialIndex = initialIndex;
            }

            internal Segment(SyntaxNode node, int initialIndex, int terminalIndex)
            {
                _node = node;
                _count = terminalIndex;
                _initialIndex = initialIndex;
            }

            public Enumerator GetEnumerator()
            {
                Debug.Assert(_node is object);
                return new Enumerator(_node, _count, _initialIndex);
            }
        }
    }
}
