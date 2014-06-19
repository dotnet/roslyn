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
    /// <summary>
    /// A list of child SyntaxNodeOrToken structs.
    /// </summary>
    public partial struct ChildSyntaxList
    {
        /// <summary>
        /// An enumerator for the ChildSyntaxList
        /// </summary>
        public struct Enumerator
        {
            private readonly CSharpSyntaxNode node;
            private readonly int count;
            private int childIndex;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="node">The underlying syntax node.</param>
            /// <param name="count">The count of elements.</param>
            internal Enumerator(CSharpSyntaxNode node, int count)
            {
                this.node = node;
                this.count = count;
                this.childIndex = -1;
            }

            /// <summary>
            /// Moves the next.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                var newIndex = this.childIndex + 1;
                if (newIndex < this.count)
                {
                    this.childIndex = newIndex;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Gets the element this enumerator instance is pointing to.
            /// </summary>
            public SyntaxNodeOrToken Current
            {
                get
                {
                    return ItemInternal(node, this.childIndex);
                }
            }

            /// <summary>
            /// Resets this instance.
            /// </summary>
            public void Reset()
            {
                this.childIndex = -1;
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

        private class EnumeratorImpl : IEnumerator<SyntaxNodeOrToken>
        {
            private Enumerator enumerator;

            /// <summary>
            /// Initializes a new instance of the <see cref="EnumeratorImpl"/> class.
            /// </summary>
            /// <param name="node">The underlying syntax node.</param>
            /// <param name="count">The count of elements.</param>
            internal EnumeratorImpl(CSharpSyntaxNode node, int count)
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
#endif
}