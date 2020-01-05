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
