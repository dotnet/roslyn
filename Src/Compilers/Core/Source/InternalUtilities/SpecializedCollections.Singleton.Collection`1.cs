// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static partial class SpecializedCollections
    {
        private static partial class Singleton
        {
            internal sealed class Collection<T> : ICollection<T>, IReadOnlyCollection<T>
            {
                private T loneValue;

                public Collection(T value)
                {
                    this.loneValue = value;
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
                    return EqualityComparer<T>.Default.Equals(loneValue, item);
                }

                public void CopyTo(T[] array, int arrayIndex)
                {
                    array[arrayIndex] = this.loneValue;
                }

                public int Count
                {
                    get { return 1; }
                }

                public bool IsReadOnly
                {
                    get { return true; }
                }

                public bool Remove(T item)
                {
                    throw new NotSupportedException();
                }

                public IEnumerator<T> GetEnumerator()
                {
                    return new Enumerator<T>(loneValue);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }
        }
    }
}
