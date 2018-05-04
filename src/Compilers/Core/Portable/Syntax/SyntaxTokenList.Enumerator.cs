// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    ///  Represents a read-only list of <see cref="SyntaxToken"/>s.
    /// </summary>
    public partial struct SyntaxTokenList
    {
        /// <summary>
        /// A structure for enumerating a <see cref="SyntaxTokenList"/>
        /// </summary>
        [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator
        {
            // This enumerator allows us to enumerate through two types of lists.
            // either it looks like:
            //
            //   Parent
            //   |
            //   List
            //   |   \
            //   c1  c2
            //
            // or
            //
            //   Parent
            //   |
            //   c1
            //
            // I.e. in the single child case, we optimize and store the child
            // directly (without any list parent).
            //
            // Enumerating over the single child case is simple.  We just 
            // return it and we're done.
            //
            // In the multi child case, things are a bit more difficult.  We need
            // to return the children in order, while also keeping their offset
            // correct.

            private readonly SyntaxNode _parent;
            private readonly GreenNode _singleNodeOrList;
            private readonly int _baseIndex;
            private readonly int _count;

            private int _index;
            private GreenNode _current;
            private int _position;

            internal Enumerator(in SyntaxTokenList list)
            {
                _parent = list._parent;
                _singleNodeOrList = list.Node;
                _baseIndex = list._index;
                _count = list.Count;

                _index = -1;
                _current = null;
                _position = list.Position;
            }

            /// <summary>
            /// Advances the enumerator to the next token in the collection.
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator
            /// has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                if (_count == 0 || _count <= _index + 1)
                {
                    // invalidate iterator
                    _current = null;
                    return false;
                }

                _index++;

                // Add the length of the previous node to the offset so that
                // the next node's offset is reported correctly.
                if (_current != null)
                {
                    _position += _current.FullWidth;
                }

                _current = GetGreenNodeAt(_singleNodeOrList, _index);
                System.Diagnostics.Debug.Assert(_current != null);
                return true;
            }

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            public SyntaxToken Current
            {
                get
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    // In both the list and the single node case we want to 
                    // return the original root parent as the parent of this
                    // token.
                    return new SyntaxToken(_parent, _current, _position, _baseIndex + _index);
                }
            }

            public override bool Equals(object obj)
            {
                throw new NotSupportedException();
            }

            public override int GetHashCode()
            {
                throw new NotSupportedException();
            }
        }

        private class EnumeratorImpl : IEnumerator<SyntaxToken>
        {
            private Enumerator _enumerator;

            // SyntaxTriviaList is a relatively big struct so is passed by ref
            internal EnumeratorImpl(in SyntaxTokenList list)
            {
                _enumerator = new Enumerator(in list);
            }

            public SyntaxToken Current => _enumerator.Current;

            object IEnumerator.Current => _enumerator.Current;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
