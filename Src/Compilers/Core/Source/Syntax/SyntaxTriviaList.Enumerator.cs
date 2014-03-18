// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public partial struct SyntaxTriviaList
    {
        public struct Enumerator
        {
            private readonly SyntaxToken token;
            private readonly GreenNode singleNodeOrList;
            private readonly int baseIndex;
            private readonly int count;

            private int index;
            private GreenNode current;
            private int position;

            internal Enumerator(ref SyntaxTriviaList list)
            {
                this.token = list.token;
                this.singleNodeOrList = list.node;
                this.baseIndex = list.index;
                this.count = list.Count;

                this.index = -1;
                this.current = null;
                this.position = list.position;
            }

            public bool MoveNext()
            {
                int newIndex = this.index + 1;
                if (newIndex >= this.count)
                {
                    // invalidate iterator
                    this.current = null;
                    return false;
                }

                this.index = newIndex;

                if (current != null)
                {
                    this.position += this.current.FullWidth;
                }

                this.current = GetGreenNodeAt(this.singleNodeOrList, newIndex);
                return true;
            }

            public SyntaxTrivia Current
            {
                get
                {
                    if (this.current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return new SyntaxTrivia(this.token, this.current, this.position, this.baseIndex + this.index);
                }
            }
        }

        private class EnumeratorImpl : IEnumerator<SyntaxTrivia>
        {
            private Enumerator enumerator;

            // SyntaxTriviaList is a relatively big struct so is passed as ref
            internal EnumeratorImpl(ref SyntaxTriviaList list)
            {
                this.enumerator = new Enumerator(ref list);
            }

            public SyntaxTrivia Current
            {
                get { return enumerator.Current; }
            }

            object IEnumerator.Current
            {
                get { return enumerator.Current; }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
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