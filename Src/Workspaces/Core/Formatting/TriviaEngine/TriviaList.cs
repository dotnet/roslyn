// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal struct TriviaList : IEnumerable<SyntaxTrivia>
    {
        private readonly SyntaxTriviaList list1;
        private readonly SyntaxTriviaList list2;

        public TriviaList(SyntaxTriviaList list1, SyntaxTriviaList list2)
        {
            this.list1 = list1;
            this.list2 = list2;
        }

        public int Count
        {
            get
            {
                return list1.Count + list2.Count;
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
            private readonly SyntaxTriviaList list1;
            private readonly SyntaxTriviaList list2;

            private SyntaxTriviaList.Enumerator enumerator;
            private int index;

            internal Enumerator(TriviaList triviaList)
            {
                this.list1 = triviaList.list1;
                this.list2 = triviaList.list2;

                this.index = -1;
                this.enumerator = list1.GetEnumerator();
            }

            public bool MoveNext()
            {
                index++;
                if (index == list1.Count)
                {
                    enumerator = list2.GetEnumerator();
                }

                return enumerator.MoveNext();
            }

            public SyntaxTrivia Current
            {
                get { return enumerator.Current; }
            }

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
