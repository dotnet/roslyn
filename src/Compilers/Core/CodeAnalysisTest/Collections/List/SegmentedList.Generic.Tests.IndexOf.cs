// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v8.0.3/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.IndexOf.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class SegmentedList_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        #region Helpers

        internal delegate int IndexOfDelegate(SegmentedList<T> list, T value);
        public enum IndexOfMethod
        {
            IndexOf_T,
            IndexOf_T_int,
            IndexOf_T_int_int,
            LastIndexOf_T,
            LastIndexOf_T_int,
            LastIndexOf_T_int_int,
        };

        private IndexOfDelegate IndexOfDelegateFromType(IndexOfMethod methodType)
        {
            switch (methodType)
            {
                case (IndexOfMethod.IndexOf_T):
                    return ((SegmentedList<T> list, T value) => { return list.IndexOf(value); });
                case (IndexOfMethod.IndexOf_T_int):
                    return ((SegmentedList<T> list, T value) => { return list.IndexOf(value, 0); });
                case (IndexOfMethod.IndexOf_T_int_int):
                    return ((SegmentedList<T> list, T value) => { return list.IndexOf(value, 0, list.Count); });
                case (IndexOfMethod.LastIndexOf_T):
                    return ((SegmentedList<T> list, T value) => { return list.LastIndexOf(value); });
                case (IndexOfMethod.LastIndexOf_T_int):
                    return ((SegmentedList<T> list, T value) => { return list.LastIndexOf(value, list.Count - 1); });
                case (IndexOfMethod.LastIndexOf_T_int_int):
                    return ((SegmentedList<T> list, T value) => { return list.LastIndexOf(value, list.Count - 1, list.Count); });
                default:
                    throw new Exception("Invalid IndexOfMethod");
            }
        }

        /// <summary>
        /// MemberData for a Theory to test the IndexOf methods for List. To avoid high code reuse of tests for the 6 IndexOf
        /// methods in List, delegates are used to cover the basic behavioral cases shared by all IndexOf methods. A bool
        /// is used to specify the ordering (front-to-back or back-to-front (e.g. LastIndexOf)) that the IndexOf method
        /// searches in.
        /// </summary>
        public static IEnumerable<object[]> IndexOfTestData()
        {
            foreach (object[] sizes in ValidCollectionSizes())
            {
                int count = (int)sizes[0];
                yield return new object[] { IndexOfMethod.IndexOf_T, count, true };
                yield return new object[] { IndexOfMethod.LastIndexOf_T, count, false };

                if (count > 0) // 0 is an invalid index for IndexOf when the count is 0.
                {
                    yield return new object[] { IndexOfMethod.IndexOf_T_int, count, true };
                    yield return new object[] { IndexOfMethod.LastIndexOf_T_int, count, false };
                    yield return new object[] { IndexOfMethod.IndexOf_T_int_int, count, true };
                    yield return new object[] { IndexOfMethod.LastIndexOf_T_int_int, count, false };
                }
            }
        }

        #endregion

        #region IndexOf

        [Theory]
        [MemberData(nameof(IndexOfTestData))]
        public void IndexOf_NoDuplicates(IndexOfMethod indexOfMethod, int count, bool frontToBackOrder)
        {
            _ = frontToBackOrder;
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> expectedList = list.ToSegmentedList();
            IndexOfDelegate IndexOf = IndexOfDelegateFromType(indexOfMethod);

            Assert.All(Enumerable.Range(0, count), i =>
            {
                Assert.Equal(i, IndexOf(list, expectedList[i]));
            });
        }

        [Theory]
        [MemberData(nameof(IndexOfTestData))]
        public void IndexOf_NonExistingValues(IndexOfMethod indexOfMethod, int count, bool frontToBackOrder)
        {
            _ = frontToBackOrder;
            SegmentedList<T> list = GenericListFactory(count);
            IEnumerable<T> nonexistentValues = CreateEnumerable(EnumerableType.List, list, count: count, numberOfMatchingElements: 0, numberOfDuplicateElements: 0);
            IndexOfDelegate IndexOf = IndexOfDelegateFromType(indexOfMethod);

            Assert.All(nonexistentValues, nonexistentValue =>
            {
                Assert.Equal(-1, IndexOf(list, nonexistentValue));
            });
        }

        [Theory]
        [MemberData(nameof(IndexOfTestData))]
        public void IndexOf_DefaultValue(IndexOfMethod indexOfMethod, int count, bool frontToBackOrder)
        {
            _ = frontToBackOrder;
            T? defaultValue = default;
            SegmentedList<T?> list = GenericListFactory(count)!;
            IndexOfDelegate IndexOf = IndexOfDelegateFromType(indexOfMethod);
            while (list.Remove(defaultValue))
                count--;
            list.Add(defaultValue);
            Assert.Equal(count, IndexOf(list!, defaultValue!));
        }

        [Theory]
        [MemberData(nameof(IndexOfTestData))]
        public void IndexOf_OrderIsCorrect(IndexOfMethod indexOfMethod, int count, bool frontToBackOrder)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> withoutDuplicates = list.ToSegmentedList();
            list.AddRange(list);
            IndexOfDelegate IndexOf = IndexOfDelegateFromType(indexOfMethod);

            Assert.All(Enumerable.Range(0, count), i =>
            {
                if (frontToBackOrder)
                    Assert.Equal(i, IndexOf(list, withoutDuplicates[i]));
                else
                    Assert.Equal(count + i, IndexOf(list, withoutDuplicates[i]));
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IndexOf_int_OrderIsCorrectWithManyDuplicates(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> withoutDuplicates = list.ToSegmentedList();
            list.AddRange(list);
            list.AddRange(list);
            list.AddRange(list);

            Assert.All(Enumerable.Range(0, count), i =>
            {
                Assert.All(Enumerable.Range(0, 4), j =>
                {
                    int expectedIndex = (j * count) + i;
                    Assert.Equal(expectedIndex, list.IndexOf(withoutDuplicates[i], (count * j)));
                    Assert.Equal(expectedIndex, list.IndexOf(withoutDuplicates[i], (count * j), count));
                });
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void LastIndexOf_int_OrderIsCorrectWithManyDuplicates(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> withoutDuplicates = list.ToSegmentedList();
            list.AddRange(list);
            list.AddRange(list);
            list.AddRange(list);

            Assert.All(Enumerable.Range(0, count), i =>
            {
                Assert.All(Enumerable.Range(0, 4), j =>
                {
                    int expectedIndex = (j * count) + i;
                    Assert.Equal(expectedIndex, list.LastIndexOf(withoutDuplicates[i], (count * (j + 1)) - 1));
                    Assert.Equal(expectedIndex, list.LastIndexOf(withoutDuplicates[i], (count * (j + 1)) - 1, count));
                });
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IndexOf_int_OutOfRangeExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            T element = CreateT(234);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count + 1)); //"Expect ArgumentOutOfRangeException for index greater than length of list.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count + 10)); //"Expect ArgumentOutOfRangeException for index greater than length of list.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, -1)); //"Expect ArgumentOutOfRangeException for negative index."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, int.MinValue)); //"Expect ArgumentOutOfRangeException for negative index."
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IndexOf_int_int_OutOfRangeExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            T element = CreateT(234);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count, 1)); //"ArgumentOutOfRangeException expected on index larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count + 1, 1)); //"ArgumentOutOfRangeException expected  on index larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, 0, count + 1)); //"ArgumentOutOfRangeException expected  on count larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count / 2, count / 2 + 2)); //"ArgumentOutOfRangeException expected.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, 0, count + 1)); //"ArgumentOutOfRangeException expected  on count larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, 0, -1)); //"ArgumentOutOfRangeException expected on negative count."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, -1, 1)); //"ArgumentOutOfRangeException expected on negative index."
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void LastIndexOf_int_OutOfRangeExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            T element = CreateT(234);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, count)); //"ArgumentOutOfRangeException expected."
            if (count == 0)  // IndexOf with a 0 count List is special cased to return -1.
                Assert.Equal(-1, list.LastIndexOf(element, -1));
            else
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, -1));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void LastIndexOf_int_int_OutOfRangeExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            T element = CreateT(234);

            if (count > 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, 0, count + 1)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, count / 2, count / 2 + 2)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, 0, count + 1)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, 0, -1)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, -1, count)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, -1, 1)); //"Expected ArgumentOutOfRangeException."                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, count, 0)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, count, 1)); //"Expected ArgumentOutOfRangeException."
            }
            else // IndexOf with a 0 count List is special cased to return -1.
            {
                Assert.Equal(-1, list.LastIndexOf(element, 0, count + 1));
                Assert.Equal(-1, list.LastIndexOf(element, count / 2, count / 2 + 2));
                Assert.Equal(-1, list.LastIndexOf(element, 0, count + 1));
                Assert.Equal(-1, list.LastIndexOf(element, 0, -1));
                Assert.Equal(-1, list.LastIndexOf(element, -1, count));
                Assert.Equal(-1, list.LastIndexOf(element, -1, 1));
                Assert.Equal(-1, list.LastIndexOf(element, count, 0));
                Assert.Equal(-1, list.LastIndexOf(element, count, 1));
            }
        }

        #endregion
    }
}
