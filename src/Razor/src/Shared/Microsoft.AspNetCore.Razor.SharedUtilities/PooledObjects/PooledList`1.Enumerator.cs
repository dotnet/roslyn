// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal ref partial struct PooledList<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly List<T>? _list;
        private int _index;
        private T? _current;

        public Enumerator(List<T> list)
        {
            _list = list;
            _index = 0;
            _current = default;
        }

        public T Current => _current!;

        object? IEnumerator.Current => Current;

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_list is { } list && _index < list.Count)
            {
                _current = list[_index];
                _index++;
                return true;
            }

            return false;
        }

        void IEnumerator.Reset()
        {
            _index = 0;
            _current = default;
        }
    }
}
