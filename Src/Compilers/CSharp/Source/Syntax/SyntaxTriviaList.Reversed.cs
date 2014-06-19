using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
#if REMOVE
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

                return new EnumeratorImpl(ref list);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                if (list.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxTrivia>();
                }

                return new EnumeratorImpl(ref list);
            }

            public override bool Equals(object obj)
            {
                return obj is Reversed && Equals((Reversed)obj);
            }

            public bool Equals(Reversed other)
            {
                return this.list.Equals(other.list);
            }

            public override int GetHashCode()
            {
                return this.list.GetHashCode();
            }

            public struct Enumerator
            {
                private readonly SyntaxToken token;
                private readonly Syntax.InternalSyntax.CSharpSyntaxNode singleNodeOrList;
                private readonly int baseIndex;
                private readonly int count;

                private int index;
                private Syntax.InternalSyntax.CSharpSyntaxNode current;
                private int position;

                public Enumerator(ref SyntaxTriviaList list)
                {
                    this.token = list.token;
                    this.singleNodeOrList = list.node;
                    this.baseIndex = list.index;
                    this.count = list.Count;

                    this.index = this.count;
                    this.current = null;

                    var last = list.LastOrDefault();
                    this.position = last.Position + last.FullWidth;
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

                public override bool Equals(object obj)
                {
                    throw new NotSupportedException();
                }

                public override int GetHashCode()
                {
                    throw new NotSupportedException();
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
#endif
}
