// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct TypeWalker
    {
        private readonly Type _type;

        public TypeWalker(Type type)
        {
            _type = type;
        }

        public TypeEnumerator GetEnumerator()
        {
            return new TypeEnumerator(_type);
        }

        internal struct TypeEnumerator : IDisposable
        {
            private bool _first;
            private ArrayBuilder<Type> _stack;

            public TypeEnumerator(Type type)
            {
                _first = true;
                _stack = ArrayBuilder<Type>.GetInstance();
                _stack.Push(type);
            }

            public readonly Type Current => _stack.Peek();

            public bool MoveNext()
            {
                if (_first)
                {
                    _first = false;
                    return true;
                }

                if (_stack.Count == 0)
                {
                    return false;
                }

                var curr = _stack.Pop();

                if (curr.HasElementType)
                {
                    _stack.Push(curr.GetElementType());
                    return true;
                }

                // Push children in reverse order so they get popped in forward order.
                var children = curr.GetGenericArguments();
                var numChildren = children.Length;
                for (int i = numChildren - 1; i >= 0; i--)
                {
                    _stack.Push(children[i]);
                }

                return _stack.Count > 0;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                Debug.Assert(_stack != null);
                _stack.Free();
                _stack = null;
            }
        }
    }
}
