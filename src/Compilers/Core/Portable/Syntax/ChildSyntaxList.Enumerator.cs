// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    public partial struct ChildSyntaxList
    {
        /// <summary>Enumerates the elements of a <see cref="ChildSyntaxList" />.</summary>
        public struct Enumerator
        {
            private SyntaxNode? _node;
            private int _count;
            private int _childIndex;
            private SlotData _slotData;

            internal Enumerator(SyntaxNode node, int count)
            {
                _node = node;
                _count = count;
                _childIndex = -1;
                _slotData = new SlotData(node);
            }

            internal Enumerator(SyntaxNode node, int count, int initialIndex)
            {
                _node = node;
                _count = count;
                _childIndex = initialIndex;
            }

            // PERF: Initialize an Enumerator directly from a SyntaxNode without going
            // via ChildNodesAndTokens. This saves constructing an intermediate ChildSyntaxList
            internal void InitializeFrom(SyntaxNode node)
            {
                _node = node;
                _count = CountNodes(node.Green);
                _childIndex = -1;
                _slotData = new SlotData(node);
            }

            /// <summary>Advances the enumerator to the next element of the <see cref="ChildSyntaxList" />.</summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
            [MemberNotNullWhen(true, nameof(_node))]
            public bool MoveNext()
            {
                var newIndex = _childIndex + 1;
                if (unchecked((uint)newIndex < (uint)_count))
                {
                    _childIndex = newIndex;
                    Debug.Assert(_node != null);
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
                    Debug.Assert(_node is object);
                    return ItemInternal(_node, _childIndex, ref _slotData);
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

                current = ItemInternal(_node, _childIndex, ref _slotData);
                return true;
            }

            internal SyntaxNode? TryMoveNextAndGetCurrentAsNode()
            {
                while (MoveNext())
                {
                    var nodeValue = ItemInternalAsNode(_node, _childIndex, ref _slotData);
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
