// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class TemporaryArrayTests
    {
        [Fact]
        public void TestEmptyAndDefault()
        {
            Assert.Equal(0, TemporaryArray<int>.Empty.Count);
            Assert.Equal(0, default(TemporaryArray<int>).Count);
            Assert.Equal(0, new TemporaryArray<int>().Count);

            Assert.Throws<IndexOutOfRangeException>(() => TemporaryArray<int>.Empty[-1]);
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                using var array = TemporaryArray<int>.Empty;
                array.AsRef()[-1] = 1;
            });

            Assert.Throws<IndexOutOfRangeException>(() => TemporaryArray<int>.Empty[0]);
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                using var array = TemporaryArray<int>.Empty;
                array.AsRef()[0] = 1;
            });

            Assert.False(TemporaryArray<int>.Empty.GetEnumerator().MoveNext());
        }

        [Fact]
        public void TestInlineElements()
        {
            using var array = TemporaryArray<int>.Empty;
            for (var i = 0; i < TemporaryArray<int>.TestAccessor.InlineCapacity; i++)
            {
                Assert.Equal(i, array.Count);
                AddAndCheck();
                Assert.False(TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
                Assert.Equal(i + 1, TemporaryArray<int>.TestAccessor.InlineCount(in array));
            }

            // The next add forces a transition to dynamic storage
            Assert.Equal(TemporaryArray<int>.TestAccessor.InlineCapacity, array.Count);
            Assert.False(TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
            AddAndCheck();
            Assert.True(TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
            Assert.Equal(0, TemporaryArray<int>.TestAccessor.InlineCount(in array));

            // The next goes directly to existing dynamic storage
            Assert.Equal(TemporaryArray<int>.TestAccessor.InlineCapacity + 1, array.Count);
            AddAndCheck();
            Assert.True(TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
            Assert.Equal(0, TemporaryArray<int>.TestAccessor.InlineCount(in array));

            // Local functions
            void AddAndCheck()
            {
                var i = array.Count;
                Assert.Throws<IndexOutOfRangeException>(() => array[i]);
                Assert.Throws<IndexOutOfRangeException>(() => array.AsRef()[i] = 1);

                array.Add(i);
                Assert.Equal(i + 1, array.Count);
                Assert.Equal(i, array[i]);
                array.AsRef()[i] = i + 1;
                Assert.Equal(i + 1, array[i]);
            }
        }

        [Fact]
        public void CannotMutateEmpty()
        {
            Assert.Equal(0, TemporaryArray<int>.Empty.Count);
            TemporaryArray<int>.Empty.Add(0);
            Assert.Equal(0, TemporaryArray<int>.Empty.Count);
        }

        [Fact]
        public void TestDisposeFreesBuilder()
        {
            var array = TemporaryArray<int>.Empty;
            array.AddRange(Enumerable.Range(0, TemporaryArray<int>.TestAccessor.InlineCapacity + 1).ToImmutableArray());
            Assert.True(TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));

            array.Dispose();
            Assert.False(TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
        }

        [Theory]
        [CombinatorialData]
        public void TestAddRange([CombinatorialRange(0, 6)] int initialItems, [CombinatorialRange(0, 6)] int addedItems)
        {
            using var array = TemporaryArray<int>.Empty;
            for (var i = 0; i < initialItems; i++)
                array.Add(i);

            Assert.Equal(initialItems, array.Count);
            array.AddRange(Enumerable.Range(0, addedItems).ToImmutableArray());
            Assert.Equal(initialItems + addedItems, array.Count);

            if (array.Count > TemporaryArray<int>.TestAccessor.InlineCapacity)
            {
                Assert.True(TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
                Assert.Equal(0, TemporaryArray<int>.TestAccessor.InlineCount(in array));
            }
            else
            {
                Assert.False(TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
                Assert.Equal(array.Count, TemporaryArray<int>.TestAccessor.InlineCount(in array));
            }

            for (var i = 0; i < initialItems; i++)
                Assert.Equal(i, array[i]);

            for (var i = 0; i < addedItems; i++)
                Assert.Equal(i, array[initialItems + i]);
        }

        [Theory]
        [CombinatorialData]
        public void TestClear([CombinatorialRange(0, 6)] int initialItems)
        {
            using var array = TemporaryArray<int>.Empty;
            for (var i = 0; i < initialItems; i++)
                array.Add(i);

            Assert.Equal(initialItems, array.Count);

            array.Clear();
            Assert.Equal(0, array.Count);

            // TemporaryArray<T>.Clear does not move from dynamic back to inline storage, so we condition this assertion
            // on the count prior to calling Clear.
            Assert.Equal(
                initialItems > TemporaryArray<int>.TestAccessor.InlineCapacity,
                TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
        }

        [Theory]
        [CombinatorialData]
        public void TestEnumerator([CombinatorialRange(0, 6)] int initialItems)
        {
            using var array = TemporaryArray<int>.Empty;
            for (var i = 0; i < initialItems; i++)
                array.Add(i);

            Assert.Equal(initialItems, array.Count);

            var enumerator = array.GetEnumerator();
            for (var i = 0; i < initialItems; i++)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(i, enumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
        }

        [Theory]
        [CombinatorialData]
        public void TestMoveToImmutable([CombinatorialRange(0, 6)] int initialItems)
        {
            using var array = TemporaryArray<int>.Empty;
            for (var i = 0; i < initialItems; i++)
                array.Add(i);

            Assert.Equal(initialItems, array.Count);

            var immutableArray = array.ToImmutableAndClear();
            Assert.Equal(Enumerable.Range(0, initialItems), immutableArray);

            Assert.Equal(0, array.Count);

            // TemporaryArray<T>.MoveToImmutable does not move from dynamic back to inline storage, so we condition this
            // assertion on the count prior to calling Clear.
            Assert.Equal(
                initialItems > TemporaryArray<int>.TestAccessor.InlineCapacity,
                TemporaryArray<int>.TestAccessor.HasDynamicStorage(in array));
        }

        [Theory]
        [CombinatorialData]
        public void TestReverseContents([CombinatorialRange(0, 6)] int initialItems)
        {
            using var array = TemporaryArray<int>.Empty;
            for (var i = 0; i < initialItems; i++)
                array.Add(i);

            Assert.Equal(initialItems, array.Count);

            array.ReverseContents();

            Assert.Equal(initialItems, array.Count);

            for (var i = 0; i < initialItems; i++)
                Assert.Equal(array[i], initialItems - 1 - i);
        }

        [Fact]
        public void TestSort()
        {
            // Create arrays with different lengths, making sure to exceed the number of inline elements to test all code paths.
            for (int i = 0; i <= TemporaryArray<int>.TestAccessor.InlineCapacity + 1; i++)
            {
                foreach (var permutation in permute(Enumerable.Range(0, i).ToArray()))
                {
                    assertSort(permutation);
                }
            }

            static void assertSort(ImmutableArray<int> inputArray)
            {
                var sortedArray = inputArray.Sort();
                Assert.Equal(inputArray.Length, sortedArray.Length);
                using var array = TemporaryArray<int>.Empty;
                foreach (var num in inputArray)
                    array.Add(num);

                Assert.Equal(array.Count, sortedArray.Length);
                array.Sort((x, y) => x.CompareTo(y));
                Assert.Equal(array.Count, sortedArray.Length);
                for (int i = 0; i < array.Count; i++)
                {
                    Assert.Equal(array[i], sortedArray[i]);
                }
            }

            // Almost copy from ServiceHubServicesTests
            static List<ImmutableArray<T>> permute<T>(T[] values)
            {
                var result = new List<ImmutableArray<T>>();
                if (values.Length == 0)
                {
                    result.Add(ImmutableArray<T>.Empty);
                    return result;
                }

                doPermute(0, values.Length - 1);
                return result;

                void doPermute(int start, int end)
                {
                    if (start == end)
                    {
                        // We have one of our possible n! solutions,
                        // add it to the list.
                        result.Add(values.ToImmutableArray());
                    }
                    else
                    {
                        for (var i = start; i <= end; i++)
                        {
                            (values[start], values[i]) = (values[i], values[start]);
                            doPermute(start + 1, end);
                            (values[start], values[i]) = (values[i], values[start]);
                        }
                    }
                }
            }
        }

        [Theory, CombinatorialData]
        public void TestRemoveLast([CombinatorialRange(0, 6)] int initialItems)
        {
            using var array = TemporaryArray<int>.Empty;
            for (var i = 0; i < initialItems; i++)
                array.Add(i);

            if (initialItems == 0)
            {
                Assert.Throws<IndexOutOfRangeException>(() => array.RemoveLast());
            }
            else
            {
                var count = array.Count;
                var last = array.RemoveLast();
                Assert.Equal(initialItems - 1, last);
                Assert.Equal(count - 1, array.Count);
            }
        }

        [Theory, CombinatorialData]
        public void TestContains([CombinatorialRange(0, 6)] int initialItems)
        {
            using var array = TemporaryArray<int>.Empty;
            for (var i = 0; i < initialItems; i++)
                array.Add(i);

            for (var i = 0; i < initialItems; i++)
                Assert.True(array.Contains(i));

            Assert.False(array.Contains(-1));
            Assert.False(array.Contains(initialItems));
        }

        [Fact]
        public void TestSingleOrDefault()
        {
            using var array = TemporaryArray<int>.Empty;
            array.Add(1);
            array.Add(2);
            array.Add(3);

            Assert.Equal(3, array.SingleOrDefault(p => p > 2));
            Assert.Equal(3, array.SingleOrDefault((p, arg) => p > arg, arg: 2));

            Assert.Equal(0, array.SingleOrDefault(p => p > 5));
            Assert.Equal(0, array.SingleOrDefault((p, arg) => p > arg, arg: 5));

            Assert.Throws<InvalidOperationException>(() => array.SingleOrDefault(p => p > 1));
            Assert.Throws<InvalidOperationException>(() => array.SingleOrDefault((p, arg) => p > arg, arg: 1));
        }
    }
}
