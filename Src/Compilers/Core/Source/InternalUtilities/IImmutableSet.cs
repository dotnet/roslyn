using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal interface IImmutableSet<T> : IReadOnlyCollection<T>
    {
        bool Contains(T value);

        IImmutableSet<T> Add(T value);
        IImmutableSet<T> Remove(T value);

        IImmutableSet<T> AddAll(IEnumerable<T> values);

        IImmutableSet<T> Union(IImmutableSet<T> other);

        IImmutableSet<T> Intersect(IImmutableSet<T> other);

        IImmutableSet<T> Difference(IImmutableSet<T> other);

        IImmutableSet<T> SymmetricDifference(IImmutableSet<T> other);

        IEnumerable<T> InOrder { get; }
    }
}