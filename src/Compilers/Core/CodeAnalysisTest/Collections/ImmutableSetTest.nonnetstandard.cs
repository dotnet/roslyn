// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.8/src/libraries/System.Collections.Immutable/tests/ImmutableSetTest.nonnetstandard.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using Xunit;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1825 // Avoid zero-length array allocations

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public abstract partial class ImmutableSetTest : ImmutablesTestBase
    {
        [Fact]
        public void ExceptTest()
        {
            this.ExceptTestHelper(Empty<int>().Add(1).Add(3).Add(5).Add(7), 3, 7);
        }

        [Fact]
        public void SymmetricExceptTest()
        {
            this.SymmetricExceptTestHelper(Empty<int>().Add(1).Add(3).Add(5).Add(7), Enumerable.Range(0, 9).ToArray());
            this.SymmetricExceptTestHelper(Empty<int>().Add(1).Add(3).Add(5).Add(7), Enumerable.Range(0, 5).ToArray());
        }

        [Fact]
        public void EnumeratorTest()
        {
            var set = this.Empty<double>();
            IComparer<double>? comparer = GetComparer(set);

            this.EnumeratorTestHelper(set, comparer, 3, 5, 1);
            double[] data = GenerateDummyFillData();
            this.EnumeratorTestHelper(set, comparer, data);
        }

        [Fact]
        public void IntersectTest()
        {
            this.IntersectTestHelper(Empty<int>().Union(Enumerable.Range(1, 10)), 8, 3, 5);
        }

        [Fact]
        public void UnionTest()
        {
            this.UnionTestHelper(this.Empty<int>(), new[] { 1, 3, 5, 7 });
            this.UnionTestHelper(this.Empty<int>().Union(new[] { 2, 4, 6 }), new[] { 1, 3, 5, 7 });
            this.UnionTestHelper(this.Empty<int>().Union(new[] { 1, 2, 3 }), new int[0] { });
            this.UnionTestHelper(this.Empty<int>().Union(new[] { 2 }), Enumerable.Range(0, 1000).ToArray());
        }

        protected static IComparer<T> GetComparer<T>(System.Collections.Immutable.IImmutableSet<T> set)
        {
            return set switch
            {
                System.Collections.Immutable.ImmutableSortedSet<T> s => s.KeyComparer,
                _ => throw ExceptionUtilities.UnexpectedValue(set),
            };
        }

        protected static IEqualityComparer<T> GetEqualityComparer<T>(System.Collections.Immutable.IImmutableSet<T> set)
        {
            return set switch
            {
                ImmutableSegmentedHashSet<T> s => s.KeyComparer,
                System.Collections.Immutable.ImmutableHashSet<T> s => s.KeyComparer,
                _ => throw ExceptionUtilities.UnexpectedValue(set),
            };
        }

        protected void TryGetValueTestHelper(System.Collections.Immutable.IImmutableSet<string> set)
        {
            Assert.NotNull(set);

            string expected = "egg";
            set = set.Add(expected);
            string lookupValue = expected.ToUpperInvariant();
            Assert.True(set.TryGetValue(lookupValue, out string actual));
            Assert.Same(expected, actual);

            Assert.False(set.TryGetValue("foo", out actual));
            Assert.Equal("foo", actual);

            Assert.False(set.Clear().TryGetValue("nonexistent", out actual));
            Assert.Equal("nonexistent", actual);
        }

        private void ExceptTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> set, params T[] valuesToRemove)
        {
            Assert.NotNull(set);
            Assert.NotNull(valuesToRemove);

            var expectedSet = new HashSet<T>(set);
            expectedSet.ExceptWith(valuesToRemove);

            var actualSet = set.Except(valuesToRemove);
            CollectionAssertAreEquivalent(expectedSet.ToList(), actualSet.ToList());
        }

        private void SymmetricExceptTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> set, params T[] otherCollection)
        {
            Assert.NotNull(set);
            Assert.NotNull(otherCollection);

            var expectedSet = new HashSet<T>(set);
            expectedSet.SymmetricExceptWith(otherCollection);

            var actualSet = set.SymmetricExcept(otherCollection);
            CollectionAssertAreEquivalent(expectedSet.ToList(), actualSet.ToList());
        }

        private void IntersectTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> set, params T[] values)
        {
            Assert.NotNull(set);
            Assert.NotNull(values);

            Assert.True(set.Intersect(Enumerable.Empty<T>()).Count == 0);

            var expected = new HashSet<T>(set);
            expected.IntersectWith(values);

            var actual = set.Intersect(values);
            CollectionAssertAreEquivalent(expected.ToList(), actual.ToList());
        }

        private void UnionTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> set, params T[] values)
        {
            Assert.NotNull(set);
            Assert.NotNull(values);

            var expected = new HashSet<T>(set);
            expected.UnionWith(values);

            var actual = set.Union(values);
            CollectionAssertAreEquivalent(expected.ToList(), actual.ToList());
        }
    }
}
