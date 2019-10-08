// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
