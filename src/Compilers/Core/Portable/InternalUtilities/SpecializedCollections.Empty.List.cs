﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
