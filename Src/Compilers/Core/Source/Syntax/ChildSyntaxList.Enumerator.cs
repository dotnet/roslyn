// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public partial struct ChildSyntaxList
    {
        /// <summary>Enumerates the elements of a <see cref="ChildSyntaxList" />.</summary>
        public struct Enumerator
        {
            private SyntaxNode node;
            private int count;
            private int childIndex;

            internal Enumerator(SyntaxNode node, int count)
            {
                this.node = node;
                this.count = count;
                this.childIndex = -1;
            }

            // PERF: Initialize an Enumerator directly from a SyntaxNode without going
            // via ChildNodesAndTokens. This saves constructing an intermediate ChildSyntaxList
            internal void InitializeFrom(SyntaxNode node)
            {
                this.node = node;
                this.count = CountNodes(node.Green);
                this.childIndex = -1;
            }

            /// <summary>Advances the enumerator to the next element of the <see cref="ChildSyntaxList" />.</summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
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

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            /// <returns>The element in the <see cref="ChildSyntaxList" /> at the current position of the enumerator.</returns>
            public SyntaxNodeOrToken Current
            {
                get
                {
                    return ItemInternal(node, this.childIndex);
                }
            }

            /// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
            public void Reset()
            {
                this.childIndex = -1;
            }

            internal bool TryMoveNextAndGetCurrent(ref SyntaxNodeOrToken current)
            {
                if (!MoveNext())
                {
                    return false;
                }

                current = ItemInternal(node, this.childIndex);
                return true;
            }

            internal SyntaxNode TryMoveNextAndGetCurrentAsNode()
            {
                while (MoveNext())
                {
                    var nodeValue = ItemInternalAsNode(node, this.childIndex);
                    if (nodeValue != null)
                    {
                        return nodeValue;
                    }
                }

                return null;
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