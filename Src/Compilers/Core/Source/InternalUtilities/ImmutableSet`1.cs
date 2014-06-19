using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace System.Collections.Immutable
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal sealed class ImmutableHashSet<T> : IEnumerable<T>
    {
        private static readonly ImmutableHashSet<T> Empty = new ImmutableHashSet<T>(ImmutableDictionary.Create<T, object>());

        private readonly ImmutableDictionary<T, object> map;

        // Temporary hack to allow access to the creation methods until we can use the BCL version
        internal static ImmutableHashSet<T> PrivateCtor_DONOTUSE(ImmutableDictionary<T, object> map)
        {
            return new ImmutableHashSet<T>(map);
        }

        // Temporary hack to allow access to the creation methods until we can use the BCL version
        internal static ImmutableHashSet<T> PrivateEmpty_DONOTUSE()
        {
            return Empty;
        }

        private ImmutableHashSet(ImmutableDictionary<T, object> map)
        {
            Debug.Assert(map != null);
            this.map = map;
        }

        public int Count
        {
            get { return map.Count; }
        }

        public bool Contains(T value)
        {
            return map.ContainsKey(value);
        }

        public ImmutableHashSet<T> Add(T value)
        {
            // no reason to cause allocations if value is already there
            if (this.Contains(value))
            {
                return this;
            }

            return new ImmutableHashSet<T>(map.Add(value, null));
        }

        public ImmutableHashSet<T> Union(IEnumerable<T> values)
        {
            ImmutableHashSet<T> result = this;
            foreach (var v in values)
            {
                // TODO: don't allocate a new set for each step.
                result = result.Add(v);
            }

            return result;
        }

        public ImmutableHashSet<T> Remove(T value)
        {
            // no reason to cause allocations if value is missing
            if (!this.Contains(value))
            {
                return this;
            }

            return this.Count == 1 ? Empty : new ImmutableHashSet<T>(map.Remove(value));
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", this) + "}";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return map.Keys.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return map.Keys.GetEnumerator();
        }

        public ImmutableHashSet<T> Union(ImmutableHashSet<T> other)
        {
            return this.Union((IEnumerable<T>)other);
        }

        public ImmutableHashSet<T> Intersect(ImmutableHashSet<T> other)
        {
            return ImmutableHashSet.From(this.Where(other.Contains));
        }

        public ImmutableHashSet<T> Difference(ImmutableHashSet<T> other)
        {
            return ImmutableHashSet.From(this.Where(t => !other.Contains(t)));
        }

        public ImmutableHashSet<T> SymmetricDifference(ImmutableHashSet<T> other)
        {
            return this.Union(other).Difference(this.Intersect(other));
        }
    }
}