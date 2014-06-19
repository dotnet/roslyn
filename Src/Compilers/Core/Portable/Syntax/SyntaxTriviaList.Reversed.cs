// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial struct SyntaxTriviaList
    {
        /// <summary>
        /// reversed enumerable
        /// </summary>
        public struct Reversed : IEnumerable<SyntaxTrivia>, IEquatable<Reversed>
        {
            private SyntaxTriviaList list;

            public Reversed(SyntaxTriviaList list)
            {
                this.list = list;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(ref list);
            }

            IEnumerator<SyntaxTrivia> IEnumerable<SyntaxTrivia>.GetEnumerator()
            {
                if (list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
                }

                return new ReversedEnumeratorImpl(ref list);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                if (list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
                }

                return new ReversedEnumeratorImpl(ref list);
            }

            public bool Equals(Reversed other)
            {
                return this.list.Equals(other.list);
            }

            public struct Enumerator
            {
                private readonly SyntaxToken token;
                private readonly GreenNode singleNodeOrList;
                private readonly int baseIndex;
                private readonly int count;

                private int index;
                private GreenNode current;
                private int position;

                public Enumerator(ref SyntaxTriviaList list)
                    : this()
                {
                    if (list.Any())
                    {
                        this.token = list.token;
                        this.singleNodeOrList = list.node;
                        this.baseIndex = list.index;
                        this.count = list.Count;

                        this.index = this.count;
                        this.current = null;

                        var last = list.Last();
                        this.position = last.Position + last.FullWidth;
                    }
                }

                public bool MoveNext()
                {
                    if (this.count == 0 || index <= 0)
                    {
                        this.current = null;
                        return false;
                    }

                    index--;

                    this.current = GetGreenNodeAt(this.singleNodeOrList, this.index);
                    this.position -= this.current.FullWidth;

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

            private class ReversedEnumeratorImpl : IEnumerator<SyntaxTrivia>
            {
                private Enumerator enumerator;

                // SyntaxTriviaList is a relatively big struct so is passed as ref
                internal ReversedEnumeratorImpl(ref SyntaxTriviaList list)
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
}