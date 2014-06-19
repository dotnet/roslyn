using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
#if REMOVE
    partial struct SyntaxTriviaList
    {
        /// <remarks>
        /// We could implement the enumerator using the indexer of the SyntaxTriviaList, but then
        /// every node's offset would be computed separately.  Since we know that we will be accessing
        /// the nodes in order, we can maintain a running offset value ourselves and avoid this
        /// overhead.
        /// </remarks>
        public struct Enumerator
        {
            private readonly SyntaxToken token;
            private readonly Syntax.InternalSyntax.CSharpSyntaxNode singleNodeOrList;
            private readonly int baseIndex;
            private readonly int count;

            private int index;
            private Syntax.InternalSyntax.CSharpSyntaxNode current;
            private int position;

            // SyntaxTriviaList is a relatively big struct so is passed as ref
            internal Enumerator(ref SyntaxTriviaList list)
            {
                this.token = list.Token;
                this.singleNodeOrList = list.Node;
                this.baseIndex = list.index;
                this.count = list.Count;

                this.index = -1;
                this.current = null;
                this.position = list.position;
            }

            /// <summary>
            /// Move to the next item in the list.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (this.count == 0 || this.count <= index + 1)
                {
                    // invalidate iterator
                    this.current = null;
                    return false;
                }

                index++;

                // Add the length of the previous node to the offset so that
                // the next node's offset is reported correctly.
                if (current != null)
                {
                    this.position += this.current.FullWidth;
                }

                this.current = GetGreenNodeAt(this.singleNodeOrList, this.index);
                return true;
            }

            /// <summary>
            /// The SyntaxTrivia item at the current location.
            /// </summary>
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
#endif
}