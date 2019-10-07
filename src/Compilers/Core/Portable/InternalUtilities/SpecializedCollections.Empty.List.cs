// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal class List<T> : Collection<T>, IList<T>, IReadOnlyList<T>
            {
                public static readonly new List<T> Instance = new List<T>();

                protected List()
                {
                }

                public int IndexOf(T item)
                {
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

                public T this[int index]
                {
                    get
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    set
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }
    }
}
