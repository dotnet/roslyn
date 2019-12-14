// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class ReadOnly
        {
            internal class Set<TUnderlying, T> : Collection<TUnderlying, T>, ISet<T>, IReadOnlySet<T>
                where TUnderlying : ISet<T>
            {
                public Set(TUnderlying underlying)
                    : base(underlying)
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
                    return Underlying.IsProperSubsetOf(other);
                }

                public bool IsProperSupersetOf(IEnumerable<T> other)
                {
                    return Underlying.IsProperSupersetOf(other);
                }

                public bool IsSubsetOf(IEnumerable<T> other)
                {
                    return Underlying.IsSubsetOf(other);
                }

                public bool IsSupersetOf(IEnumerable<T> other)
                {
                    return Underlying.IsSupersetOf(other);
                }

                public bool Overlaps(IEnumerable<T> other)
                {
                    return Underlying.Overlaps(other);
                }

                public bool SetEquals(IEnumerable<T> other)
                {
                    return Underlying.SetEquals(other);
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
