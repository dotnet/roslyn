// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal class Set<T> : Collection<T>, ISet<T>
            {
                public static readonly new ISet<T> Instance = new Set<T>();

                protected Set()
                {
                }

                public new bool Add(T item)
                {
                    throw new NotImplementedException();
                }

                public void ExceptWith(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public void IntersectWith(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public bool IsProperSubsetOf(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public bool IsProperSupersetOf(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public bool IsSubsetOf(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public bool IsSupersetOf(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public bool Overlaps(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public bool SetEquals(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public void SymmetricExceptWith(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public void UnionWith(IEnumerable<T> other)
                {
                    throw new NotImplementedException();
                }

                public new System.Collections.IEnumerator GetEnumerator()
                {
                    return Set<T>.Instance.GetEnumerator();
                }
            }
        }
    }
}
