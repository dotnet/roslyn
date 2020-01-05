// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public readonly partial struct ChildSyntaxList
    {
        public readonly partial struct Reversed : IEnumerable<SyntaxNodeOrToken>, IEquatable<Reversed>
        {
            private readonly SyntaxNode _node;
            private readonly int _count;

            internal Reversed(SyntaxNode node, int count)
            {
                _node = node;
                _count = count;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_node, _count);
            }

            IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
            {
                if (_node == null)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
                }

                return new EnumeratorImpl(_node, _count);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                if (_node == null)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
                }

                return new EnumeratorImpl(_node, _count);
            }

            public override int GetHashCode()
            {
                return _node != null ? Hash.Combine(_node.GetHashCode(), _count) : 0;
            }

            public override bool Equals(object obj)
            {
                return (obj is Reversed) && Equals((Reversed)obj);
            }

            public bool Equals(Reversed other)
            {
                return _node == other._node
                    && _count == other._count;
            }

            public struct Enumerator
            {
                private readonly SyntaxNode _node;
                private readonly int _count;
                private int _childIndex;

                internal Enumerator(SyntaxNode node, int count)
                {
                    _node = node;
                    _count = count;
                    _childIndex = count;
                }

                public bool MoveNext()
                {
                    return --_childIndex >= 0;
                }

                public SyntaxNodeOrToken Current
                {
                    get
                    {
                        return ItemInternal(_node, _childIndex);
                    }
                }

                public void Reset()
                {
                    _childIndex = _count;
                }
            }

            private class EnumeratorImpl : IEnumerator<SyntaxNodeOrToken>
            {
                private Enumerator _enumerator;

                internal EnumeratorImpl(SyntaxNode node, int count)
                {
                    _enumerator = new Enumerator(node, count);
                }

                /// <summary>
                /// Gets the element in the collection at the current position of the enumerator.
                /// </summary>
                /// <returns>
                /// The element in the collection at the current position of the enumerator.
                ///   </returns>
                public SyntaxNodeOrToken Current
                {
                    get { return _enumerator.Current; }
                }

                /// <summary>
                /// Gets the element in the collection at the current position of the enumerator.
                /// </summary>
                /// <returns>
                /// The element in the collection at the current position of the enumerator.
                ///   </returns>
                object IEnumerator.Current
                {
                    get { return _enumerator.Current; }
                }

                /// <summary>
                /// Advances the enumerator to the next element of the collection.
                /// </summary>
                /// <returns>
                /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
                /// </returns>
                /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                /// <summary>
                /// Sets the enumerator to its initial position, which is before the first element in the collection.
                /// </summary>
                /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                public void Reset()
                {
                    _enumerator.Reset();
                }

                /// <summary>
                /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                /// </summary>
                public void Dispose()
                { }
            }
        }
    }
}
