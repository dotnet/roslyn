// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public class SetAbstractDomain<T> : AbstractDomain<ImmutableHashSet<T>>
    {
        private SetAbstractDomain() { }

        public static SetAbstractDomain<T> Default { get; } = new SetAbstractDomain<T>();

        public override ImmutableHashSet<T> Bottom => ImmutableHashSet<T>.Empty;

        public override int Compare(ImmutableHashSet<T> oldValue, ImmutableHashSet<T> newValue, bool assertMonotonicity)
        {
            if (ReferenceEquals(oldValue, newValue))
            {
                return 0;
            }

            int result;
            // PERF: Avoid additional hash set allocation by using overload which takes
            // a set argument instead of IEnumerable argument.
            var isSubset = oldValue.IsSubsetOfSet(newValue);

            if (isSubset &&
                oldValue.Count == newValue.Count)
            {
                // oldValue == newValue
                result = 0;
            }
            else if (isSubset)
            {
                // oldValue < newValue
                result = -1;
            }
            else
            {
                // oldValue > newValue
                result = 1;
            }

            return result;
        }

        public override ImmutableHashSet<T> Merge(ImmutableHashSet<T> value1, ImmutableHashSet<T> value2) => MergeOrIntersect(value1, value2, merge: true);

        public ImmutableHashSet<T> Intersect(ImmutableHashSet<T> value1, ImmutableHashSet<T> value2) => MergeOrIntersect(value1, value2, merge: false);

        private static ImmutableHashSet<T> MergeOrIntersect(ImmutableHashSet<T> value1, ImmutableHashSet<T> value2, bool merge)
        {
            if (value1.IsEmpty)
            {
                return merge ? value2 : value1;
            }
            else if (value2.IsEmpty || ReferenceEquals(value1, value2))
            {
                return merge ? value1 : value2;
            }

            // PERF: Avoid additional allocations by using the overload that takes a set argument
            // instead of IEnumerable argument.
            return merge ? ImmutableHashSetExtensions.AddRange(value1, value2) : value1.IntersectSet(value2);
        }
    }
}
