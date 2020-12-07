// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial struct SyntaxTriviaList
    {
        /// <summary>
        /// Reversed enumerable.
        /// </summary>
        public readonly struct Reversed : IEnumerable<SyntaxTrivia>, IEquatable<Reversed>
        {
            private readonly SyntaxTriviaList _list;

            public Reversed(SyntaxTriviaList list)
            {
                _list = list;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(in _list);
            }

            IEnumerator<SyntaxTrivia> IEnumerable<SyntaxTrivia>.GetEnumerator()
            {
                if (_list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
                }

                return new ReversedEnumeratorImpl(in _list);
            }

            IEnumerator
                IEnumerable.GetEnumerator()
            {
                if (_list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
                }

                return new ReversedEnumeratorImpl(in _list);
            }

            public override int GetHashCode()
            {
                return _list.GetHashCode();
            }

            public override bool Equals(object? obj)
            {
                return obj is Reversed && Equals((Reversed)obj);
            }

            public bool Equals(Reversed other)
            {
                return _list.Equals(other._list);
            }

            [StructLayout(LayoutKind.Auto)]
            public struct Enumerator
            {
                private readonly SyntaxToken _token;
                private readonly GreenNode? _singleNodeOrList;
                private readonly int _baseIndex;
                private readonly int _count;

                private int _index;
                private GreenNode? _current;
                private int _position;

                internal Enumerator(in SyntaxTriviaList list)
                    : this()
                {
                    if (list.Node is object)
                    {
                        _token = list.Token;
                        _singleNodeOrList = list.Node;
                        _baseIndex = list.Index;
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

                    Debug.Assert(_singleNodeOrList is object);
                    _index--;

                    _current = GetGreenNodeAt(_singleNodeOrList, _index);
                    Debug.Assert(_current is object);
                    _position -= _current.FullWidth;

                    return true;
                }

                public SyntaxTrivia Current
                {
                    get
                    {
                        if (_current == null)
                        {
                            throw new InvalidOperationException();
                        }

                        return new SyntaxTrivia(_token, _current, _position, _baseIndex + _index);
                    }
                }
            }

            private class ReversedEnumeratorImpl : IEnumerator<SyntaxTrivia>
            {
                private Enumerator _enumerator;

                // SyntaxTriviaList is a relatively big struct so is passed as ref
                internal ReversedEnumeratorImpl(in SyntaxTriviaList list)
                {
                    _enumerator = new Enumerator(in list);
                }

                public SyntaxTrivia Current => _enumerator.Current;

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
