// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
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
