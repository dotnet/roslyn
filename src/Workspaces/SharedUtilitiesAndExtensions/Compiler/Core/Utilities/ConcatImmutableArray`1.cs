// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal readonly struct ConcatImmutableArray<T> : IEnumerable<T>
    {
        private readonly ImmutableArray<T> _first;
        private readonly ImmutableArray<T> _second;

        public ConcatImmutableArray(ImmutableArray<T> first, ImmutableArray<T> second)
        {
            _first = first;
            _second = second;
        }

        public int Length => _first.Length + _second.Length;

        public bool Any(Func<T, bool> predicate)
            => _first.Any(predicate) || _second.Any(predicate);

        public Enumerator GetEnumerator()
            => new Enumerator(_first, _second);

        public ImmutableArray<T> ToImmutableArray()
            => _first.NullToEmpty().AddRange(_second.NullToEmpty());

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private ImmutableArray<T>.Enumerator _current;
            private ImmutableArray<T> _next;

            public Enumerator(ImmutableArray<T> first, ImmutableArray<T> second)
            {
                _current = first.NullToEmpty().GetEnumerator();
                _next = second.NullToEmpty();
            }

            public T Current => _current.Current;
            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_current.MoveNext())
                {
                    return true;
                }

                _current = _next.GetEnumerator();
                _next = ImmutableArray<T>.Empty;
                return _current.MoveNext();
            }

            void IDisposable.Dispose()
            {
            }

            void IEnumerator.Reset()
                => throw new NotSupportedException();
        }
    }
}
