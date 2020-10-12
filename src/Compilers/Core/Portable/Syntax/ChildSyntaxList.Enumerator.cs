﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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
            // MemberNotNullWhen(true, nameof(_node)) https://github.com/dotnet/roslyn/issues/41964
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
                    Debug.Assert(_node is object);
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

                // node! can be removed when we enable MemberNotNull https://github.com/dotnet/roslyn/issues/41964
                current = ItemInternal(_node!, _childIndex);
                return true;
            }

            internal SyntaxNode? TryMoveNextAndGetCurrentAsNode()
            {
                while (MoveNext())
                {
                    // node! can be removed when we enable MemberNotNull https://github.com/dotnet/roslyn/issues/41964
                    var nodeValue = ItemInternalAsNode(_node!, _childIndex);
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
