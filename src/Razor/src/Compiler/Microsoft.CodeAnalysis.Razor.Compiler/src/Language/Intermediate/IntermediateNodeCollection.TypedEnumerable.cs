// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed partial class IntermediateNodeCollection
{
    /// <summary>
    /// A deferred, ordered view of children assignable to <typeparamref name="TNode"/>.
    /// </summary>
    /// <remarks>
    /// Keep this concrete type when enumerating to avoid boxing the view and its enumerator.
    /// </remarks>
    public readonly struct TypedEnumerable<TNode> : IEnumerable<TNode>
        where TNode : IntermediateNode
    {
        private readonly IntermediateNodeCollection _collection;

        internal TypedEnumerable(IntermediateNodeCollection collection)
        {
            _collection = collection;
        }

        public Enumerator GetEnumerator() => new(_collection);

        IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<TNode>
        {
            private IntermediateNodeCollection.Enumerator _inner;
            private TNode? _current;
            private bool _completed;

            internal Enumerator(IntermediateNodeCollection collection)
            {
                _inner = collection.GetEnumerator();
            }

            public readonly TNode Current => _current!;

            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!_completed)
                {
                    while (_inner.MoveNext())
                    {
                        if (_inner.Current is TNode node)
                        {
                            _current = node;
                            return true;
                        }
                    }

                    Dispose();
                }

                return false;
            }

            public void Dispose()
            {
                _inner.Dispose();
                _current = null;
                // Once exhausted or disposed, do not resume if more children are added.
                _completed = true;
            }

            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
