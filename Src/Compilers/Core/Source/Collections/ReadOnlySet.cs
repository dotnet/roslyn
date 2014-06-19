using System;
using System.Linq;
using System.Collections.Generic;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Wraps an underlying set with a read only wrapper so that it can not be
    /// mutated from clients it is passed to.
    /// </summary>
    internal class ReadOnlySet<T> : ISet<T>
    {
        public static readonly ReadOnlySet<T> Empty = new ReadOnlySet<T>(SpecializedCollections.EmptySet<T>());

        private readonly ISet<T> set;

        public ReadOnlySet(ISet<T> set)
        {
            Contract.ThrowIfNull(set);
            this.set = set;
        }

        public bool Add(T item)
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
            return set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return set.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            set.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                return set.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return set.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return set.GetEnumerator();
        }
    }

    internal static class ReadOnlySet
    {
        public static ReadOnlySet<T> Create<T>(ISet<T> set)
        {
            return (set.Count > 0) ? new ReadOnlySet<T>(set) : ReadOnlySet<T>.Empty;
        }

        public static ReadOnlySet<T> Create<T>(IEnumerable<T> enumerable)
        {
            HashSet<T> result = null;
            foreach (var item in enumerable)
            {
                if (result == null)
                {
                    result = new HashSet<T>();
                }
                result.Add(item);
            }
            return (result != null) ? new ReadOnlySet<T>(result) : ReadOnlySet<T>.Empty;
        }
    }
}