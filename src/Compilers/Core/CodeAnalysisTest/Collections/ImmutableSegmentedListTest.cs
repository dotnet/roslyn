// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableListTest : ImmutableListTestBase
    {
        private enum Operation
        {
            Add,
            AddRange,
            Insert,
            InsertRange,
            RemoveAt,
            RemoveRange,
            Last,
        }

        [Fact]
        public void RandomOperationsTest()
        {
            int operationCount = this.RandomOperationsCount;
            var expected = new List<int>();
            var actual = ImmutableList<int>.Empty;

            int seed = unchecked((int)DateTime.Now.Ticks);
            Debug.WriteLine("Using random seed {0}", seed);
            var random = new Random(seed);

            for (int iOp = 0; iOp < operationCount; iOp++)
            {
                switch ((Operation)random.Next((int)Operation.Last))
                {
                    case Operation.Add:
                        int value = random.Next();
                        Debug.WriteLine("Adding \"{0}\" to the list.", value);
                        expected.Add(value);
                        actual = actual.Add(value);
                        VerifyBalanced(actual);
                        break;
                    case Operation.AddRange:
                        int inputLength = random.Next(100);
                        int[] values = Enumerable.Range(0, inputLength).Select(i => random.Next()).ToArray();
                        Debug.WriteLine("Adding {0} elements to the list.", inputLength);
                        expected.AddRange(values);
                        actual = actual.AddRange(values);
                        VerifyBalanced(actual);
                        break;
                    case Operation.Insert:
                        int position = random.Next(expected.Count + 1);
                        value = random.Next();
                        Debug.WriteLine("Adding \"{0}\" to position {1} in the list.", value, position);
                        expected.Insert(position, value);
                        actual = actual.Insert(position, value);
                        VerifyBalanced(actual);
                        break;
                    case Operation.InsertRange:
                        inputLength = random.Next(100);
                        values = Enumerable.Range(0, inputLength).Select(i => random.Next()).ToArray();
                        position = random.Next(expected.Count + 1);
                        Debug.WriteLine("Adding {0} elements to position {1} in the list.", inputLength, position);
                        expected.InsertRange(position, values);
                        actual = actual.InsertRange(position, values);
                        VerifyBalanced(actual);
                        break;
                    case Operation.RemoveAt:
                        if (expected.Count > 0)
                        {
                            position = random.Next(expected.Count);
                            Debug.WriteLine("Removing element at position {0} from the list.", position);
                            expected.RemoveAt(position);
                            actual = actual.RemoveAt(position);
                            VerifyBalanced(actual);
                        }

                        break;
                    case Operation.RemoveRange:
                        position = random.Next(expected.Count);
                        inputLength = random.Next(expected.Count - position);
                        Debug.WriteLine("Removing {0} elements starting at position {1} from the list.", inputLength, position);
                        expected.RemoveRange(position, inputLength);
                        actual = actual.RemoveRange(position, inputLength);
                        VerifyBalanced(actual);
                        break;
                }

                Assert.Equal<int>(expected, actual);
            }
        }

        [Fact]
        public void EmptyTest()
        {
            var empty = ImmutableList<GenericParameterHelper>.Empty;
            Assert.Same(empty, ImmutableList<GenericParameterHelper>.Empty);
            Assert.Same(empty, empty.Clear());
            Assert.Same(empty, ((IImmutableList<GenericParameterHelper>)empty).Clear());
            Assert.True(empty.IsEmpty);
            Assert.Equal(0, empty.Count);
            Assert.Equal(-1, empty.IndexOf(new GenericParameterHelper()));
            Assert.Equal(-1, empty.IndexOf(null));
        }

        [Fact]
        public void GetHashCodeVariesByInstance()
        {
            Assert.NotEqual(ImmutableList.Create<int>().GetHashCode(), ImmutableList.Create(5).GetHashCode());
        }

        [Fact]
        public void AddAndIndexerTest()
        {
            var list = ImmutableList<int>.Empty;
            for (int i = 1; i <= 10; i++)
            {
                list = list.Add(i * 10);
                Assert.False(list.IsEmpty);
                Assert.Equal(i, list.Count);
            }

            for (int i = 1; i <= 10; i++)
            {
                Assert.Equal(i * 10, list[i - 1]);
            }

            var bulkList = ImmutableList<int>.Empty.AddRange(Enumerable.Range(1, 10).Select(i => i * 10));
            Assert.Equal<int>(list.ToArray(), bulkList.ToArray());
        }

        [Fact]
        public void AddRangeTest()
        {
            var list = ImmutableList<int>.Empty;
            list = list.AddRange(new[] { 1, 2, 3 });
            list = list.AddRange(Enumerable.Range(4, 2));
            list = list.AddRange(ImmutableList<int>.Empty.AddRange(new[] { 6, 7, 8 }));
            list = list.AddRange(new int[0]);
            list = list.AddRange(ImmutableList<int>.Empty.AddRange(Enumerable.Range(9, 1000)));
            Assert.Equal(Enumerable.Range(1, 1008), list);
        }

        [Fact]
        public void AddRange_IOrderedCollection()
        {
            var list = ImmutableList<int>.Empty;
            ImmutableList<int>.Builder builder = ImmutableList.CreateBuilder<int>();
            builder.Add(1);

            list = list.AddRange(builder);
            Assert.Equal(new int[] { 1 }, list);
        }

        [Fact]
        public void AddRangeOptimizationsTest()
        {
            // All these optimizations are tested based on filling an empty list.
            var emptyList = ImmutableList.Create<string>();

            // Adding an empty list to an empty list should yield the original list.
            Assert.Same(emptyList, emptyList.AddRange(new string[0]));

            // Adding a non-empty immutable list to an empty one should return the added list.
            var nonEmptyListDefaultComparer = ImmutableList.Create("5");
            Assert.Same(nonEmptyListDefaultComparer, emptyList.AddRange(nonEmptyListDefaultComparer));

            // Adding a Builder instance to an empty list should be seen through.
            var builderOfNonEmptyListDefaultComparer = nonEmptyListDefaultComparer.ToBuilder();
            Assert.Same(nonEmptyListDefaultComparer, emptyList.AddRange(builderOfNonEmptyListDefaultComparer));
        }

        [Fact]
        public void AddRangeBalanceTest()
        {
            int randSeed = unchecked((int)DateTime.Now.Ticks);
            Debug.WriteLine("Random seed: {0}", randSeed);
            var random = new Random(randSeed);

            int expectedTotalSize = 0;

            var list = ImmutableList<int>.Empty;

            // Add some small batches, verifying balance after each
            for (int i = 0; i < 128; i++)
            {
                int batchSize = random.Next(32);
                Debug.WriteLine("Adding {0} elements to the list", batchSize);
                list = list.AddRange(Enumerable.Range(expectedTotalSize + 1, batchSize));
                VerifyBalanced(list);
                expectedTotalSize += batchSize;
            }

            // Add a single large batch to the end
            int largeBatchSize = random.Next(32768) + 32768;
            Debug.WriteLine("Adding {0} elements to the list", largeBatchSize);
            list = list.AddRange(Enumerable.Range(expectedTotalSize + 1, largeBatchSize));
            VerifyBalanced(list);
            expectedTotalSize += largeBatchSize;

            Assert.Equal(Enumerable.Range(1, expectedTotalSize), list);

            list.Root.VerifyHeightIsWithinTolerance();
        }

        [Fact]
        public void InsertRangeRandomBalanceTest()
        {
            int randSeed = unchecked((int)DateTime.Now.Ticks);
            Debug.WriteLine("Random seed: {0}", randSeed);
            var random = new Random(randSeed);

            var immutableList = ImmutableList.CreateBuilder<int>();
            var list = new List<int>();

            const int maxBatchSize = 32;
            int valueCounter = 0;
            for (int i = 0; i < 24; i++)
            {
                int startPosition = random.Next(list.Count + 1);
                int length = random.Next(maxBatchSize + 1);
                int[] values = new int[length];
                for (int j = 0; j < length; j++)
                {
                    values[j] = ++valueCounter;
                }

                immutableList.InsertRange(startPosition, values);
                list.InsertRange(startPosition, values);

                Assert.Equal(list, immutableList);
                immutableList.Root.VerifyBalanced();
            }

            immutableList.Root.VerifyHeightIsWithinTolerance();
        }

        [Fact]
        public void InsertTest()
        {
            var list = ImmutableList<int>.Empty;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.Insert(1, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.Insert(-1, 5));

            list = list.Insert(0, 10);
            list = list.Insert(1, 20);
            list = list.Insert(2, 30);

            list = list.Insert(2, 25);
            list = list.Insert(1, 15);
            list = list.Insert(0, 5);

            Assert.Equal(6, list.Count);
            var expectedList = new[] { 5, 10, 15, 20, 25, 30 };
            var actualList = list.ToArray();
            Assert.Equal<int>(expectedList, actualList);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.Insert(7, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.Insert(-1, 5));
        }

        [Fact]
        public void InsertBalanceTest()
        {
            var list = ImmutableList.Create(1);

            list = list.Insert(0, 2);
            list = list.Insert(1, 3);

            VerifyBalanced(list);
        }

        [Fact]
        public void InsertRangeTest()
        {
            var list = ImmutableList<int>.Empty;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.InsertRange(1, new[] { 1 }));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.InsertRange(-1, new[] { 1 }));

            list = list.InsertRange(0, new[] { 1, 4, 5 });
            list = list.InsertRange(1, new[] { 2, 3 });
            list = list.InsertRange(2, new int[0]);
            Assert.Equal(Enumerable.Range(1, 5), list);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.InsertRange(6, new[] { 1 }));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.InsertRange(-1, new[] { 1 }));
        }

        [Fact]
        public void InsertRangeImmutableTest()
        {
            var list = ImmutableList<int>.Empty;
            var nonEmptyList = ImmutableList.Create(1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.InsertRange(1, nonEmptyList));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.InsertRange(-1, nonEmptyList));

            list = list.InsertRange(0, ImmutableList.Create(1, 104, 105));
            list = list.InsertRange(1, ImmutableList.Create(2, 3));
            list = list.InsertRange(2, ImmutableList<int>.Empty);
            list = list.InsertRange(3, ImmutableList<int>.Empty.InsertRange(0, Enumerable.Range(4, 100)));
            Assert.Equal(Enumerable.Range(1, 105), list);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.InsertRange(106, nonEmptyList));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.InsertRange(-1, nonEmptyList));
        }

        [Fact]
        public void NullHandlingTest()
        {
            var list = ImmutableList<GenericParameterHelper>.Empty;
            Assert.False(list.Contains(null));
            Assert.Equal(-1, list.IndexOf(null));

            list = list.Add((GenericParameterHelper)null);
            Assert.Equal(1, list.Count);
            Assert.Null(list[0]);
            Assert.True(list.Contains(null));
            Assert.Equal(0, list.IndexOf(null));

            list = list.Remove((GenericParameterHelper)null);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            Assert.False(list.Contains(null));
            Assert.Equal(-1, list.IndexOf(null));
        }

        [Fact]
        public void Remove_NullEqualityComparer()
        {
            var collection = ImmutableList.Create(1, 2, 3);
            var modified = collection.Remove(2, null);
            Assert.Equal(new[] { 1, 3 }, modified);

            // Try again through the explicit interface implementation.
            IImmutableList<int> collectionIface = collection;
            var modified2 = collectionIface.Remove(2, null);
            Assert.Equal(new[] { 1, 3 }, modified2);
        }

        [Fact]
        public void RemoveTest()
        {
            ImmutableList<int> list = ImmutableList<int>.Empty;
            for (int i = 1; i <= 10; i++)
            {
                list = list.Add(i * 10);
            }

            list = list.Remove(30);
            Assert.Equal(9, list.Count);
            Assert.False(list.Contains(30));

            list = list.Remove(100);
            Assert.Equal(8, list.Count);
            Assert.False(list.Contains(100));

            list = list.Remove(10);
            Assert.Equal(7, list.Count);
            Assert.False(list.Contains(10));

            var removeList = new int[] { 20, 70 };
            list = list.RemoveAll(removeList.Contains);
            Assert.Equal(5, list.Count);
            Assert.False(list.Contains(20));
            Assert.False(list.Contains(70));

            IImmutableList<int> list2 = ImmutableList<int>.Empty;
            for (int i = 1; i <= 10; i++)
            {
                list2 = list2.Add(i * 10);
            }

            list2 = list2.Remove(30);
            Assert.Equal(9, list2.Count);
            Assert.False(list2.Contains(30));

            list2 = list2.Remove(100);
            Assert.Equal(8, list2.Count);
            Assert.False(list2.Contains(100));

            list2 = list2.Remove(10);
            Assert.Equal(7, list2.Count);
            Assert.False(list2.Contains(10));

            list2 = list2.RemoveAll(removeList.Contains);
            Assert.Equal(5, list2.Count);
            Assert.False(list2.Contains(20));
            Assert.False(list2.Contains(70));
        }

        [Fact]
        public void RemoveNonExistentKeepsReference()
        {
            var list = ImmutableList<int>.Empty;
            Assert.Same(list, list.Remove(3));
        }

        /// <summary>
        /// Verifies that RemoveRange does not enumerate its argument if the list is empty
        /// and therefore could not possibly have any elements to remove anyway.
        /// </summary>
        /// <remarks>
        /// While this would seem an implementation detail and simply an optimization,
        /// it turns out that changing this behavior now *could* represent a breaking change
        /// because if the enumerable were to throw an exception, that exception would not be
        /// observed previously, but would start to be thrown if this behavior changed.
        /// So this is a test to lock the behavior in place.
        /// </remarks>
        /// <seealso cref="ImmutableSetTest.ExceptDoesEnumerateSequenceIfThisIsEmpty"/>
        [Fact]
        public void RemoveRangeDoesNotEnumerateSequenceIfThisIsEmpty()
        {
            var list = ImmutableList<int>.Empty;
            list.RemoveRange(Enumerable.Range(1, 1).Select<int, int>(n => { throw new ShouldNotBeInvokedException(); }));
        }

        [Fact]
        public void RemoveAtTest()
        {
            var list = ImmutableList<int>.Empty;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.RemoveAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.RemoveAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.RemoveAt(1));

            for (int i = 1; i <= 10; i++)
            {
                list = list.Add(i * 10);
            }

            list = list.RemoveAt(2);
            Assert.Equal(9, list.Count);
            Assert.False(list.Contains(30));

            list = list.RemoveAt(8);
            Assert.Equal(8, list.Count);
            Assert.False(list.Contains(100));

            list = list.RemoveAt(0);
            Assert.Equal(7, list.Count);
            Assert.False(list.Contains(10));
        }

        [Fact]
        public void IndexOfAndContainsTest()
        {
            var expectedList = new List<string>(new[] { "Microsoft", "Windows", "Bing", "Visual Studio", "Comics", "Computers", "Laptops" });

            var list = ImmutableList<string>.Empty;
            foreach (string newElement in expectedList)
            {
                Assert.False(list.Contains(newElement));
                list = list.Add(newElement);
                Assert.True(list.Contains(newElement));
                Assert.Equal(expectedList.IndexOf(newElement), list.IndexOf(newElement));
                Assert.Equal(expectedList.IndexOf(newElement), list.IndexOf(newElement.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase));
                Assert.Equal(-1, list.IndexOf(newElement.ToUpperInvariant()));

                foreach (string existingElement in expectedList.TakeWhile(v => v != newElement))
                {
                    Assert.True(list.Contains(existingElement));
                    Assert.Equal(expectedList.IndexOf(existingElement), list.IndexOf(existingElement));
                    Assert.Equal(expectedList.IndexOf(existingElement), list.IndexOf(existingElement.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase));
                    Assert.Equal(-1, list.IndexOf(existingElement.ToUpperInvariant()));
                }
            }
        }

        [Fact]
        public void Indexer()
        {
            var list = ImmutableList.CreateRange(Enumerable.Range(1, 3));
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
            Assert.Equal(3, list[2]);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[3]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[-1]);

            Assert.Equal(3, ((IList)list)[2]);
            Assert.Equal(3, ((IList<int>)list)[2]);
        }

        [Fact]
        public void IndexOf()
        {
            IndexOfTests.IndexOfTest(
                seq => ImmutableList.CreateRange(seq),
                (b, v) => b.IndexOf(v),
                (b, v, i) => b.IndexOf(v, i),
                (b, v, i, c) => b.IndexOf(v, i, c),
                (b, v, i, c, eq) => b.IndexOf(v, i, c, eq));
            IndexOfTests.IndexOfTest(
                seq => (IImmutableList<int>)ImmutableList.CreateRange(seq),
                (b, v) => b.IndexOf(v),
                (b, v, i) => b.IndexOf(v, i),
                (b, v, i, c) => b.IndexOf(v, i, c),
                (b, v, i, c, eq) => b.IndexOf(v, i, c, eq));
        }

        [Fact]
        public void LastIndexOf()
        {
            IndexOfTests.LastIndexOfTest(
                seq => ImmutableList.CreateRange(seq),
                (b, v) => b.LastIndexOf(v),
                (b, v, eq) => b.LastIndexOf(v, eq),
                (b, v, i) => b.LastIndexOf(v, i),
                (b, v, i, c) => b.LastIndexOf(v, i, c),
                (b, v, i, c, eq) => b.LastIndexOf(v, i, c, eq));
            IndexOfTests.LastIndexOfTest(
                seq => (IImmutableList<int>)ImmutableList.CreateRange(seq),
                (b, v) => b.LastIndexOf(v),
                (b, v, eq) => b.LastIndexOf(v, eq),
                (b, v, i) => b.LastIndexOf(v, i),
                (b, v, i, c) => b.LastIndexOf(v, i, c),
                (b, v, i, c, eq) => b.LastIndexOf(v, i, c, eq));
        }

        [Fact]
        public void ReplaceTest()
        {
            // Verify replace at beginning, middle, and end.
            var list = ImmutableList<int>.Empty.Add(3).Add(5).Add(8);
            Assert.Equal<int>(new[] { 4, 5, 8 }, list.Replace(3, 4));
            Assert.Equal<int>(new[] { 3, 6, 8 }, list.Replace(5, 6));
            Assert.Equal<int>(new[] { 3, 5, 9 }, list.Replace(8, 9));
            Assert.Equal<int>(new[] { 4, 5, 8 }, ((IImmutableList<int>)list).Replace(3, 4));
            Assert.Equal<int>(new[] { 3, 6, 8 }, ((IImmutableList<int>)list).Replace(5, 6));
            Assert.Equal<int>(new[] { 3, 5, 9 }, ((IImmutableList<int>)list).Replace(8, 9));

            // Verify replacement of first element when there are duplicates.
            list = ImmutableList<int>.Empty.Add(3).Add(3).Add(5);
            Assert.Equal<int>(new[] { 4, 3, 5 }, list.Replace(3, 4));
            Assert.Equal<int>(new[] { 4, 4, 5 }, list.Replace(3, 4).Replace(3, 4));
            Assert.Equal<int>(new[] { 4, 3, 5 }, ((IImmutableList<int>)list).Replace(3, 4));
            Assert.Equal<int>(new[] { 4, 4, 5 }, ((IImmutableList<int>)list).Replace(3, 4).Replace(3, 4));
        }

        [Fact]
        public void ReplaceWithEqualityComparerTest()
        {
            var list = ImmutableList.Create(new Person { Name = "Andrew", Age = 20 });
            var newAge = new Person { Name = "Andrew", Age = 21 };
            var updatedList = list.Replace(newAge, newAge, new NameOnlyEqualityComparer());
            Assert.Equal(newAge.Age, updatedList[0].Age);

            // Try again with a null equality comparer, which should use the default EQ.
            updatedList = list.Replace(list[0], newAge);
            Assert.NotSame(list, updatedList);

            // Finally, try one last time using the interface implementation.
            IImmutableList<Person> iface = list;
            var updatedIface = iface.Replace(list[0], newAge);
            Assert.NotSame(iface, updatedIface);
        }

        [Fact]
        public void ReplaceMissingThrowsTest()
        {
            AssertExtensions.Throws<ArgumentException>("oldValue", () => ImmutableList<int>.Empty.Replace(5, 3));
        }

        [Fact]
        public void EqualsTest()
        {
            Assert.False(ImmutableList<int>.Empty.Equals(null));
            Assert.False(ImmutableList<int>.Empty.Equals("hi"));
            Assert.True(ImmutableList<int>.Empty.Equals(ImmutableList<int>.Empty));
            Assert.False(ImmutableList<int>.Empty.Add(3).Equals(ImmutableList<int>.Empty.Add(3)));
        }

        [Fact]
        public void Create()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;

            ImmutableList<string> list = ImmutableList.Create<string>();
            Assert.Equal(0, list.Count);

            list = ImmutableList.Create("a");
            Assert.Equal(1, list.Count);

            list = ImmutableList.Create("a", "b");
            Assert.Equal(2, list.Count);

            list = ImmutableList.CreateRange((IEnumerable<string>)new[] { "a", "b" });
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void ToImmutableList()
        {
            ImmutableList<string> list = new[] { "a", "b" }.ToImmutableList();
            Assert.Equal(2, list.Count);

            list = new[] { "a", "b" }.ToImmutableList();
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void ToImmutableListOfSameType()
        {
            var list = ImmutableList.Create("a");
            Assert.Same(list, list.ToImmutableList());
        }

        [Fact]
        public void RemoveAllNullTest()
        {
            AssertExtensions.Throws<ArgumentNullException>("match", () => ImmutableList<int>.Empty.RemoveAll(null));
        }

        [Fact]
        public void RemoveRangeArrayTest()
        {
            Assert.True(ImmutableList<int>.Empty.RemoveRange(0, 0).IsEmpty);

            var list = ImmutableList.Create(1, 2, 3);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.RemoveRange(-1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => list.RemoveRange(0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list.RemoveRange(4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => list.RemoveRange(0, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => list.RemoveRange(2, 2));
            Assert.Equal(list, list.RemoveRange(3, 0));
        }

        [Fact]
        public void RemoveRange_EnumerableEqualityComparer_AcceptsNullEQ()
        {
            var list = ImmutableList.Create(1, 2, 3);
            var removed2eq = list.RemoveRange(new[] { 2 }, null);
            Assert.Equal(2, removed2eq.Count);
            Assert.Equal(new[] { 1, 3 }, removed2eq);
        }

        [Fact]
        public void RemoveRangeEnumerableTest()
        {
            var list = ImmutableList.Create(1, 2, 3);
            AssertExtensions.Throws<ArgumentNullException>("items", () => list.RemoveRange(null));

            ImmutableList<int> removed2 = list.RemoveRange(new[] { 2 });
            Assert.Equal(2, removed2.Count);
            Assert.Equal(new[] { 1, 3 }, removed2);

            ImmutableList<int> removed13 = list.RemoveRange(new[] { 1, 3, 5 });
            Assert.Equal(1, removed13.Count);
            Assert.Equal(new[] { 2 }, removed13);
            Assert.Equal(new[] { 2 }, ((IImmutableList<int>)list).RemoveRange(new[] { 1, 3, 5 }));

            Assert.Same(list, list.RemoveRange(new[] { 5 }));
            Assert.Same(ImmutableList.Create<int>(), ImmutableList.Create<int>().RemoveRange(new[] { 1 }));

            var listWithDuplicates = ImmutableList.Create(1, 2, 2, 3);
            Assert.Equal(new[] { 1, 2, 3 }, listWithDuplicates.RemoveRange(new[] { 2 }));
            Assert.Equal(new[] { 1, 3 }, listWithDuplicates.RemoveRange(new[] { 2, 2 }));

            AssertExtensions.Throws<ArgumentNullException>("items", () => ((IImmutableList<int>)ImmutableList.Create(1, 2, 3)).RemoveRange(null));
            Assert.Equal(new[] { 1, 3 }, ((IImmutableList<int>)ImmutableList.Create(1, 2, 3)).RemoveRange(new[] { 2 }));
        }

        [Fact]
        public void EnumeratorTest()
        {
            var list = ImmutableList.Create("a");
            var enumerator = list.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("a", enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("a", enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Dispose();
            Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
        }

        [Fact]
        public void EnumeratorRecyclingMisuse()
        {
            var collection = ImmutableList.Create(1);
            var enumerator = collection.GetEnumerator();
            var enumeratorCopy = enumerator;
            Assert.True(enumerator.MoveNext());
            enumerator.Dispose();
            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Current);
            enumerator.Dispose(); // double-disposal should not throw
            enumeratorCopy.Dispose();

            // We expect that acquiring a new enumerator will use the same underlying Stack<T> object,
            // but that it will not throw exceptions for the new enumerator.
            enumerator = collection.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(collection[0], enumerator.Current);
            enumerator.Dispose();
        }

        [Fact]
        public void ReverseTest2()
        {
            var emptyList = ImmutableList.Create<int>();
            Assert.Same(emptyList, emptyList.Reverse());

            var populatedList = ImmutableList.Create(3, 2, 1);
            Assert.Equal(Enumerable.Range(1, 3), populatedList.Reverse());
        }

        [Fact]
        public void SetItem()
        {
            var emptyList = ImmutableList.Create<int>();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => emptyList[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => emptyList[0]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => emptyList[1]);

            var listOfOne = emptyList.Add(5);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => listOfOne[-1]);
            Assert.Equal(5, listOfOne[0]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => listOfOne[1]);
        }

        [Fact]
        public void IsSynchronized()
        {
            ICollection collection = ImmutableList.Create<int>();
            Assert.True(collection.IsSynchronized);
        }

        [Fact]
        public void IListIsReadOnly()
        {
            IList list = ImmutableList.Create<int>();
            Assert.True(list.IsReadOnly);
            Assert.True(list.IsFixedSize);
            Assert.Throws<NotSupportedException>(() => list.Add(1));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, 1));
            Assert.Throws<NotSupportedException>(() => list.Remove(1));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = 1);
        }

        [Fact]
        public void IListOfTIsReadOnly()
        {
            IList<int> list = ImmutableList.Create<int>();
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(1));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, 1));
            Assert.Throws<NotSupportedException>(() => list.Remove(1));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = 1);
        }

        [Fact]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableList.Create<int>());
            ImmutableList<double> list = ImmutableList.Create<double>(1, 2, 3);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(list);

            object rootNode = DebuggerAttributes.GetFieldValue(ImmutableList.Create<string>("1", "2", "3"), "_root");
            DebuggerAttributes.ValidateDebuggerDisplayReferences(rootNode);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            double[] items = itemProperty.GetValue(info.Instance) as double[];
            Assert.Equal(list, items);
        }

        [Fact]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableList.Create<double>());
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

#if NETCOREAPP
        [Fact]
        public void UsableWithCollectibleAssemblies()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("dynamic_assembly"), AssemblyBuilderAccess.RunAndCollect);
            var module = assembly.DefineDynamicModule("dynamic");
            var typeBuilder = module.DefineType("Dummy");

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            var dummType = typeBuilder.CreateTypeInfo();

            var createMethod = typeof(ImmutableList).GetMethods().Where(m => m.Name == "Create" && m.GetParameters().Length == 0).Single().MakeGenericMethod(dummType.AsType());
            var list = (IEnumerable)createMethod.Invoke(null, null);

            var addMethod = list.GetType().GetMethod("Add");
            list = (IEnumerable)addMethod.Invoke(list, new object[] { Activator.CreateInstance(dummType.AsType()) });

            list.GetEnumerator(); // ensure this doesn't throw
        }
#endif

        [Fact]
        public void ItemRef()
        {
            var list = new[] { 1, 2, 3 }.ToImmutableList();

            ref readonly var safeRef = ref list.ItemRef(1);
            ref var unsafeRef = ref Unsafe.AsRef(safeRef);

            Assert.Equal(2, list.ItemRef(1));

            unsafeRef = 4;

            Assert.Equal(4, list.ItemRef(1));
        }

        [Fact]
        public void ItemRef_OutOfBounds()
        {
            var list = new[] { 1, 2, 3 }.ToImmutableList();

            Assert.Throws<ArgumentOutOfRangeException>(() => list.ItemRef(5));
        }

        protected override IEnumerable<T> GetEnumerableOf<T>(params T[] contents)
        {
            return ImmutableList<T>.Empty.AddRange(contents);
        }

        protected override void RemoveAllTestHelper<T>(ImmutableList<T> list, Predicate<T> test)
        {
            var expected = list.ToList();
            expected.RemoveAll(test);
            var actual = list.RemoveAll(test);
            Assert.Equal<T>(expected, actual.ToList());
        }

        protected override void ReverseTestHelper<T>(ImmutableList<T> list, int index, int count)
        {
            var expected = list.ToList();
            expected.Reverse(index, count);
            var actual = list.Reverse(index, count);
            Assert.Equal<T>(expected, actual.ToList());
        }

        protected override List<T> SortTestHelper<T>(ImmutableList<T> list)
        {
            return list.Sort().ToList();
        }

        protected override List<T> SortTestHelper<T>(ImmutableList<T> list, Comparison<T> comparison)
        {
            return list.Sort(comparison).ToList();
        }

        protected override List<T> SortTestHelper<T>(ImmutableList<T> list, IComparer<T> comparer)
        {
            return list.Sort(comparer).ToList();
        }

        protected override List<T> SortTestHelper<T>(ImmutableList<T> list, int index, int count, IComparer<T> comparer)
        {
            return list.Sort(index, count, comparer).ToList();
        }

        internal override IImmutableListQueries<T> GetListQuery<T>(ImmutableList<T> list)
        {
            return list;
        }

        private static void VerifyBalanced<T>(ImmutableList<T> tree)
        {
            tree.Root.VerifyBalanced();
        }

        private struct Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private class NameOnlyEqualityComparer : IEqualityComparer<Person>
        {
            public bool Equals(Person x, Person y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(Person obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
