// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
#nullable disable

    /// <summary>
    /// Contains tests that ensure the correctness of the HashSet class.
    /// </summary>
    public abstract class HashSet_Generic_Tests<T> : ISet_Generic_Tests<T>
    {
        #region ISet<T> Helper Methods

        private static bool IsNetFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

        protected override bool ResetImplemented => true;

        protected override ModifyOperation ModifyEnumeratorThrows => IsNetFramework ? base.ModifyEnumeratorThrows : (base.ModifyEnumeratorAllowed & ~(ModifyOperation.Remove | ModifyOperation.Clear));

        protected override ModifyOperation ModifyEnumeratorAllowed => IsNetFramework ? base.ModifyEnumeratorAllowed : ModifyOperation.Overwrite | ModifyOperation.Remove | ModifyOperation.Clear;

        protected override ISet<T> GenericISetFactory()
        {
            return new HashSet<T>();
        }

        #endregion

        #region Constructors

        private static IEnumerable<int> NonSquares(int limit)
        {
            for (int i = 0; i != limit; ++i)
            {
                int root = (int)Math.Sqrt(i);
                if (i != root * root)
                    yield return i;
            }
        }

        [Fact]
        public void HashSet_Generic_Constructor()
        {
            HashSet<T> set = new HashSet<T>();
            Assert.Empty(set);
        }

        [Fact]
        public void HashSet_Generic_Constructor_IEqualityComparer()
        {
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            HashSet<T> set = new HashSet<T>(comparer);
            if (comparer == null)
                Assert.Equal(EqualityComparer<T>.Default, set.Comparer);
            else
                Assert.Equal(comparer, set.Comparer);
        }

        [Fact]
        public void HashSet_Generic_Constructor_NullIEqualityComparer()
        {
            IEqualityComparer<T> comparer = null;
            HashSet<T> set = new HashSet<T>(comparer);
            if (comparer == null)
                Assert.Equal(EqualityComparer<T>.Default, set.Comparer);
            else
                Assert.Equal(comparer, set.Comparer);
        }

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        public void HashSet_Generic_Constructor_IEnumerable(EnumerableType enumerableType, int setLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            _ = setLength;
            _ = numberOfMatchingElements;
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, numberOfDuplicateElements);
            HashSet<T> set = new HashSet<T>(enumerable);
            Assert.True(set.SetEquals(enumerable));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_IEnumerable_WithManyDuplicates(int count)
        {
            IEnumerable<T> items = CreateEnumerable(EnumerableType.List, null, count, 0, 0);
            HashSet<T> hashSetFromDuplicates = new HashSet<T>(Enumerable.Range(0, 40).SelectMany(i => items).ToArray());
            HashSet<T> hashSetFromNoDuplicates = new HashSet<T>(items);
            Assert.True(hashSetFromNoDuplicates.SetEquals(hashSetFromDuplicates));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_HashSet_SparselyFilled(int count)
        {
            HashSet<T> source = (HashSet<T>)CreateEnumerable(EnumerableType.HashSet, null, count, 0, 0);
            List<T> sourceElements = source.ToList();
            foreach (int i in NonSquares(count))
                source.Remove(sourceElements[i]);// Unevenly spaced survivors increases chance of catching any spacing-related bugs.


            HashSet<T> set = new HashSet<T>(source, GetIEqualityComparer());
            Assert.True(set.SetEquals(source));
        }

        [Fact]
        public void HashSet_Generic_Constructor_IEnumerable_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new HashSet<T>((IEnumerable<T>)null));
            Assert.Throws<ArgumentNullException>(() => new HashSet<T>((IEnumerable<T>)null, EqualityComparer<T>.Default));
        }

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        public void HashSet_Generic_Constructor_IEnumerable_IEqualityComparer(EnumerableType enumerableType, int setLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            _ = setLength;
            _ = numberOfMatchingElements;
            _ = numberOfDuplicateElements;
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, 0);
            HashSet<T> set = new HashSet<T>(enumerable, GetIEqualityComparer());
            Assert.True(set.SetEquals(enumerable));
        }

        #endregion

        #region RemoveWhere

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_RemoveWhere_AllElements(int setLength)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
            int removedCount = set.RemoveWhere((value) => { return true; });
            Assert.Equal(setLength, removedCount);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_RemoveWhere_NoElements(int setLength)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
            int removedCount = set.RemoveWhere((value) => { return false; });
            Assert.Equal(0, removedCount);
            Assert.Equal(setLength, set.Count);
        }

        [Fact]
        public void HashSet_Generic_RemoveWhere_NewObject() // Regression Dev10_624201
        {
            object[] array = new object[2];
            object obj = new object();
            HashSet<object> set = new HashSet<object>();

            set.Add(obj);
            set.Remove(obj);
            foreach (object o in set) { }
            set.CopyTo(array, 0, 2);
            set.RemoveWhere((element) => { return false; });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_RemoveWhere_NullMatchPredicate(int setLength)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
            Assert.Throws<ArgumentNullException>(() => set.RemoveWhere(null));
        }

        #endregion

        #region TrimExcess

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_TrimExcess_OnValidSetThatHasntBeenRemovedFrom(int setLength)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
            set.TrimExcess();
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_TrimExcess_Repeatedly(int setLength)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
            List<T> expected = set.ToList();
            set.TrimExcess();
            set.TrimExcess();
            set.TrimExcess();
            Assert.True(set.SetEquals(expected));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_TrimExcess_AfterRemovingOneElement(int setLength)
        {
            if (setLength > 0)
            {
                HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
                List<T> expected = set.ToList();
                T elementToRemove = set.ElementAt(0);

                set.TrimExcess();
                Assert.True(set.Remove(elementToRemove));
                expected.Remove(elementToRemove);
                set.TrimExcess();

                Assert.True(set.SetEquals(expected));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_TrimExcess_AfterClearingAndAddingSomeElementsBack(int setLength)
        {
            if (setLength > 0)
            {
                HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
                set.TrimExcess();
                set.Clear();
                set.TrimExcess();
                Assert.Equal(0, set.Count);

                AddToCollection(set, setLength / 10);
                set.TrimExcess();
                Assert.Equal(setLength / 10, set.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_TrimExcess_AfterClearingAndAddingAllElementsBack(int setLength)
        {
            if (setLength > 0)
            {
                HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
                set.TrimExcess();
                set.Clear();
                set.TrimExcess();
                Assert.Equal(0, set.Count);

                AddToCollection(set, setLength);
                set.TrimExcess();
                Assert.Equal(setLength, set.Count);
            }
        }

        #endregion

        #region CopyTo

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_CopyTo_NegativeCount_ThrowsArgumentOutOfRangeException(int count)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(count);
            T[] arr = new T[count];
            Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(arr, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(arr, 0, int.MinValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_CopyTo_NoIndexDefaultsToZero(int count)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(count);
            T[] arr1 = new T[count];
            T[] arr2 = new T[count];
            set.CopyTo(arr1);
            set.CopyTo(arr2, 0);
            Assert.True(arr1.SequenceEqual(arr2));
        }

        #endregion

        #region CreateSetComparer

        [Fact]
        public void SetComparer_SetEqualsTests()
        {
            List<T> objects = new List<T>() { CreateT(1), CreateT(2), CreateT(3), CreateT(4), CreateT(5), CreateT(6) };

            var set = new HashSet<HashSet<T>>()
            {
                new HashSet<T> { objects[0], objects[1], objects[2] },
                new HashSet<T> { objects[3], objects[4], objects[5] }
            };

            var noComparerSet = new HashSet<HashSet<T>>()
            {
                new HashSet<T> { objects[0], objects[1], objects[2] },
                new HashSet<T> { objects[3], objects[4], objects[5] }
            };

            var comparerSet1 = new HashSet<HashSet<T>>(HashSet<T>.CreateSetComparer())
            {
                new HashSet<T> { objects[0], objects[1], objects[2] },
                new HashSet<T> { objects[3], objects[4], objects[5] }
            };

            var comparerSet2 = new HashSet<HashSet<T>>(HashSet<T>.CreateSetComparer())
            {
                new HashSet<T> { objects[3], objects[4], objects[5] },
                new HashSet<T> { objects[0], objects[1], objects[2] }
            };

            Assert.False(noComparerSet.SetEquals(set));
            Assert.True(comparerSet1.SetEquals(set));
            Assert.True(comparerSet2.SetEquals(set));
        }

        [Fact]
        public void SetComparer_SequenceEqualTests()
        {
            List<T> objects = new List<T>() { CreateT(1), CreateT(2), CreateT(3), CreateT(4), CreateT(5), CreateT(6) };

            var set = new HashSet<HashSet<T>>()
            {
                new HashSet<T> { objects[0], objects[1], objects[2] },
                new HashSet<T> { objects[3], objects[4], objects[5] }
            };

            var noComparerSet = new HashSet<HashSet<T>>()
            {
                new HashSet<T> { objects[0], objects[1], objects[2] },
                new HashSet<T> { objects[3], objects[4], objects[5] }
            };

            var comparerSet = new HashSet<HashSet<T>>(HashSet<T>.CreateSetComparer())
            {
                new HashSet<T> { objects[0], objects[1], objects[2] },
                new HashSet<T> { objects[3], objects[4], objects[5] }
            };

            Assert.False(noComparerSet.SequenceEqual(set));
            Assert.True(noComparerSet.SequenceEqual(set, HashSet<T>.CreateSetComparer()));
            Assert.False(comparerSet.SequenceEqual(set));
        }

        #endregion

        [Fact]
        public void CanBeCastedToISet()
        {
            HashSet<T> set = new HashSet<T>();
            ISet<T> iset = (set as ISet<T>);
            Assert.NotNull(iset);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_int(int capacity)
        {
            HashSet<T> set = new HashSet<T>(capacity);
            Assert.Equal(0, set.Count);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_int_AddUpToAndBeyondCapacity(int capacity)
        {
            HashSet<T> set = new HashSet<T>(capacity);

            AddToCollection(set, capacity);
            Assert.Equal(capacity, set.Count);

            AddToCollection(set, capacity + 1);
            Assert.Equal(capacity + 1, set.Count);
        }

        [Fact]
        public void HashSet_Generic_Constructor_int_Negative_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(int.MinValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_int_IEqualityComparer(int capacity)
        {
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            HashSet<T> set = new HashSet<T>(capacity, comparer);
            Assert.Equal(0, set.Count);
            if (comparer == null)
                Assert.Equal(EqualityComparer<T>.Default, set.Comparer);
            else
                Assert.Equal(comparer, set.Comparer);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_int_IEqualityComparer_AddUpToAndBeyondCapacity(int capacity)
        {
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            HashSet<T> set = new HashSet<T>(capacity, comparer);

            AddToCollection(set, capacity);
            Assert.Equal(capacity, set.Count);

            AddToCollection(set, capacity + 1);
            Assert.Equal(capacity + 1, set.Count);
        }

        [Fact]
        public void HashSet_Generic_Constructor_int_IEqualityComparer_Negative_ThrowsArgumentOutOfRangeException()
        {
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(-1, comparer));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(int.MinValue, comparer));
        }

        #region TryGetValue

        [Fact]
        public void HashSet_Generic_TryGetValue_Contains()
        {
            T value = CreateT(1);
            HashSet<T> set = new HashSet<T> { value };
            T equalValue = CreateT(1);
            T actualValue;
            Assert.True(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(value, actualValue);
            if (!typeof(T).IsValueType)
            {
                Assert.Same((object)value, (object)actualValue);
            }
        }

        [Fact]
        public void HashSet_Generic_TryGetValue_Contains_OverwriteOutputParam()
        {
            T value = CreateT(1);
            HashSet<T> set = new HashSet<T> { value };
            T equalValue = CreateT(1);
            T actualValue = CreateT(2);
            Assert.True(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(value, actualValue);
            if (!typeof(T).IsValueType)
            {
                Assert.Same((object)value, (object)actualValue);
            }
        }

        [Fact]
        public void HashSet_Generic_TryGetValue_NotContains()
        {
            T value = CreateT(1);
            HashSet<T> set = new HashSet<T> { value };
            T equalValue = CreateT(2);
            T actualValue;
            Assert.False(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(default(T), actualValue);
        }

        [Fact]
        public void HashSet_Generic_TryGetValue_NotContains_OverwriteOutputParam()
        {
            T value = CreateT(1);
            HashSet<T> set = new HashSet<T> { value };
            T equalValue = CreateT(2);
            T actualValue = equalValue;
            Assert.False(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(default(T), actualValue);
        }

        #endregion

        #region Remove

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void Remove_NonDefaultComparer_ComparerUsed(int capacity)
        {
            var c = new TrackingEqualityComparer<T>();
            var set = new HashSet<T>(c);

            AddToCollection(set, capacity);
            T first = set.First();
            c.EqualsCalls = 0;
            c.GetHashCodeCalls = 0;

            Assert.Equal(capacity, set.Count);
            set.Remove(first);
            Assert.Equal(capacity - 1, set.Count);

            Assert.InRange(c.EqualsCalls, 1, int.MaxValue);
            Assert.InRange(c.GetHashCodeCalls, 1, int.MaxValue);
        }

        #endregion
    }
}
