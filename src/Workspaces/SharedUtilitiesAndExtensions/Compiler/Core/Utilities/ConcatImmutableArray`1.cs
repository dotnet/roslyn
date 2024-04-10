// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

internal readonly struct ConcatImmutableArray<T>(ImmutableArray<T> first, ImmutableArray<T> second) : IEnumerable<T>
{
    public int Length => first.Length + second.Length;

    public bool Any(Func<T, bool> predicate)
        => first.Any(predicate) || second.Any(predicate);

    public Enumerator GetEnumerator()
        => new(first, second);

    public ImmutableArray<T> ToImmutableArray()
        => first.NullToEmpty().AddRange(second.NullToEmpty());

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public struct Enumerator(ImmutableArray<T> first, ImmutableArray<T> second) : IEnumerator<T>
    {
        private ImmutableArray<T>.Enumerator _current = first.NullToEmpty().GetEnumerator();
        private ImmutableArray<T> _next = second.NullToEmpty();

        public T Current => _current.Current;
        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_current.MoveNext())
            {
                return true;
            }

            _current = _next.GetEnumerator();
            _next = [];
            return _current.MoveNext();
        }

        readonly void IDisposable.Dispose()
        {
        }

        void IEnumerator.Reset()
            => throw new NotSupportedException();
    }
}
