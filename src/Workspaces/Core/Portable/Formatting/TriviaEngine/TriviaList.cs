// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal struct TriviaList : IEnumerable<SyntaxTrivia>
    {
        private readonly SyntaxTriviaList _list1;
        private readonly SyntaxTriviaList _list2;

        public TriviaList(SyntaxTriviaList list1, SyntaxTriviaList list2)
        {
            _list1 = list1;
            _list2 = list2;
        }

        public int Count
        {
            get
            {
                return _list1.Count + _list2.Count;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<SyntaxTrivia> IEnumerable<SyntaxTrivia>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public struct Enumerator : IEnumerator<SyntaxTrivia>
        {
            private readonly SyntaxTriviaList _list1;
            private readonly SyntaxTriviaList _list2;

            private SyntaxTriviaList.Enumerator _enumerator;
            private int _index;

            internal Enumerator(TriviaList triviaList)
            {
                _list1 = triviaList._list1;
                _list2 = triviaList._list2;

                _index = -1;
                _enumerator = _list1.GetEnumerator();
            }

            public bool MoveNext()
            {
                _index++;
                if (_index == _list1.Count)
                {
                    _enumerator = _list2.GetEnumerator();
                }

                return _enumerator.MoveNext();
            }

            public SyntaxTrivia Current => _enumerator.Current;

            void IDisposable.Dispose()
            {
            }

            void IEnumerator.Reset()
            {
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }
        }
    }
}
