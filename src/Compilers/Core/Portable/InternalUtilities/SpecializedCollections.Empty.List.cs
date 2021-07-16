// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal static class BoxedImmutableArray<T>
            {
                // empty boxed immutable array
                public static readonly IReadOnlyList<T> Instance = ImmutableArray<T>.Empty;
            }

            internal class List<T> : Collection<T>, IList<T>, IReadOnlyList<T>
            {
                public static new readonly List<T> Instance = new();

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
