// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial struct SyntaxTokenList
    {
        /// <summary>
        /// Reversed enumerable.
        /// </summary>
        public readonly struct Reversed : IEnumerable<SyntaxToken>, IEquatable<Reversed>
        {
            private readonly SyntaxTokenList _list;

            public Reversed(SyntaxTokenList list)
            {
                _list = list;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(in _list);
            }

            IEnumerator<SyntaxToken> IEnumerable<SyntaxToken>.GetEnumerator()
            {
                if (_list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
                }

                return new EnumeratorImpl(in _list);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                if (_list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
                }

                return new EnumeratorImpl(in _list);
            }

            public override bool Equals(object obj)
            {
                return obj is Reversed && Equals((Reversed)obj);
            }

            public bool Equals(Reversed other)
            {
                return _list.Equals(other._list);
            }

            public override int GetHashCode()
            {
                return _list.GetHashCode();
            }

            [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
            [StructLayout(LayoutKind.Auto)]
            public struct Enumerator
            {
                private readonly SyntaxNode _parent;
                private readonly GreenNode _singleNodeOrList;
                private readonly int _baseIndex;
                private readonly int _count;

                private int _index;
                private GreenNode _current;
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
                        _position = last.Position + last.FullWidth;
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

                    _current = GetGreenNodeAt(_singleNodeOrList, _index);
                    _position -= _current.FullWidth;

                    return true;
                }

                public SyntaxToken Current
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

                // SyntaxTriviaList is a relatively big struct so is passed as ref
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
}
