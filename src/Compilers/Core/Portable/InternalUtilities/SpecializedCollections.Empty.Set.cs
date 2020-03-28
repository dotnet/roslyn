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
            internal class Set<T> : Collection<T>, ISet<T>, IReadOnlySet<T>
            {
                public static readonly new Set<T> Instance = new Set<T>();

                protected Set()
                {
                }

                public new bool Add(T item)
                {
                    throw new NotSupportedException();
                }

                public void ExceptWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public void IntersectWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public bool IsProperSubsetOf(IEnumerable<T> other)
                {
                    return !other.IsEmpty();
                }

                public bool IsProperSupersetOf(IEnumerable<T> other)
                {
                    return false;
                }

                public bool IsSubsetOf(IEnumerable<T> other)
                {
                    return true;
                }

                public bool IsSupersetOf(IEnumerable<T> other)
                {
                    return other.IsEmpty();
                }

                public bool Overlaps(IEnumerable<T> other)
                {
                    return false;
                }

                public bool SetEquals(IEnumerable<T> other)
                {
                    return other.IsEmpty();
                }

                public void SymmetricExceptWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public void UnionWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
