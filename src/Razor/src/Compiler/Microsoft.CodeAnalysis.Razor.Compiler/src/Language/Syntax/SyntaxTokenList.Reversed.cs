// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal readonly partial struct SyntaxTokenList
{
    public readonly struct Reversed(SyntaxTokenList list) : IEnumerable<SyntaxToken>, IEquatable<Reversed>
    {
        private readonly SyntaxTokenList _list = list;

        public Enumerator GetEnumerator() => new(in _list);

        IEnumerator<SyntaxToken> IEnumerable<SyntaxToken>.GetEnumerator()
            => _list.Count == 0
                ? SpecializedCollections.EmptyEnumerator<SyntaxToken>()
                : new EnumeratorImpl(in _list);

        IEnumerator IEnumerable.GetEnumerator()
            => _list.Count == 0
                ? SpecializedCollections.EmptyEnumerator<SyntaxToken>()
                : (IEnumerator)new EnumeratorImpl(in _list);

        public override bool Equals(object? obj)
            => obj is Reversed reversed && Equals(reversed);

        public bool Equals(Reversed other)
            => _list.Equals(other._list);

        public override int GetHashCode()
            => _list.GetHashCode();

        public struct Enumerator
        {
            private readonly SyntaxNode? _parent;
            private readonly GreenNode? _singleNodeOrList;
            private readonly int _baseIndex;
            private readonly int _count;

            private int _index;
            private GreenNode? _current;
            private int _position;

            internal Enumerator(in SyntaxTokenList list)
                : this()
            {
                if (list.Any())
                {
                    _parent = list._parent;
                    _singleNodeOrList = list.Node;
                    _baseIndex = list._index;
                    _count = list.Count;

                    _index = _count;
                    _current = null;

                    var last = list.Last();
                    _position = last.Position + last.Width;
                }
            }

            public bool MoveNext()
            {
                if (_count == 0 || _index <= 0)
                {
                    _current = null;
                    return false;
                }

                _index--;

                Debug.Assert(_singleNodeOrList != null);
                _current = GetGreenNodeAt(_singleNodeOrList, _index);
                Debug.Assert(_current != null);
                _position -= _current.Width;

                return true;
            }

            public readonly SyntaxToken Current
            {
                get
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return new SyntaxToken(_parent, _current, _position, _baseIndex + _index);
                }
            }

            public override readonly bool Equals(object? obj)
                => throw new NotSupportedException();

            public override readonly int GetHashCode()
                => throw new NotSupportedException();
        }

        private sealed class EnumeratorImpl : IEnumerator<SyntaxToken>
        {
            private Enumerator _enumerator;

            internal EnumeratorImpl(in SyntaxTokenList list)
            {
                _enumerator = new Enumerator(in list);
            }

            public SyntaxToken Current => _enumerator.Current;

            object IEnumerator.Current => _enumerator.Current;

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }
    }
}
