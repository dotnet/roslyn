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
    public partial struct ChildSyntaxList
    {
        public partial struct Reversed : IEnumerable<SyntaxNodeOrToken>, IEquatable<Reversed>
        {
            private readonly SyntaxNode node;
            private readonly int count;

            internal Reversed(SyntaxNode node, int count)
            {
                this.node = node;
                this.count = count;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this.node, this.count);
            }

            IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
            {
                if (this.node == null)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
                }

                return new EnumeratorImpl(this.node, this.count);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                if (this.node == null)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
                }

                return new EnumeratorImpl(this.node, this.count);
            }

            public bool Equals(Reversed other)
            {
                return this.node == other.node
                    && this.count == other.count;
            }

            public struct Enumerator
            {
                private readonly SyntaxNode node;
                private readonly int count;
                private int childIndex;

                internal Enumerator(SyntaxNode node, int count)
                {
                    this.node = node;
                    this.count = count;
                    this.childIndex = count;
                }

                public bool MoveNext()
                {
                    return --childIndex >= 0;
                }

                public SyntaxNodeOrToken Current
                {
                    get
                    {
                        return ItemInternal(node, childIndex);
                    }
                }

                public void Reset()
                {
                    this.childIndex = this.count;
                }
            }

            private class EnumeratorImpl : IEnumerator<SyntaxNodeOrToken>
            {
                private Enumerator enumerator;

                internal EnumeratorImpl(SyntaxNode node, int count)
                {
                    this.enumerator = new Enumerator(node, count);
                }

                /// <summary>
                /// Gets the element in the collection at the current position of the enumerator.
                /// </summary>
                /// <returns>
                /// The element in the collection at the current position of the enumerator.
                ///   </returns>
                public SyntaxNodeOrToken Current
                {
                    get { return enumerator.Current; }
                }

                /// <summary>
                /// Gets the element in the collection at the current position of the enumerator.
                /// </summary>
                /// <returns>
                /// The element in the collection at the current position of the enumerator.
                ///   </returns>
                object IEnumerator.Current
                {
                    get { return enumerator.Current; }
                }

                /// <summary>
                /// Advances the enumerator to the next element of the collection.
                /// </summary>
                /// <returns>
                /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
                /// </returns>
                /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                public bool MoveNext()
                {
                    return enumerator.MoveNext();
                }

                /// <summary>
                /// Sets the enumerator to its initial position, which is before the first element in the collection.
                /// </summary>
                /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                public void Reset()
                {
                    enumerator.Reset();
                }

                /// <summary>
                /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                /// </summary>
                public void Dispose()
                { }
            }
        }
    }
}
