// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal abstract partial class GreenNode
{
    [NonCopyable]
    public ref struct NodeEnumerable(GreenNode node)
    {
        private readonly GreenNode _node = node;

        public readonly Enumerator GetEnumerator()
            => new Enumerator(_node);

        [NonCopyable]
        public ref struct Enumerator
        {
            private readonly ArrayBuilder<Syntax.InternalSyntax.ChildSyntaxList.Enumerator> _stack;

            private bool _started;
            private GreenNode _current;

            public Enumerator(GreenNode node)
            {
                _current = node;
                _stack = ArrayBuilder<Syntax.InternalSyntax.ChildSyntaxList.Enumerator>.GetInstance();
                _stack.Push(node.ChildNodesAndTokens().GetEnumerator());
            }

            public readonly void Dispose()
                => _stack.Free();

            public readonly GreenNode Current
            {
                get
                {
                    Debug.Assert(_started);
                    return _current;
                }
            }

            public bool MoveNext()
            {
                if (!_started)
                {
                    // First call that starts the whole process.  We don't actually want to start processing the stack
                    // yet.  We just want to return the original node (which we already stored into _current).
                    _started = true;
                    return true;
                }
                else
                {
                    while (_stack.TryPop(out var currentEnumerator))
                    {
                        if (currentEnumerator.MoveNext())
                        {
                            _current = currentEnumerator.Current;

                            // push back this enumerator back onto the stack as it may still have more elements to give.
                            _stack.Push(currentEnumerator);

                            // also push the children of this current node so we'll walk into those.
                            if (!_current.IsToken)
                                _stack.Push(_current.ChildNodesAndTokens().GetEnumerator());

                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
