// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static partial class SpecializedCollections
    {
        private static partial class Empty
        {
            internal class Collection<T> : Enumerable<T>, ICollection<T>
            {
                public static readonly ICollection<T> Instance = new Collection<T>();

                protected Collection()
                {
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
                    return false;
                }

                public void CopyTo(T[] array, int arrayIndex)
                {
                }

                public int Count => 0;

                public bool IsReadOnly => true;

                public bool Remove(T item)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
