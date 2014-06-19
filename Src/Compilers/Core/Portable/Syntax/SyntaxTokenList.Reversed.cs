// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public partial struct SyntaxTokenList
    {
        /// <summary>
        /// reversed enumerable
        /// </summary>
        public struct Reversed : IEnumerable<SyntaxToken>, IEquatable<Reversed>
        {
            private SyntaxTokenList list;

            public Reversed(SyntaxTokenList list)
            {
                this.list = list;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(ref list);
            }

            IEnumerator<SyntaxToken> IEnumerable<SyntaxToken>.GetEnumerator()
            {
                if (list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
                }

                return new EnumeratorImpl(ref list);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                if (list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
                }

                return new EnumeratorImpl(ref list);
            }

            public override bool Equals(object obj)
            {
                return obj is Reversed && Equals((Reversed)obj);
            }

            public bool Equals(Reversed other)
            {
                return list.Equals(other.list);
            }

            public override int GetHashCode()
            {
                return list.GetHashCode();
            }

            public struct Enumerator
            {
                private readonly SyntaxNode parent;
                private readonly GreenNode singleNodeOrList;
                private readonly int baseIndex;
                private readonly int count;

                private int index;
                private GreenNode current;
                private int position;

                public Enumerator(ref SyntaxTokenList list)
                    : this()
                {
                    if (list.Any())
                    {
                        this.parent = list.parent;
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

                public SyntaxToken Current
                {
                    get
                    {
                        if (this.current == null)
                        {
                            throw new InvalidOperationException();
                        }

                        return new SyntaxToken(this.parent, this.current, this.position, this.baseIndex + this.index);
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
                private Enumerator enumerator;

                // SyntaxTriviaList is a relatively big struct so is passed as ref
                internal EnumeratorImpl(ref SyntaxTokenList list)
                {
                    this.enumerator = new Enumerator(ref list);
                }

                public SyntaxToken Current
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
