// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.8/src/libraries/System.Collections.Immutable/tests/ImmutableSetTest.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SetTriad = System.Tuple<System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>, bool>;

#pragma warning disable CA1829 // Use Length/Count property instead of Count() when available
#pragma warning disable RS0002 // Use 'SpecializedCollections.SingletonEnumerable()'

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public abstract partial class ImmutableSetTest : ImmutablesTestBase
    {
        [Fact]
        public void AddTest()
        {
            AddTestHelper(this.Empty<int>(), 3, 5, 4, 3);
        }

        [Fact]
        public void AddDuplicatesTest()
        {
            var arrayWithDuplicates = Enumerable.Range(1, 100).Concat(Enumerable.Range(1, 100)).ToArray();
            AddTestHelper(this.Empty<int>(), arrayWithDuplicates);
        }

        [Fact]
        public void RemoveTest()
        {
            RemoveTestHelper(this.Empty<int>().Add(3).Add(5), 5, 3);
        }

        [Fact]
        public void AddRemoveLoadTest()
        {
            var data = GenerateDummyFillData();
            AddRemoveLoadTestHelper(Empty<double>(), data);
        }

        [Fact]
        public void RemoveNonExistingTest()
        {
            System.Collections.Immutable.IImmutableSet<int> emptySet = this.Empty<int>();
            Assert.True(IsSame(emptySet, emptySet.Remove(5)));

            // Also fill up a set with many elements to build up the tree, then remove from various places in the tree.
            const int Size = 200;
            var set = emptySet;
            for (int i = 0; i < Size; i += 2)
            { // only even numbers!
                set = set.Add(i);
            }

            // Verify that removing odd numbers doesn't change anything.
            for (int i = 1; i < Size; i += 2)
            {
                var setAfterRemoval = set.Remove(i);
                Assert.True(IsSame(set, setAfterRemoval));
            }
        }

        [Fact]
        public void AddBulkFromImmutableToEmpty()
        {
            var set = this.Empty<int>().Add(5);
            var empty2 = this.Empty<int>();
            Assert.True(IsSame(set, empty2.Union(set))); // "Filling an empty immutable set with the contents of another immutable set with the exact same comparer should return the other set."
        }

        /// <summary>
        /// Verifies that Except *does* enumerate its argument if the collection is empty.
        /// </summary>
        /// <remarks>
        /// While this would seem an implementation detail and simply lack of an optimization,
        /// it turns out that changing this behavior now *could* represent a breaking change
        /// because if the enumerable were to throw an exception, that exception would be
        /// observed previously, but would no longer be thrown if this behavior changed.
        /// So this is a test to lock the behavior in place or be thoughtful if adding the optimization.
        /// </remarks>
        /// <!--<seealso cref="ImmutableListTest.RemoveRangeDoesNotEnumerateSequenceIfThisIsEmpty"/>-->
        [Fact]
        public void ExceptDoesEnumerateSequenceIfThisIsEmpty()
        {
            bool enumerated = false;
            Empty<int>().Except(Enumerable.Range(1, 1).Select(n => { enumerated = true; return n; }));
            Assert.True(enumerated);
        }

        [Fact]
        public void SetEqualsTest()
        {
            Assert.True(this.Empty<int>().SetEquals(this.Empty<int>()));
            var nonEmptySet = this.Empty<int>().Add(5);
            Assert.True(nonEmptySet.SetEquals(nonEmptySet));

            this.SetCompareTestHelper(s => s.SetEquals, s => s.SetEquals, this.GetSetEqualsScenarios());
        }

        [Fact]
        public void IsProperSubsetOfTest()
        {
            this.SetCompareTestHelper(s => s.IsProperSubsetOf, s => s.IsProperSubsetOf, GetIsProperSubsetOfScenarios());
        }

        [Fact]
        public void IsProperSupersetOfTest()
        {
            this.SetCompareTestHelper(s => s.IsProperSupersetOf, s => s.IsProperSupersetOf, GetIsProperSubsetOfScenarios().Select(Flip));
        }

        [Fact]
        public void IsSubsetOfTest()
        {
            this.SetCompareTestHelper(s => s.IsSubsetOf, s => s.IsSubsetOf, GetIsSubsetOfScenarios());
        }

        [Fact]
        public void IsSupersetOfTest()
        {
            this.SetCompareTestHelper(s => s.IsSupersetOf, s => s.IsSupersetOf, GetIsSubsetOfScenarios().Select(Flip));
        }

        [Fact]
        public void OverlapsTest()
        {
            this.SetCompareTestHelper(s => s.Overlaps, s => s.Overlaps, GetOverlapsScenarios());
        }

        [Fact]
        public void EqualsTest()
        {
            Assert.False(Empty<int>().Equals(null));
            Assert.False(Empty<int>().Equals("hi"));
            Assert.True(Empty<int>().Equals(Empty<int>()));
            Assert.False(Empty<int>().Add(3).Equals(Empty<int>().Add(3)));
            Assert.False(Empty<int>().Add(5).Equals(Empty<int>().Add(3)));
            Assert.False(Empty<int>().Add(3).Add(5).Equals(Empty<int>().Add(3)));
            Assert.False(Empty<int>().Add(3).Equals(Empty<int>().Add(3).Add(5)));
        }

        [Fact]
        public void GetHashCodeTest()
        {
            // verify that get hash code is the default address based one.
            Assert.Equal(EqualityComparer<object>.Default.GetHashCode(Empty<int>()), Empty<int>().GetHashCode());
        }

        [Fact]
        public void ClearTest()
        {
            var originalSet = this.Empty<int>();
            var nonEmptySet = originalSet.Add(5);
            var clearedSet = nonEmptySet.Clear();
            Assert.True(IsSame(originalSet, clearedSet));
        }

        [Fact]
        public void ISetMutationMethods()
        {
            var set = (ISet<int>)this.Empty<int>();
            Assert.Throws<NotSupportedException>(() => set.Add(0));
            Assert.Throws<NotSupportedException>(() => set.ExceptWith(null!));
            Assert.Throws<NotSupportedException>(() => set.UnionWith(null!));
            Assert.Throws<NotSupportedException>(() => set.IntersectWith(null!));
            Assert.Throws<NotSupportedException>(() => set.SymmetricExceptWith(null!));
        }

        [Fact]
        public void ICollectionOfTMembers()
        {
            var set = (ICollection<int>)this.Empty<int>();
            Assert.Throws<NotSupportedException>(() => set.Add(1));
            Assert.Throws<NotSupportedException>(() => set.Clear());
            Assert.Throws<NotSupportedException>(() => set.Remove(1));
            Assert.True(set.IsReadOnly);
        }

        [Fact]
        public void ICollectionMethods()
        {
            ICollection builder = (ICollection)this.Empty<string>();
            string[] array = Array.Empty<string>();
            builder.CopyTo(array, 0);

            builder = (ICollection)this.Empty<string>().Add("a");
            array = new string[builder.Count + 1];

            builder.CopyTo(array, 1);
            Assert.Equal(new[] { null, "a" }, array);

            Assert.True(builder.IsSynchronized);
            Assert.NotNull(builder.SyncRoot);
            Assert.Same(builder.SyncRoot, builder.SyncRoot);
        }

        [Fact]
        public void NullHandling()
        {
            var empty = this.Empty<string?>();
            var set = empty.Add(null);
            Assert.True(set.Contains(null));
            Assert.True(set.TryGetValue(null, out var @null));
            Assert.Null(@null);
            Assert.Equal(empty, set.Remove(null));

            set = empty.Union(new[] { null, "a" });
            Assert.True(set.IsSupersetOf(new[] { null, "a" }));
            Assert.True(set.IsSubsetOf(new[] { null, "a" }));
            Assert.True(set.IsProperSupersetOf(new[] { (string?)null }));
            Assert.True(set.IsProperSubsetOf(new[] { null, "a", "b" }));
            Assert.True(set.Overlaps(new[] { null, "b" }));
            Assert.True(set.SetEquals(new[] { null, null, "a", "a" }));

            set = set.Intersect(new[] { (string?)null });
            Assert.Equal(1, set.Count);

            set = set.Except(new[] { (string?)null });
            Assert.False(set.Contains(null));
        }

        protected abstract bool IncludesGetHashCodeDerivative { get; }

        internal static List<T> ToListNonGeneric<T>(System.Collections.IEnumerable sequence)
        {
            Assert.NotNull(sequence);

            var list = new List<T>();
            var enumerator = sequence.GetEnumerator();
            while (enumerator.MoveNext())
            {
                list.Add((T)enumerator.Current);
            }

            return list;
        }

        protected abstract System.Collections.Immutable.IImmutableSet<T> Empty<T>();

        protected abstract ISet<T> EmptyMutable<T>();

        protected System.Collections.Immutable.IImmutableSet<T> SetWith<T>(params T[] items)
        {
            return this.Empty<T>().Union(items);
        }

        protected static void CustomSortTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> emptySet, bool matchOrder, T[] injectedValues, T[] expectedValues)
        {
            Assert.NotNull(emptySet);
            Assert.NotNull(injectedValues);
            Assert.NotNull(expectedValues);

            var set = emptySet;
            foreach (T value in injectedValues)
            {
                set = set.Add(value);
            }

            Assert.Equal(expectedValues.Length, set.Count);
            if (matchOrder)
            {
                Assert.Equal<T>(expectedValues, set.ToList());
            }
            else
            {
                CollectionAssertAreEquivalent(expectedValues, set.ToList());
            }
        }

        /// <summary>
        /// Tests various aspects of a set.  This should be called only from the unordered or sorted overloads of this method.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the set.</typeparam>
        /// <param name="emptySet">The empty set.</param>
        protected static void EmptyTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> emptySet)
        {
            Assert.NotNull(emptySet);

            Assert.Equal(0, emptySet.Count); //, "Empty set should have a Count of 0");
            Assert.Equal(0, emptySet.Count()); //, "Enumeration of an empty set yielded elements.");
            Assert.True(IsSame(emptySet, emptySet.Clear()));
        }

        private IEnumerable<SetTriad> GetSetEqualsScenarios()
        {
            return new List<SetTriad>
            {
                new SetTriad(SetWith<int>(), Array.Empty<int>(), true),
                new SetTriad(SetWith<int>(5), new int[] { 5 }, true),
                new SetTriad(SetWith<int>(5), new int[] { 5, 5 }, true),
                new SetTriad(SetWith<int>(5, 8), new int[] { 5, 5 }, false),
                new SetTriad(SetWith<int>(5, 8), new int[] { 5, 7 }, false),
                new SetTriad(SetWith<int>(5, 8), new int[] { 5, 8 }, true),
                new SetTriad(SetWith<int>(5), Array.Empty<int>(), false),
                new SetTriad(SetWith<int>(), new int[] { 5 }, false),
                new SetTriad(SetWith<int>(5, 8), new int[] { 5 }, false),
                new SetTriad(SetWith<int>(5), new int[] { 5, 8 }, false),
                new SetTriad(SetWith<int>(5, 8), SetWith<int>(5, 8), true),
            };
        }

        private static IEnumerable<SetTriad> GetIsProperSubsetOfScenarios()
        {
            return new List<SetTriad>
            {
                new SetTriad(Array.Empty<int>(), Array.Empty<int>(), false),
                new SetTriad(new int[] { 1 }, Array.Empty<int>(), false),
                new SetTriad(new int[] { 1 }, new int[] { 2 }, false),
                new SetTriad(new int[] { 1 }, new int[] { 2, 3 }, false),
                new SetTriad(new int[] { 1 }, new int[] { 1, 2 }, true),
                new SetTriad(Array.Empty<int>(), new int[] { 1 }, true),
            };
        }

        private static IEnumerable<SetTriad> GetIsSubsetOfScenarios()
        {
            var results = new List<SetTriad>
            {
                new SetTriad(Array.Empty<int>(), Array.Empty<int>(), true),
                new SetTriad(new int[] { 1 }, new int[] { 1 }, true),
                new SetTriad(new int[] { 1, 2 }, new int[] { 1, 2 }, true),
                new SetTriad(new int[] { 1 }, Array.Empty<int>(), false),
                new SetTriad(new int[] { 1 }, new int[] { 2 }, false),
                new SetTriad(new int[] { 1 }, new int[] { 2, 3 }, false),
            };

            // By definition, any proper subset is also a subset.
            // But because a subset may not be a proper subset, we filter the proper- scenarios.
            results.AddRange(GetIsProperSubsetOfScenarios().Where(s => s.Item3));
            return results;
        }

        private static IEnumerable<SetTriad> GetOverlapsScenarios()
        {
            return new List<SetTriad>
            {
                new SetTriad(Array.Empty<int>(), Array.Empty<int>(), false),
                new SetTriad(Array.Empty<int>(), new int[] { 1 }, false),
                new SetTriad(new int[] { 1 }, new int[] { 2 }, false),
                new SetTriad(new int[] { 1 }, new int[] { 2, 3 }, false),
                new SetTriad(new int[] { 1, 2 }, new int[] { 3 }, false),
                new SetTriad(new int[] { 1 }, new int[] { 1, 2 }, true),
                new SetTriad(new int[] { 1, 2 }, new int[] { 1 }, true),
                new SetTriad(new int[] { 1 }, new int[] { 1 }, true),
                new SetTriad(new int[] { 1, 2 }, new int[] { 2, 3, 4 }, true),
            };
        }

        private void SetCompareTestHelper<T>(Func<System.Collections.Immutable.IImmutableSet<T>, Func<IEnumerable<T>, bool>> operation, Func<ISet<T>, Func<IEnumerable<T>, bool>> baselineOperation, IEnumerable<Tuple<IEnumerable<T>, IEnumerable<T>, bool>> scenarios)
        {
            //const string message = "Scenario #{0}: Set 1: {1}, Set 2: {2}";

            int iteration = 0;
            foreach (var scenario in scenarios)
            {
                iteration++;

                // Figure out the response expected based on the BCL mutable collections.
                var baselineSet = this.EmptyMutable<T>();
                baselineSet.UnionWith(scenario.Item1);
                var expectedFunc = baselineOperation(baselineSet);
                bool expected = expectedFunc(scenario.Item2);
                Assert.Equal(expected, scenario.Item3); //, "Test scenario has an expected result that is inconsistent with BCL mutable collection behavior.");

                var actualFunc = operation(this.SetWith(scenario.Item1.ToArray()));
                var args = new object[] { iteration, ToStringDeferred(scenario.Item1), ToStringDeferred(scenario.Item2) };
                Assert.Equal(scenario.Item3, actualFunc(this.SetWith(scenario.Item2.ToArray()))); //, message, args);
                Assert.Equal(scenario.Item3, actualFunc(scenario.Item2)); //, message, args);
            }
        }

        private static Tuple<IEnumerable<T>, IEnumerable<T>, bool> Flip<T>(Tuple<IEnumerable<T>, IEnumerable<T>, bool> scenario)
        {
            return new Tuple<IEnumerable<T>, IEnumerable<T>, bool>(scenario.Item2, scenario.Item1, scenario.Item3);
        }

        private static void RemoveTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> set, params T[] values)
        {
            Assert.NotNull(set);
            Assert.NotNull(values);

            Assert.True(IsSame(set, set.Except(Enumerable.Empty<T>())));

            int initialCount = set.Count;
            int removedCount = 0;
            foreach (T value in values)
            {
                var nextSet = set.Remove(value);
                Assert.NotSame(set, nextSet);
                Assert.Equal(initialCount - removedCount, set.Count);
                Assert.Equal(initialCount - removedCount - 1, nextSet.Count);

                Assert.True(IsSame(nextSet, nextSet.Remove(value))); //, "Removing a non-existing element should not change the set reference.");
                removedCount++;
                set = nextSet;
            }

            Assert.Equal(initialCount - removedCount, set.Count);
        }

        private static void AddRemoveLoadTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> set, T[] data)
        {
            Assert.NotNull(set);
            Assert.NotNull(data);

            foreach (T value in data)
            {
                var newSet = set.Add(value);
                Assert.NotSame(set, newSet);
                set = newSet;
            }

            foreach (T value in data)
            {
                Assert.True(set.Contains(value));
            }

            foreach (T value in data)
            {
                var newSet = set.Remove(value);
                Assert.NotSame(set, newSet);
                set = newSet;
            }
        }

        protected static void EnumeratorTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> emptySet, IComparer<T>? comparer, params T[] values)
        {
            var set = emptySet;
            foreach (T value in values)
            {
                set = set.Add(value);
            }

            var nonGenericEnumerableList = ToListNonGeneric<T>(set);
            CollectionAssertAreEquivalent(nonGenericEnumerableList, values);

            var list = set.ToList();
            CollectionAssertAreEquivalent(list, values);

            if (comparer != null)
            {
                Array.Sort(values, comparer);
                Assert.Equal<T>(values, list);
            }

            // Apply some less common uses to the enumerator to test its metal.
            IEnumerator<T> enumerator;
            using (enumerator = set.GetEnumerator())
            {
                Assert.Equal(default, enumerator.Current);
                enumerator.Reset(); // reset isn't usually called before MoveNext
                Assert.Equal(default, enumerator.Current);
                ManuallyEnumerateTest(list, enumerator);
                Assert.False(enumerator.MoveNext()); // call it again to make sure it still returns false

                enumerator.Reset();
                Assert.Equal(default, enumerator.Current);
                ManuallyEnumerateTest(list, enumerator);
                Assert.Equal(default, enumerator.Current);

                // this time only partially enumerate
                enumerator.Reset();
                enumerator.MoveNext();
                enumerator.Reset();
                ManuallyEnumerateTest(list, enumerator);
            }

            enumerator.Reset();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(set.First(), enumerator.Current);
        }

        private static void AddTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> set, params T[] values)
        {
            Assert.NotNull(set);
            Assert.NotNull(values);

            Assert.True(IsSame(set, set.Union(Enumerable.Empty<T>())));

            int initialCount = set.Count;

            var uniqueValues = new HashSet<T>(values);
            var enumerateAddSet = set.Union(values);
            Assert.Equal(initialCount + uniqueValues.Count, enumerateAddSet.Count);
            foreach (T value in values)
            {
                Assert.True(enumerateAddSet.Contains(value));
            }

            int addedCount = 0;
            foreach (T value in values)
            {
                bool duplicate = set.Contains(value);
                var nextSet = set.Add(value);
                Assert.True(nextSet.Count > 0);
                Assert.Equal(initialCount + addedCount, set.Count);
                int expectedCount = initialCount + addedCount;
                if (!duplicate)
                {
                    expectedCount++;
                }
                Assert.Equal(expectedCount, nextSet.Count);
                Assert.Equal(duplicate, set.Contains(value));
                Assert.True(nextSet.Contains(value));
                if (!duplicate)
                {
                    addedCount++;
                }

                // Next assert temporarily disabled because Roslyn's set doesn't follow this rule.
                Assert.True(IsSame(nextSet, nextSet.Add(value))); //, "Adding duplicate value {0} should keep the original reference.", value);
                set = nextSet;
            }
        }
    }
}
