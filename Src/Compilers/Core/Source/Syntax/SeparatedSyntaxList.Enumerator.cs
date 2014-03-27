// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public partial struct SeparatedSyntaxList<TNode>
    {
        // Public struct enumerator
        // Only implements enumerator pattern as used by foreach
        // Does not implement IEnumerator. Doing so would require the struct to implement IDisposable too.
        public struct Enumerator
        {
            private readonly SeparatedSyntaxList<TNode> list;
            private int index;

            internal Enumerator(SeparatedSyntaxList<TNode> list)
            {
                this.list = list;
                this.index = -1;
            }

            public bool MoveNext()
            {
                int newIndex = this.index + 1;
                if (newIndex < this.list.Count)
                {
                    this.index = newIndex;
                    return true;
                }

                return false;
            }

            public TNode Current
            {
                get
                {
                    return this.list[this.index];
                }
            }

            public void Reset()
            {
                this.index = -1;
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

        // IEnumerator wrapper for Enumerator.
        private class EnumeratorImpl : IEnumerator<TNode>
        {
            private Enumerator e;

            internal EnumeratorImpl(SeparatedSyntaxList<TNode> list)
            {
                this.e = new Enumerator(list);
            }

            public TNode Current
            {
                get
                {
                    return e.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return e.Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return e.MoveNext();
            }

            public void Reset()
            {
                e.Reset();
            }
        }
    }
}