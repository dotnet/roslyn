using System.Collections.Generic;

namespace Roslyn.Utilities
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class IImmutableSetExtensions
    {
        public static IImmutableSet<T> Union<T>(this IImmutableSet<T> iset, IEnumerable<T> other)
        {
            var otherSet = other as IImmutableSet<T> ?? ImmutableSet<T>.Empty.AddAll(other);
            return iset.Union(otherSet);
        }

        public static IImmutableSet<T> Intersect<T>(this IImmutableSet<T> iset, IEnumerable<T> other)
        {
            var otherSet = other as IImmutableSet<T> ?? ImmutableSet<T>.Empty.AddAll(other);
            return iset.Intersect(otherSet);
        }

        public static IImmutableSet<T> Difference<T>(this IImmutableSet<T> iset, IEnumerable<T> other)
        {
            var otherSet = other as IImmutableSet<T> ?? ImmutableSet<T>.Empty.AddAll(other);
            return iset.Difference(otherSet);
        }

        public static IImmutableSet<T> SymmetricDifference<T>(this IImmutableSet<T> iset, IEnumerable<T> other)
        {
            var otherSet = other as IImmutableSet<T> ?? ImmutableSet<T>.Empty.AddAll(other);
            return iset.SymmetricDifference(otherSet);
        }
    }
}