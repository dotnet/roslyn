// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            {
                throw new NotSupportedException();
            }
        }
    }
}
