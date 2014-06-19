using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal class ImmutableSingletonSet<T> : AbstractImmutableSet<T>
    {
        private readonly T value;

        public ImmutableSingletonSet(T value)
        {
            this.value = value;
        }

        internal override IEqualityComparer<T> ValueEqualityComparer
        {
            get
            {
                return EqualityComparer<T>.Default;
            }
        }

        public override int Count
        {
            get
            {
                return 1;
            }
        }

        public override bool Contains(T value)
        {
            return this.ValueEqualityComparer.Equals(this.value, value);
        }

        public override IImmutableSet<T> Add(T value)
        {
            return ImmutableSet<T>.Empty.Add(this.value).Add(value);
        }

        public override IImmutableSet<T> Remove(T value)
        {
            if (this.ValueEqualityComparer.Equals(this.value, value))
            {
                return ImmutableSet<T>.Empty;
            }
            else
            {
                return this;
            }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            yield return value;
        }

        public override IEnumerable<T> InOrder
        {
            get
            {
                yield return value;
            }
        }
    }
}