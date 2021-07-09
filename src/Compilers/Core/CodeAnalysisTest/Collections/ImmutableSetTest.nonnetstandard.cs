// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using SetTriad = System.Tuple<System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>, bool>;

namespace System.Collections.Immutable.Tests
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
            IComparer<double> comparer = null;
            var set = this.Empty<double>();
            var sortedSet = set as ISortKeyCollection<double>;
            if (sortedSet != null)
            {
                comparer = sortedSet.KeyComparer;
            }

            this.EnumeratorTestHelper(set, comparer, 3, 5, 1);
            double[] data = this.GenerateDummyFillData();
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

        internal abstract IBinaryTree GetRootNode<T>(IImmutableSet<T> set);

        protected void TryGetValueTestHelper(IImmutableSet<string> set)
        {
            Requires.NotNull(set, nameof(set));

            string expected = "egg";
            set = set.Add(expected);
            string actual;
            string lookupValue = expected.ToUpperInvariant();
            Assert.True(set.TryGetValue(lookupValue, out actual));
            Assert.Same(expected, actual);

            Assert.False(set.TryGetValue("foo", out actual));
            Assert.Equal("foo", actual);

            Assert.False(set.Clear().TryGetValue("nonexistent", out actual));
            Assert.Equal("nonexistent", actual);
        }

        private void ExceptTestHelper<T>(IImmutableSet<T> set, params T[] valuesToRemove)
        {
            Assert.NotNull(set);
            Assert.NotNull(valuesToRemove);

            var expectedSet = new HashSet<T>(set);
            expectedSet.ExceptWith(valuesToRemove);

            var actualSet = set.Except(valuesToRemove);
            CollectionAssertAreEquivalent(expectedSet.ToList(), actualSet.ToList());

            this.VerifyAvlTreeState(actualSet);
        }

        private void SymmetricExceptTestHelper<T>(IImmutableSet<T> set, params T[] otherCollection)
        {
            Assert.NotNull(set);
            Assert.NotNull(otherCollection);

            var expectedSet = new HashSet<T>(set);
            expectedSet.SymmetricExceptWith(otherCollection);

            var actualSet = set.SymmetricExcept(otherCollection);
            CollectionAssertAreEquivalent(expectedSet.ToList(), actualSet.ToList());

            this.VerifyAvlTreeState(actualSet);
        }

        private void IntersectTestHelper<T>(IImmutableSet<T> set, params T[] values)
        {
            Assert.NotNull(set);
            Assert.NotNull(values);

            Assert.True(set.Intersect(Enumerable.Empty<T>()).Count == 0);

            var expected = new HashSet<T>(set);
            expected.IntersectWith(values);

            var actual = set.Intersect(values);
            CollectionAssertAreEquivalent(expected.ToList(), actual.ToList());

            this.VerifyAvlTreeState(actual);
        }

        private void UnionTestHelper<T>(IImmutableSet<T> set, params T[] values)
        {
            Assert.NotNull(set);
            Assert.NotNull(values);

            var expected = new HashSet<T>(set);
            expected.UnionWith(values);

            var actual = set.Union(values);
            CollectionAssertAreEquivalent(expected.ToList(), actual.ToList());

            this.VerifyAvlTreeState(actual);
        }

        private void VerifyAvlTreeState<T>(IImmutableSet<T> set)
        {
            var rootNode = this.GetRootNode(set);
            rootNode.VerifyBalanced();
            rootNode.VerifyHeightIsWithinTolerance(set.Count);
        }
    }
}
