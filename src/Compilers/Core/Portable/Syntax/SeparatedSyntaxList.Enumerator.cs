// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    public partial struct SeparatedSyntaxList<TNode>
    {
        // Public struct enumerator
        // Only implements enumerator pattern as used by foreach
        // Does not implement IEnumerator. Doing so would require the struct to implement IDisposable too.
        [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
        public struct Enumerator
        {
            private readonly SeparatedSyntaxList<TNode> _list;
            private int _index;

            internal Enumerator(in SeparatedSyntaxList<TNode> list)
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
                    return _list[_index];
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

        // IEnumerator wrapper for Enumerator.
        private class EnumeratorImpl : IEnumerator<TNode>
        {
            private Enumerator _e;

            internal EnumeratorImpl(in SeparatedSyntaxList<TNode> list)
            {
                _e = new Enumerator(in list);
            }

            public TNode Current
            {
                get
                {
                    return _e.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return _e.Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return _e.MoveNext();
            }

            public void Reset()
            {
                _e.Reset();
            }
        }
    }
}
