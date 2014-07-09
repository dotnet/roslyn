// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    ///  Represents a read-only list of <see cref="SyntaxToken"/>s.
    /// </summary>
    public partial struct SyntaxTokenList
    {
        /// <summary>
        /// A structure for enumerating a <see cref="SyntaxTokenList"/>
        /// </summary>
        public struct Enumerator
        {
            // This enumerator allows us to enumerate through two types of lists.
            // either it looks like:
            //
            //   Parent
            //   |
            //   List
            //   |   \
            //   c1  c2
            //
            // or
            //
            //   Parent
            //   |
            //   c1
            //
            // I.e. in the single child case, we optimize and store the child
            // directly (without any list parent).
            //
            // Enumerating over the single child case is simple.  We just 
            // return it and we're done.
            //
            // In the multi child case, things are a bit more difficult.  We need
            // to return the children in order, while also keeping their offset
            // correct.

            private readonly SyntaxNode parent;
            private readonly GreenNode singleNodeOrList;
            private readonly int baseIndex;
            private readonly int count;

            private int index;
            private GreenNode current;
            private int position;

            internal Enumerator(ref SyntaxTokenList list)
            {
                this.parent = list.parent;
                this.singleNodeOrList = list.node;
                this.baseIndex = list.index;
                this.count = list.Count;

                this.index = -1;
                this.current = null;
                this.position = list.position;
            }

            /// <summary>
            /// Advances the enumerator to the next token in the collection.
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator
            /// has passed the end of the collection.</returns>
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
                System.Diagnostics.Debug.Assert(this.current != null);
                return true;
            }

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            public SyntaxToken Current
            {
                get
                {
                    if (this.current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    // In both the list and the single node case we want to 
                    // return the original root parent as the parent of this
                    // token.
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

            // SyntaxTriviaList is a relatively big struct so is passed by ref
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