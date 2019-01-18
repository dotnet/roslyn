// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private SyntaxNode _node;
            private int _count;
            private int _childIndex;

            internal Enumerator(SyntaxNode node, int count)
            {
                _node = node;
                _count = count;
                _childIndex = -1;
            }

            // PERF: Initialize an Enumerator directly from a SyntaxNode without going
            // via ChildNodesAndTokens. This saves constructing an intermediate ChildSyntaxList
            internal void InitializeFrom(SyntaxNode node)
            {
                _node = node;
                _count = CountNodes(node.Green);
                _childIndex = -1;
            }

            /// <summary>Advances the enumerator to the next element of the <see cref="ChildSyntaxList" />.</summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                var newIndex = _childIndex + 1;
                if (newIndex < _count)
                {
                    _childIndex = newIndex;
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
                    return ItemInternal(_node, _childIndex);
                }
            }

            /// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
            public void Reset()
            {
                _childIndex = -1;
            }

            internal bool TryMoveNextAndGetCurrent(out SyntaxNodeOrToken current)
            {
                if (!MoveNext())
                {
                    current = default;
                    return false;
                }

                current = ItemInternal(_node, _childIndex);
                return true;
            }

            internal SyntaxNode TryMoveNextAndGetCurrentAsNode()
            {
                while (MoveNext())
                {
                    var nodeValue = ItemInternalAsNode(_node, _childIndex);
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
            private Enumerator _enumerator;

            internal EnumeratorImpl(SyntaxNode node, int count)
            {
                _enumerator = new Enumerator(node, count);
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            ///   </returns>
            public SyntaxNodeOrToken Current
            {
                get { return _enumerator.Current; }
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            ///   </returns>
            object IEnumerator.Current
            {
                get { return _enumerator.Current; }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                _enumerator.Reset();
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            { }
        }
    }
}
