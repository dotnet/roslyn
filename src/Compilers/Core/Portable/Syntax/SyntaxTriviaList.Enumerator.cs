// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    public partial struct SyntaxTriviaList
    {
        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator
        {
            private SyntaxToken _token;
            private GreenNode _singleNodeOrList;
            private int _baseIndex;
            private int _count;

            private int _index;
            private GreenNode _current;
            private int _position;

            internal Enumerator(in SyntaxTriviaList list)
            {
                _token = list.Token;
                _singleNodeOrList = list.Node;
                _baseIndex = list.Index;
                _count = list.Count;

                _index = -1;
                _current = null;
                _position = list.Position;
            }

            // PERF: Passing SyntaxToken by ref since it's a non-trivial struct
            private void InitializeFrom(in SyntaxToken token, GreenNode greenNode, int index, int position)
            {
                _token = token;
                _singleNodeOrList = greenNode;
                _baseIndex = index;
                _count = greenNode.IsList ? greenNode.SlotCount : 1;

                _index = -1;
                _current = null;
                _position = position;
            }

            // PERF: Used to initialize an enumerator for leading trivia directly from a token.
            // This saves constructing an intermediate SyntaxTriviaList. Also, passing token
            // by ref since it's a non-trivial struct
            internal void InitializeFromLeadingTrivia(in SyntaxToken token)
            {
                InitializeFrom(in token, token.Node.GetLeadingTriviaCore(), 0, token.Position);
            }

            // PERF: Used to initialize an enumerator for trailing trivia directly from a token.
            // This saves constructing an intermediate SyntaxTriviaList. Also, passing token
            // by ref since it's a non-trivial struct
            internal void InitializeFromTrailingTrivia(in SyntaxToken token)
            {
                var leading = token.Node.GetLeadingTriviaCore();
                int index = 0;
                if (leading != null)
                {
                    index = leading.IsList ? leading.SlotCount : 1;
                }

                var trailingGreen = token.Node.GetTrailingTriviaCore();
                int trailingPosition = token.Position + token.FullWidth;
                if (trailingGreen != null)
                {
                    trailingPosition -= trailingGreen.FullWidth;
                }

                InitializeFrom(in token, trailingGreen, index, trailingPosition);
            }

            public bool MoveNext()
            {
                int newIndex = _index + 1;
                if (newIndex >= _count)
                {
                    // invalidate iterator
                    _current = null;
                    return false;
                }

                _index = newIndex;

                if (_current != null)
                {
                    _position += _current.FullWidth;
                }

                _current = GetGreenNodeAt(_singleNodeOrList, newIndex);
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

            internal bool TryMoveNextAndGetCurrent(out SyntaxTrivia current)
            {
                if (!MoveNext())
                {
                    current = default;
                    return false;
                }

                current = new SyntaxTrivia(_token, _current, _position, _baseIndex + _index);
                return true;
            }
        }

        private class EnumeratorImpl : IEnumerator<SyntaxTrivia>
        {
            private Enumerator _enumerator;

            // SyntaxTriviaList is a relatively big struct so is passed as ref
            internal EnumeratorImpl(in SyntaxTriviaList list)
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
