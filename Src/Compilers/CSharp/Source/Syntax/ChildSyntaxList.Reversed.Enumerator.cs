using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
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
        /// A reversed list of child SyntaxNodeOrToken structs.
        /// </summary>
        public partial struct Reversed
        {
            /// <summary>
            /// Enumerator or the Reversed list 
            /// </summary>
            public struct Enumerator : IEnumerator<SyntaxNodeOrToken>
            {
                private readonly CSharpSyntaxNode listNode;
                private int index;

                /// <summary>
                /// Initializes a new instance of the <see cref="Enumerator"/> struct.
                /// </summary>
                internal Enumerator(CSharpSyntaxNode list, int count)
                {
                    this.listNode = list;
                    this.index = count;
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
                    return --index >= 0;
                }

                /// <summary>
                /// Gets the element the enumerator instance is currently pointing to.
                /// </summary>
                public SyntaxNodeOrToken Current
                {
                    get
                    {
                        return ChildSyntaxList.ItemInternal(listNode, index);
                    }
                }

                /// <summary>
                /// Gets the element the enumerator instance is currently pointing to.
                /// </summary>
                object IEnumerator.Current
                {
                    get { return this.Current; }
                }

                /// <summary>
                /// Sets the enumerator to its initial position, which is before the first element in the collection.
                /// </summary>
                /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                void IEnumerator.Reset()
                {
                }

                /// <summary>
                /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                /// </summary>
                void IDisposable.Dispose()
                {
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
        }
    }
#endif
}