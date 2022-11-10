// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://raw.githubusercontent.com/dotnet/runtime/v6.0.0-preview.5.21301.5/src/libraries/System.Collections.Immutable/tests/ImmutableListTestBase.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public abstract class ImmutableListTestBase : SimpleElementImmutablesTestBase
    {
        protected static readonly Func<IList, object?, object> IndexOfFunc = (l, v) => l.IndexOf(v);
        protected static readonly Func<IList, object?, object> ContainsFunc = (l, v) => l.Contains(v);
        protected static readonly Func<IList, object?, object> RemoveFunc = (l, v) => { l.Remove(v); return l.Count; };

        internal abstract IReadOnlyList<T> GetListQuery<T>(ImmutableSegmentedList<T> list);

        private protected abstract ImmutableSegmentedList<TOutput> ConvertAllImpl<T, TOutput>(ImmutableSegmentedList<T> list, Converter<T, TOutput> converter);

        private protected abstract void ForEachImpl<T>(ImmutableSegmentedList<T> list, Action<T> action);

        private protected abstract ImmutableSegmentedList<T> GetRangeImpl<T>(ImmutableSegmentedList<T> list, int index, int count);

        private protected abstract void CopyToImpl<T>(ImmutableSegmentedList<T> list, T[] array);

        private protected abstract void CopyToImpl<T>(ImmutableSegmentedList<T> list, T[] array, int arrayIndex);

        private protected abstract void CopyToImpl<T>(ImmutableSegmentedList<T> list, int index, T[] array, int arrayIndex, int count);

        private protected abstract bool ExistsImpl<T>(ImmutableSegmentedList<T> list, Predicate<T> match);

        private protected abstract T? FindImpl<T>(ImmutableSegmentedList<T> list, Predicate<T> match);

        private protected abstract ImmutableSegmentedList<T> FindAllImpl<T>(ImmutableSegmentedList<T> list, Predicate<T> match);

        private protected abstract int FindIndexImpl<T>(ImmutableSegmentedList<T> list, Predicate<T> match);

        private protected abstract int FindIndexImpl<T>(ImmutableSegmentedList<T> list, int startIndex, Predicate<T> match);

        private protected abstract int FindIndexImpl<T>(ImmutableSegmentedList<T> list, int startIndex, int count, Predicate<T> match);

        private protected abstract T? FindLastImpl<T>(ImmutableSegmentedList<T> list, Predicate<T> match);

        private protected abstract int FindLastIndexImpl<T>(ImmutableSegmentedList<T> list, Predicate<T> match);

        private protected abstract int FindLastIndexImpl<T>(ImmutableSegmentedList<T> list, int startIndex, Predicate<T> match);

        private protected abstract int FindLastIndexImpl<T>(ImmutableSegmentedList<T> list, int startIndex, int count, Predicate<T> match);

        private protected abstract bool TrueForAllImpl<T>(ImmutableSegmentedList<T> list, Predicate<T> test);

        private protected abstract int BinarySearchImpl<T>(ImmutableSegmentedList<T> list, T item);

        private protected abstract int BinarySearchImpl<T>(ImmutableSegmentedList<T> list, T item, IComparer<T>? comparer);

        private protected abstract int BinarySearchImpl<T>(ImmutableSegmentedList<T> list, int index, int count, T item, IComparer<T>? comparer);

        [Fact]
        public void CopyToEmptyTest()
        {
            var array = new int[0];
            CopyToImpl(ImmutableSegmentedList<int>.Empty, array);
            CopyToImpl(ImmutableSegmentedList<int>.Empty, array, 0);
            CopyToImpl(ImmutableSegmentedList<int>.Empty, 0, array, 0, 0);
            ((ICollection)this.GetListQuery(ImmutableSegmentedList<int>.Empty)).CopyTo(array, 0);
        }

        [Fact]
        public void CopyToTest()
        {
            var list = ImmutableSegmentedList.Create(1, 2);
            var enumerable = (IEnumerable<int>)list;

            var array = new int[2];
            CopyToImpl(list, array);
            Assert.Equal(enumerable, array);

            array = new int[2];
            CopyToImpl(list, array, 0);
            Assert.Equal(enumerable, array);

            array = new int[2];
            CopyToImpl(list, 0, array, 0, list.Count);
            Assert.Equal(enumerable, array);

            array = new int[1]; // shorter than source length
            CopyToImpl(list, 0, array, 0, array.Length);
            Assert.Equal(enumerable.Take(array.Length), array);

            array = new int[3];
            CopyToImpl(list, 1, array, 2, 1);
            Assert.Equal(new[] { 0, 0, 2 }, array);

            array = new int[2];
            ((ICollection)GetListQuery(list)).CopyTo(array, 0);
            Assert.Equal(enumerable, array);
        }

        [Fact]
        public void ForEachTest()
        {
            ForEachImpl(ImmutableSegmentedList<int>.Empty, n => { throw ExceptionUtilities.Unreachable(); });

            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(5, 3));
            var hitTest = new bool[list.Max() + 1];
            ForEachImpl(list, i =>
            {
                Assert.False(hitTest[i]);
                hitTest[i] = true;
            });

            for (int i = 0; i < hitTest.Length; i++)
            {
                Assert.Equal(list.Contains(i), hitTest[i]);
                Assert.Equal(((IList)list).Contains(i), hitTest[i]);
            }
        }

        [Fact]
        public void ExistsTest()
        {
            Assert.False(ExistsImpl(ImmutableSegmentedList<int>.Empty, n => true));

            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(1, 5));
            Assert.True(ExistsImpl(list, n => n == 3));
            Assert.False(ExistsImpl(list, n => n == 8));
        }

        [Fact]
        public void FindAllTest()
        {
            Assert.True(FindAllImpl(ImmutableSegmentedList<int>.Empty, n => true).IsEmpty);
            var list = ImmutableSegmentedList<int>.Empty.AddRange(new[] { 2, 3, 4, 5, 6 });
            var actual = FindAllImpl(list, n => n % 2 == 1);
            var expected = list.ToList().FindAll(n => n % 2 == 1);
            Assert.Equal<int>(expected, actual.ToList());
        }

        [Fact]
        public void FindTest()
        {
            Assert.Equal(0, FindImpl(ImmutableSegmentedList<int>.Empty, n => true));
            var list = ImmutableSegmentedList<int>.Empty.AddRange(new[] { 2, 3, 4, 5, 6 });
            Assert.Equal(3, FindImpl(list, n => (n % 2) == 1));
        }

        [Fact]
        public void FindLastTest()
        {
            Assert.Equal(0, FindLastImpl(ImmutableSegmentedList<int>.Empty, n => { throw ExceptionUtilities.Unreachable(); }));
            var list = ImmutableSegmentedList<int>.Empty.AddRange(new[] { 2, 3, 4, 5, 6 });
            Assert.Equal(5, FindLastImpl(list, n => (n % 2) == 1));
        }

        [Fact]
        public void FindIndexTest()
        {
            Assert.Equal(-1, FindIndexImpl(ImmutableSegmentedList<int>.Empty, n => true));
            Assert.Equal(-1, FindIndexImpl(ImmutableSegmentedList<int>.Empty, 0, n => true));
            Assert.Equal(-1, FindIndexImpl(ImmutableSegmentedList<int>.Empty, 0, 0, n => true));

            // Create a list with contents: 100,101,102,103,104,100,101,102,103,104
            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(100, 5).Concat(Enumerable.Range(100, 5)));
            var bclList = list.ToList();
            Assert.Equal(-1, FindIndexImpl(list, n => n == 6));

            for (int idx = 0; idx < list.Count; idx++)
            {
                for (int count = 0; count <= list.Count - idx; count++)
                {
                    foreach (int c in list)
                    {
                        int predicateInvocationCount = 0;
                        Predicate<int> match = n =>
                        {
                            predicateInvocationCount++;
                            return n == c;
                        };
                        int expected = bclList.FindIndex(idx, count, match);
                        int expectedInvocationCount = predicateInvocationCount;
                        predicateInvocationCount = 0;
                        int actual = FindIndexImpl(list, idx, count, match);
                        int actualInvocationCount = predicateInvocationCount;
                        Assert.Equal(expected, actual);
                        Assert.Equal(expectedInvocationCount, actualInvocationCount);

                        if (count == list.Count)
                        {
                            // Also test the FindIndex overload that takes no count parameter.
                            predicateInvocationCount = 0;
                            actual = FindIndexImpl(list, idx, match);
                            Assert.Equal(expected, actual);
                            Assert.Equal(expectedInvocationCount, actualInvocationCount);

                            if (idx == 0)
                            {
                                // Also test the FindIndex overload that takes no index parameter.
                                predicateInvocationCount = 0;
                                actual = FindIndexImpl(list, match);
                                Assert.Equal(expected, actual);
                                Assert.Equal(expectedInvocationCount, actualInvocationCount);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void FindLastIndexTest()
        {
            Assert.Equal(-1, FindLastIndexImpl(ImmutableSegmentedList<int>.Empty, n => true));
            Assert.Equal(-1, FindLastIndexImpl(ImmutableSegmentedList<int>.Empty, 0, n => true));
            Assert.Equal(-1, FindLastIndexImpl(ImmutableSegmentedList<int>.Empty, 0, 0, n => true));

            // Create a list with contents: 100,101,102,103,104,100,101,102,103,104
            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(100, 5).Concat(Enumerable.Range(100, 5)));
            var bclList = list.ToList();
            Assert.Equal(-1, FindLastIndexImpl(list, n => n == 6));

            for (int idx = 0; idx < list.Count; idx++)
            {
                for (int count = 0; count <= idx + 1; count++)
                {
                    foreach (int c in list)
                    {
                        int predicateInvocationCount = 0;
                        Predicate<int> match = n =>
                        {
                            predicateInvocationCount++;
                            return n == c;
                        };
                        int expected = bclList.FindLastIndex(idx, count, match);
                        int expectedInvocationCount = predicateInvocationCount;
                        predicateInvocationCount = 0;
                        int actual = FindLastIndexImpl(list, idx, count, match);
                        int actualInvocationCount = predicateInvocationCount;
                        Assert.Equal(expected, actual);
                        Assert.Equal(expectedInvocationCount, actualInvocationCount);

                        if (count == list.Count)
                        {
                            // Also test the FindIndex overload that takes no count parameter.
                            predicateInvocationCount = 0;
                            actual = FindLastIndexImpl(list, idx, match);
                            Assert.Equal(expected, actual);
                            Assert.Equal(expectedInvocationCount, actualInvocationCount);

                            if (idx == list.Count - 1)
                            {
                                // Also test the FindIndex overload that takes no index parameter.
                                predicateInvocationCount = 0;
                                actual = FindLastIndexImpl(list, match);
                                Assert.Equal(expected, actual);
                                Assert.Equal(expectedInvocationCount, actualInvocationCount);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void IList_IndexOf_NullArgument()
        {
            this.AssertIListBaseline(IndexOfFunc, 1, null);
            this.AssertIListBaseline(IndexOfFunc, "item", null);
            this.AssertIListBaseline(IndexOfFunc, new int?(1), null);
            this.AssertIListBaseline(IndexOfFunc, new int?(), null);
        }

        [Fact]
        public void IList_IndexOf_ArgTypeMismatch()
        {
            this.AssertIListBaseline(IndexOfFunc, "first item", new object());
            this.AssertIListBaseline(IndexOfFunc, 1, 1.0);

            this.AssertIListBaseline(IndexOfFunc, new int?(1), 1);
            this.AssertIListBaseline(IndexOfFunc, new int?(1), new int?(1));
            this.AssertIListBaseline(IndexOfFunc, new int?(1), string.Empty);
        }

        [Fact]
        public void IList_IndexOf_EqualsOverride()
        {
            this.AssertIListBaseline(IndexOfFunc, new ProgrammaticEquals(v => v is string), "foo");
            this.AssertIListBaseline(IndexOfFunc, new ProgrammaticEquals(v => v is string), 3);
        }

        [Fact]
        public void IList_Contains_NullArgument()
        {
            this.AssertIListBaseline(ContainsFunc, 1, null);
            this.AssertIListBaseline(ContainsFunc, "item", null);
            this.AssertIListBaseline(ContainsFunc, new int?(1), null);
            this.AssertIListBaseline(ContainsFunc, new int?(), null);
        }

        [Fact]
        public void IList_Contains_ArgTypeMismatch()
        {
            this.AssertIListBaseline(ContainsFunc, "first item", new object());
            this.AssertIListBaseline(ContainsFunc, 1, 1.0);

            this.AssertIListBaseline(ContainsFunc, new int?(1), 1);
            this.AssertIListBaseline(ContainsFunc, new int?(1), new int?(1));
            this.AssertIListBaseline(ContainsFunc, new int?(1), string.Empty);
        }

        [Fact]
        public void IList_Contains_EqualsOverride()
        {
            this.AssertIListBaseline(ContainsFunc, new ProgrammaticEquals(v => v is string), "foo");
            this.AssertIListBaseline(ContainsFunc, new ProgrammaticEquals(v => v is string), 3);
        }

        [Fact]
        public void ConvertAllTest()
        {
            Assert.True(ConvertAllImpl<int, float>(ImmutableSegmentedList<int>.Empty, n => n).IsEmpty);
            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(5, 10));
            Converter<int, double> converter = n => 2.0 * n;
            var expected = list.ToList().Select(x => converter(x)).ToList();
            var actual = ConvertAllImpl(list, converter);
            Assert.Equal<double>(expected.ToList(), actual.ToList());
        }

        [Fact]
        public void GetRangeTest()
        {
            Assert.True(GetRangeImpl(ImmutableSegmentedList<int>.Empty, 0, 0).IsEmpty);
            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(5, 10));
            var bclList = list.ToList();

            for (int index = 0; index < list.Count; index++)
            {
                for (int count = 0; count < list.Count - index; count++)
                {
                    var expected = bclList.GetRange(index, count);
                    var actual = GetRangeImpl(list, index, count);
                    Assert.Equal<int>(expected.ToList(), actual.ToList());
                }
            }
        }

        [Fact]
        public void TrueForAllTest()
        {
            Assert.True(TrueForAllImpl(ImmutableSegmentedList<int>.Empty, n => false));
            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(5, 10));
            this.TrueForAllTestHelper(list, n => n % 2 == 0);
            this.TrueForAllTestHelper(list, n => n % 2 == 1);
            this.TrueForAllTestHelper(list, n => true);
        }

        [Fact]
        public void RemoveAllTest()
        {
            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(5, 10));
            this.RemoveAllTestHelper(list, n => false);
            this.RemoveAllTestHelper(list, n => true);
            this.RemoveAllTestHelper(list, n => n < 7);
            this.RemoveAllTestHelper(list, n => n > 7);
            this.RemoveAllTestHelper(list, n => n == 7);
        }

        [Fact]
        public void ReverseTest()
        {
            var list = ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(5, 10));

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list.Count - i; j++)
                {
                    this.ReverseTestHelper(list, i, j);
                }
            }
        }

        [Fact]
        public void Sort_NullComparison_Throws()
        {
            Assert.Throws<ArgumentNullException>("comparison", () => this.SortTestHelper(ImmutableSegmentedList<int>.Empty, (Comparison<int>)null!));
        }

        [Fact]
        public void SortTest()
        {
            var scenarios = new[] {
                ImmutableSegmentedList<int>.Empty,
                ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(1, 50)),
                ImmutableSegmentedList<int>.Empty.AddRange(Enumerable.Range(1, 50).Reverse()),
            };

            foreach (var scenario in scenarios)
            {
                var expected = scenario.ToList();
                expected.Sort();
                var actual = this.SortTestHelper(scenario);
                Assert.Equal<int>(expected, actual);

                expected = scenario.ToList();
                Comparison<int> comparison = (x, y) => x > y ? 1 : (x < y ? -1 : 0);
                expected.Sort(comparison);
                actual = this.SortTestHelper(scenario, comparison);
                Assert.Equal<int>(expected, actual);

                expected = scenario.ToList();
                IComparer<int>? comparer = null;
                expected.Sort(comparer);
                actual = this.SortTestHelper(scenario, comparer);
                Assert.Equal<int>(expected, actual);

                expected = scenario.ToList();
                comparer = Comparer<int>.Create(comparison);
                expected.Sort(comparer);
                actual = this.SortTestHelper(scenario, comparer);
                Assert.Equal<int>(expected, actual);

                for (int i = 0; i < scenario.Count; i++)
                {
                    for (int j = 0; j < scenario.Count - i; j++)
                    {
                        expected = scenario.ToList();
                        comparer = null;
                        expected.Sort(i, j, comparer);
                        actual = this.SortTestHelper(scenario, i, j, comparer);
                        Assert.Equal<int>(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public void BinarySearch()
        {
            var basis = new List<int>(Enumerable.Range(1, 50).Select(n => n * 2));
            var query = basis.ToImmutableSegmentedList();
            for (int value = basis.First() - 1; value <= basis.Last() + 1; value++)
            {
                int expected = basis.BinarySearch(value);
                int actual = BinarySearchImpl(query, value);
                if (expected != actual) Debugger.Break();
                Assert.Equal(expected, actual);

                for (int index = 0; index < basis.Count - 1; index++)
                {
                    for (int count = 0; count <= basis.Count - index; count++)
                    {
                        expected = basis.BinarySearch(index, count, value, null);
                        actual = BinarySearchImpl(query, index, count, value, null);
                        if (expected != actual) Debugger.Break();
                        Assert.Equal(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public void BinarySearchPartialSortedList()
        {
            var reverseSorted = System.Collections.Immutable.ImmutableArray.CreateRange(Enumerable.Range(1, 150).Select(n => n * 2).Reverse());
            this.BinarySearchPartialSortedListHelper(reverseSorted, 0, 50);
            this.BinarySearchPartialSortedListHelper(reverseSorted, 50, 50);
            this.BinarySearchPartialSortedListHelper(reverseSorted, 100, 50);
        }

        private void BinarySearchPartialSortedListHelper(System.Collections.Immutable.ImmutableArray<int> inputData, int sortedIndex, int sortedLength)
        {
            if (sortedIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sortedIndex));
            if (sortedLength < 1)
                throw new ArgumentOutOfRangeException(nameof(sortedLength));

            inputData = inputData.Sort(sortedIndex, sortedLength, Comparer<int>.Default);
            int min = inputData[sortedIndex];
            int max = inputData[sortedIndex + sortedLength - 1];

            var basis = new List<int>(inputData);
            var query = inputData.ToImmutableSegmentedList();
            for (int value = min - 1; value <= max + 1; value++)
            {
                for (int index = sortedIndex; index < sortedIndex + sortedLength; index++) // make sure the index we pass in is always within the sorted range in the list.
                {
                    for (int count = 0; count <= sortedLength - index; count++)
                    {
                        int expected = basis.BinarySearch(index, count, value, null);
                        int actual = BinarySearchImpl(query, index, count, value, null);
                        if (expected != actual) Debugger.Break();
                        Assert.Equal(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public void SyncRoot()
        {
            var collection = (ICollection)this.GetEnumerableOf<int>();
            Assert.NotNull(collection.SyncRoot);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }

        [Fact]
        public void GetEnumeratorTest()
        {
            var enumerable = this.GetEnumerableOf(1);
            Assert.Equal(new[] { 1 }, enumerable.ToList()); // exercises the enumerator

            IEnumerable enumerableNonGeneric = enumerable;
            Assert.Equal(new[] { 1 }, enumerableNonGeneric.Cast<int>().ToList()); // exercises the enumerator
        }

        private protected abstract void RemoveAllTestHelper<T>(ImmutableSegmentedList<T> list, Predicate<T> test);

        private protected abstract void ReverseTestHelper<T>(ImmutableSegmentedList<T> list, int index, int count);

        private protected abstract List<T> SortTestHelper<T>(ImmutableSegmentedList<T> list);

        private protected abstract List<T> SortTestHelper<T>(ImmutableSegmentedList<T> list, Comparison<T> comparison);

        private protected abstract List<T> SortTestHelper<T>(ImmutableSegmentedList<T> list, IComparer<T>? comparer);

        private protected abstract List<T> SortTestHelper<T>(ImmutableSegmentedList<T> list, int index, int count, IComparer<T>? comparer);

        protected void AssertIListBaselineBothDirections<T1, T2>(Func<IList, object?, object> operation, T1 item, T2 other)
        {
            this.AssertIListBaseline(operation, item, other);
            this.AssertIListBaseline(operation, other, item);
        }

        /// <summary>
        /// Asserts that the <see cref="ImmutableSegmentedList{T}"/> or <see cref="ImmutableSegmentedList{T}.Builder"/>'s
        /// implementation of <see cref="IList"/> behave the same way <see cref="List{T}"/> does.
        /// </summary>
        /// <typeparam name="T">The type of the element for one collection to test with.</typeparam>
        /// <param name="operation">
        /// The <see cref="IList"/> operation to perform.
        /// The function is provided with the <see cref="IList"/> implementation to test
        /// and the item to use as the argument to the operation.
        /// The function should return some equatable value by which to compare the effects
        /// of the operation across <see cref="IList"/> implementations.
        /// </param>
        /// <param name="item">The item to add to the collection.</param>
        /// <param name="other">The item to pass to the <paramref name="operation"/> function as the second parameter.</param>
        protected void AssertIListBaseline<T>(Func<IList, object?, object> operation, T item, object? other)
        {
            IList bclList = new List<T> { item };
            IList testedList = (IList)this.GetListQuery(ImmutableSegmentedList.Create(item));

            object expected = operation(bclList, other);
            object actual = operation(testedList, other);
            Assert.Equal(expected, actual);
        }

        private void TrueForAllTestHelper<T>(ImmutableSegmentedList<T> list, Predicate<T> test)
        {
            var bclList = list.ToList();
            var expected = bclList.TrueForAll(test);
            var actual = TrueForAllImpl(list, test);
            Assert.Equal(expected, actual);
        }

        protected class ProgrammaticEquals
        {
            private readonly Func<object?, bool> equalsCallback;

            internal ProgrammaticEquals(Func<object?, bool> equalsCallback)
            {
                this.equalsCallback = equalsCallback;
            }

            public override bool Equals(object? obj)
            {
                return this.equalsCallback(obj);
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }
    }
}
