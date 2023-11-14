// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

internal abstract partial class GreenNode
{
    public struct NodeEnumerable(GreenNode node) : IEnumerable<GreenNode>
    {
        public readonly Enumerator GetEnumerator()
            => new(node);

        IEnumerator<GreenNode> IEnumerable<GreenNode>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public struct Enumerator(GreenNode node) : IEnumerator<GreenNode>
        {
            private readonly GreenNode _node = node;
            private readonly ArrayBuilder<Syntax.InternalSyntax.ChildSyntaxList.Enumerator> _stack = ArrayBuilder<Syntax.InternalSyntax.ChildSyntaxList.Enumerator>.GetInstance();

            private bool _started;
            private GreenNode _current = null!;

            public readonly GreenNode Current
                => _current;

            public bool MoveNext()
            {
                if (!_started)
                {
                    _started = true;
                    _current = _node;
                    _stack.Push(_node.ChildNodesAndTokens().GetEnumerator());
                    return true;
                }
                else
                {
                    while (_stack.TryPop(out var currentEnumerator))
                    {
                        while (currentEnumerator.MoveNext())
                        {
                            var current = currentEnumerator.Current;
                            if (current is null)
                                continue;

                            // push back this enumerator back onto the stack as it may still have more elements to give.
                            _stack.Push(currentEnumerator);

                            // also push the children of this current node so we'll walk into those.
                            if (!current.IsToken)
                                _stack.Push(current.ChildNodesAndTokens().GetEnumerator());

                            // Finally, return the current node we ran into back to the caller.
                            _current = current;
                            return true;
                        }
                    }
                }

                return false;
            }

            public void Reset()
            {
                _started = false;
                _stack.Clear();
                _current = null!;
            }

            public readonly void Dispose()
            {
                _stack.Free();
            }

            readonly object IEnumerator.Current => Current;
        }
    }
}
