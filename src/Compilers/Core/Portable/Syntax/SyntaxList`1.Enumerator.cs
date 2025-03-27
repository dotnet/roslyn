// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    public readonly partial struct SyntaxList<TNode>
    {
        [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
        public struct Enumerator
        {
            private readonly SyntaxList<TNode> _list;
            private int _index;

            internal Enumerator(SyntaxList<TNode> list)
            {
                _list = list;
                _index = -1;
            }

            public bool MoveNext()
            {
                int newIndex = _index + 1;
                if (newIndex < _list.Count)
                {
                    _index = newIndex;
                    return true;
                }

                return false;
            }

            public TNode Current
            {
                get
                {
                    return (TNode)_list.ItemInternal(_index)!;
                }
            }

            public void Reset()
            {
                _index = -1;
            }

            public override bool Equals(object? obj)
            {
                throw new NotSupportedException();
            }

            public override int GetHashCode()
            {
                throw new NotSupportedException();
            }
        }

        private class EnumeratorImpl : IEnumerator<TNode>
        {
            private Enumerator _e;

            internal EnumeratorImpl(in SyntaxList<TNode> list)
            {
                _e = new Enumerator(list);
            }

            public bool MoveNext()
            {
                return _e.MoveNext();
            }

            public TNode Current
            {
                get
                {
                    return _e.Current;
                }
            }

            void IDisposable.Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    return _e.Current;
                }
            }

            void IEnumerator.Reset()
            {
                _e.Reset();
            }
        }
    }
}
