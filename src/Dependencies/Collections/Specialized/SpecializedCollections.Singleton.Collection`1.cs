// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static partial class SpecializedCollections
    {
        private static partial class Singleton
        {
            internal sealed class List<T> : IReadOnlyList<T>, IList<T>, IReadOnlyCollection<T>
            {
                private readonly T _loneValue;

                public List(T value)
                {
                    _loneValue = value;
                }

                public void Add(T item)
                {
                    throw new NotSupportedException();
                }

                public void Clear()
                {
                    throw new NotSupportedException();
                }

                public bool Contains(T item)
                {
                    return EqualityComparer<T>.Default.Equals(_loneValue, item);
                }

                public void CopyTo(T[] array, int arrayIndex)
                {
                    array[arrayIndex] = _loneValue;
                }

                public int Count => 1;

                public bool IsReadOnly => true;

                public bool Remove(T item)
                {
                    throw new NotSupportedException();
                }

                public IEnumerator<T> GetEnumerator()
                {
                    return new Enumerator<T>(_loneValue);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                public T this[int index]
                {
                    get
                    {
                        if (index != 0)
                        {
                            throw new IndexOutOfRangeException();
                        }

                        return _loneValue;
                    }

                    set
                    {
                        throw new NotSupportedException();
                    }
                }

                public int IndexOf(T item)
                {
                    if (Equals(_loneValue, item))
                    {
                        return 0;
                    }

                    return -1;
                }

                public void Insert(int index, T item)
                {
                    throw new NotSupportedException();
                }

                public void RemoveAt(int index)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
