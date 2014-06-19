using System.Collections.Generic;
using System.Linq;

namespace Roslyn.Utilities
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal abstract class AbstractImmutableSet<T> : AbstractEnumerable<T>, IImmutableSet<T>
    {
        internal abstract IEqualityComparer<T> ValueEqualityComparer { get; }

        public abstract int Count { get; }

        public abstract bool Contains(T value);

        public abstract IImmutableSet<T> Add(T value);

        public abstract IImmutableSet<T> Remove(T value);

        public abstract IEnumerable<T> InOrder { get; }

        public IImmutableSet<T> AddAll(IEnumerable<T> values)
        {
            IImmutableSet<T> result = this;
            foreach (var v in values)
            {
                result = result.Add(v);
            }

            return result;
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", this) + "}";
        }

        public override int GetHashCode()
        {
            // can't use Enumerable.Sum here as it uses a 'checked' block.
            var result = 0;
            foreach (var v in this)
            {
                result += v.GetHashCode();
            }

            return result;
        }

        public IImmutableSet<T> Union(IImmutableSet<T> other)
        {
            return this.AddAll(other);
        }

        public IImmutableSet<T> Intersect(IImmutableSet<T> other)
        {
            return new ImmutableSet<T>(this.ValueEqualityComparer).AddAll(this.Where(other.Contains));
        }

        public IImmutableSet<T> Difference(IImmutableSet<T> other)
        {
            return new ImmutableSet<T>(this.ValueEqualityComparer).AddAll(this.Where(t => !other.Contains(t)));
        }

        public IImmutableSet<T> SymmetricDifference(IImmutableSet<T> other)
        {
            return this.Union(other).Difference(this.Intersect(other));
        }
    }
}