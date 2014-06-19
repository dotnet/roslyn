using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
#if REMOVE
    /// <summary>
    /// A list of child SyntaxNodeOrToken structs.
    /// </summary>
    public partial struct ChildSyntaxList
    {
        /// <summary>
        /// A reversed list of child SyntaxNodeOrToken structs.
        /// </summary>
        public partial struct Reversed : IEnumerable<SyntaxNodeOrToken>, IEquatable<Reversed>
        {
            /// <summary>
            /// The underlying syntax node
            /// </summary>
            private readonly CSharpSyntaxNode node;

            /// <summary>
            /// The count of elements in this list
            /// </summary>
            private int count;

            /// <summary>
            /// Initializes a new instance of the <see cref="Reversed"/> struct.
            /// </summary>
            /// <param name="node">The underlying syntax node.</param>
            /// <param name="count">The count of elements in this list.</param>
            internal Reversed(CSharpSyntaxNode node, int count)
            {
                this.node = node;
                this.count = count;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns></returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(node, this.count);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
            {
                if (this.node == null)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
                }

                return this.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                if (this.node == null)
                {
                    return SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>();
                }

                return this.GetEnumerator();
            }

            public override bool Equals(object obj)
            {
                return obj is Reversed && Equals((Reversed)obj);
            }

            public bool Equals(Reversed other)
            {
                return this.node == other.node
                    && this.count == other.count;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(this.node, this.count);
            }
        }
    }
#endif
}