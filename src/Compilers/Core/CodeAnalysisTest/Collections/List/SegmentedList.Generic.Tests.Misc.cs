// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.Misc.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public class SegmentedList_Generic_Tests_Insert
    {
        internal class Driver<T>
        {
            public Func<T?[], IEnumerable<T?>>[] CollectionGenerators { get; }

            public Driver()
            {
                CollectionGenerators = new Func<T?[], IEnumerable<T?>>[]
                {
                    ConstructTestList,
                    ConstructTestEnumerable,
                    ConstructLazyTestEnumerable,
                };
            }

            #region Insert

            public void BasicInsert(T?[] items, T? item, int index, int repeat)
            {
                SegmentedList<T?> list = new SegmentedList<T?>(items);

                for (int i = 0; i < repeat; i++)
                {
                    list.Insert(index, item);
                }

                Assert.True(list.Contains(item)); //"Expect it to contain the item."
                Assert.Equal(list.Count, items.Length + repeat); //"Expect to be the same."

                for (int i = 0; i < index; i++)
                {
                    Assert.Equal(list[i], items[i]); //"Expect to be the same."
                }

                for (int i = index; i < index + repeat; i++)
                {
                    Assert.Equal(list[i], item); //"Expect to be the same."
                }

                for (int i = index + repeat; i < list.Count; i++)
                {
                    Assert.Equal(list[i], items[i - repeat]); //"Expect to be the same."
                }
            }

            public void InsertValidations(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                int[] bad = new int[] { items.Length + 1, items.Length + 2, int.MaxValue, -1, -2, int.MinValue };
                for (int i = 0; i < bad.Length; i++)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(bad[i], items[0])); //"ArgumentOutOfRangeException expected."
                }
            }

            public void NonGenericIListBasicInsert(T?[] items, T? item, int index, int repeat)
            {
                SegmentedList<T?> list = new SegmentedList<T?>(items);
                IList _ilist = list;

                for (int i = 0; i < repeat; i++)
                {
                    _ilist.Insert(index, item);
                }

                Assert.True(list.Contains(item)); //"Expected it to be true."
                Assert.Equal(list.Count, items.Length + repeat); //"Expected them to be equal."

                for (int i = 0; i < index; i++)
                {
                    Assert.Equal(list[i], items[i]); //"Expected them to be equal."
                }

                for (int i = index; i < index + repeat; i++)
                {
                    Assert.Equal((object?)list[i], item); //"Expected them to be equal."
                }

                for (int i = index + repeat; i < list.Count; i++)
                {
                    Assert.Equal(list[i], items[i - repeat]); //"Expected them to be equal."
                }
            }

            public void NonGenericIListInsertValidations(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                IList _ilist = list;
                int[] bad = new int[] { items.Length + 1, items.Length + 2, int.MaxValue, -1, -2, int.MinValue };
                for (int i = 0; i < bad.Length; i++)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => _ilist.Insert(bad[i], items[0])); //"ArgumentOutOfRangeException expected."
                }

                Assert.Throws<ArgumentException>("value", () => _ilist.Insert(0, new LinkedListNode<string>("blargh"))); //"ArgumentException expected."
            }

            #endregion

            #region InsertRange

            public void InsertRangeIEnumerable(T?[] itemsX, T?[] itemsY, int index, int repeat, Func<T?[], IEnumerable<T?>> constructIEnumerable)
            {
                SegmentedList<T?> list = new SegmentedList<T?>(constructIEnumerable(itemsX));

                for (int i = 0; i < repeat; i++)
                {
                    list.InsertRange(index, constructIEnumerable(itemsY));
                }

                foreach (T? item in itemsY)
                {
                    Assert.True(list.Contains(item)); //"Should contain the item."
                }
                Assert.Equal(list.Count, itemsX.Length + (itemsY.Length * repeat)); //"Should have the same result."

                for (int i = 0; i < index; i++)
                {
                    Assert.Equal(list[i], itemsX[i]); //"Should have the same result."
                }

                for (int i = index; i < index + (itemsY.Length * repeat); i++)
                {
                    Assert.Equal(list[i], itemsY[(i - index) % itemsY.Length]); //"Should have the same result."
                }

                for (int i = index + (itemsY.Length * repeat); i < list.Count; i++)
                {
                    Assert.Equal(list[i], itemsX[i - (itemsY.Length * repeat)]); //"Should have the same result."
                }

                //InsertRange into itself
                list = new SegmentedList<T?>(constructIEnumerable(itemsX));
                list.InsertRange(index, list);

                foreach (T? item in itemsX)
                {
                    Assert.True(list.Contains(item)); //"Should contain the item."
                }
                Assert.Equal(list.Count, itemsX.Length + (itemsX.Length)); //"Should have the same result."

                for (int i = 0; i < index; i++)
                {
                    Assert.Equal(list[i], itemsX[i]); //"Should have the same result."
                }

                for (int i = index; i < index + (itemsX.Length); i++)
                {
                    Assert.Equal(list[i], itemsX[(i - index) % itemsX.Length]); //"Should have the same result."
                }

                for (int i = index + (itemsX.Length); i < list.Count; i++)
                {
                    Assert.Equal(list[i], itemsX[i - (itemsX.Length)]); //"Should have the same result."
                }
            }

            public void InsertRangeValidations(T?[] items, Func<T?[], IEnumerable<T?>> constructIEnumerable)
            {
                SegmentedList<T?> list = new SegmentedList<T?>(constructIEnumerable(items));
                int[] bad = new int[] { items.Length + 1, items.Length + 2, int.MaxValue, -1, -2, int.MinValue };
                for (int i = 0; i < bad.Length; i++)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(bad[i], constructIEnumerable(items))); //"ArgumentOutOfRangeException expected"
                }

                Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, null!)); //"ArgumentNullException expected."
            }

            public IEnumerable<T?> ConstructTestEnumerable(T?[] items)
            {
                return items;
            }

            public IEnumerable<T?> ConstructLazyTestEnumerable(T?[] items)
            {
                return ConstructTestEnumerable(items)
                    .Select(item => item);
            }

            public IEnumerable<T?> ConstructTestList(T?[] items)
            {
                return items.ToList();
            }

            #endregion

            #region GetRange

            public void BasicGetRange(T[] items, int index, int count)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                SegmentedList<T> range = list.GetRange(index, count);

                //ensure range is good
                for (int i = 0; i < count; i++)
                {
                    Assert.Equal(range[i], items[i + index]); //String.Format("Err_170178aqhbpa Expected item: {0} at: {1} actual: {2}", items[i + index], i, range[i])
                }

                //ensure no side effects
                for (int i = 0; i < items.Length; i++)
                {
                    Assert.Equal(list[i], items[i]); //String.Format("Err_00125698ahpap Expected item: {0} at: {1} actual: {2}", items[i], i, list[i])
                }
            }

            public void EnsureRangeIsReference(T[] items, T item, int index, int count)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                SegmentedList<T> range = list.GetRange(index, count);
                T tempItem = list[index];
                range[0] = item;
                Assert.Equal(list[index], tempItem); //String.Format("Err_707811hapba Expected item: {0} at: {1} actual: {2}", tempItem, index, list[index])
            }

            public void EnsureThrowsAfterModification(T[] items, T item, int index, int count)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                SegmentedList<T> range = list.GetRange(index, count);
                T tempItem = list[index];
                list[index] = item;

                Assert.Equal(range[0], tempItem); //String.Format("Err_1221589ajpa Expected item: {0} at: {1} actual: {2}", tempItem, 0, range[0])
            }

            public void GetRangeValidations(T[] items)
            {
                //
                //Always send items.Length is even
                //
                SegmentedList<T> list = new SegmentedList<T>(items);
                int[] bad = new int[] {  /**/items.Length,1,
                    /**/
                                    items.Length+1,0,
                    /**/
                                    items.Length+1,1,
                    /**/
                                    items.Length,2,
                    /**/
                                    items.Length/2,items.Length/2+1,
                    /**/
                                    items.Length-1,2,
                    /**/
                                    items.Length-2,3,
                    /**/
                                    1,items.Length,
                    /**/
                                    0,items.Length+1,
                    /**/
                                    1,items.Length+1,
                    /**/
                                    2,items.Length,
                    /**/
                                    items.Length/2+1,items.Length/2,
                    /**/
                                    2,items.Length-1,
                    /**/
                                    3,items.Length-2
                                };

                for (int i = 0; i < bad.Length; i++)
                {
                    Assert.Throws<ArgumentException>(null, () => list.GetRange(bad[i], bad[++i])); //"ArgumentException expected."
                }

                bad = new int[] {
                    /**/
                                    -1,-1,
                    /**/
                                    -1,0,
                    /**/
                                    -1,1,
                    /**/
                                    -1,2,
                    /**/
                                    0,-1,
                    /**/
                                    1,-1,
                    /**/
                                    2,-1
                                };

                for (int i = 0; i < bad.Length; i++)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => list.GetRange(bad[i], bad[++i])); //"ArgumentOutOfRangeException expected."
                }
            }

            #endregion

            #region Exists(Pred<T>)

            public void Exists_Verify(T[] items)
            {
                Exists_VerifyVanilla(items);
                Exists_VerifyDuplicates(items);
            }

            public void Exists_VerifyExceptions(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>();
                Predicate<T> predicate = (T item) => { return true; };

                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                //[] Verify Null match
                Assert.Throws<ArgumentNullException>(() => list.Exists(null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"
            }

            private void Exists_VerifyVanilla(T[] items)
            {
                T? expectedItem = default(T);
                SegmentedList<T?> list = new SegmentedList<T?>();
                Predicate<T?> expectedItemDelegate = (T? item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };
                bool typeNullable = default(T) == null;

                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                //[] Verify Exists returns the correct index
                for (int i = 0; i < items.Length; ++i)
                {
                    expectedItem = items[i];

                    Assert.True(list.Exists(expectedItemDelegate),
                        "Err_282308ahid Verifying Nullable returned FAILED\n");
                }

                //[] Verify Exists returns true if the match returns true on every item
                Assert.True((0 < items.Length) == list.Exists((T? item) => { return true; }),
                        "Err_548ahid Verify Exists returns 0 if the match returns true on every item FAILED\n");

                //[] Verify Exists returns false if the match returns false on every item
                Assert.True(!list.Exists((T? item) => { return false; }),
                        "Err_30848ahidi Verify Exists returns -1 if the match returns false on every item FAILED\n");

                //[] Verify with default(T)
                list.Add(default(T));
                Assert.True(list.Exists((T? item) => { return item == null ? default(T) == null : item.Equals(default(T)); }),
                        "Err_541848ajodi Verify with default(T) FAILED\n");
                list.RemoveAt(list.Count - 1);
            }

            private void Exists_VerifyDuplicates(T[] items)
            {
                T? expectedItem = default(T);
                SegmentedList<T> list = new SegmentedList<T>();
                Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

                if (0 < items.Length)
                {
                    for (int i = 0; i < items.Length; ++i)
                        list.Add(items[i]);

                    for (int i = 0; i < items.Length && i < 2; ++i)
                        list.Add(items[i]);

                    //[] Verify first item is duplicated
                    expectedItem = items[0];
                    Assert.True(list.Exists(expectedItemDelegate),
                            "Err_2879072qaiadf  Verify first item is duplicated FAILED\n");
                }

                if (1 < items.Length)
                {
                    //[] Verify second item is duplicated
                    expectedItem = items[1];
                    Assert.True(list.Exists(expectedItemDelegate),
                            "Err_4588ajdia Verify second item is duplicated FAILED\n");

                    //[] Verify with match that matches more then one item
                    Assert.True(list.Exists((T item) => { return item != null && (item.Equals(items[0]) || item.Equals(items[1])); }),
                            "Err_4489ajodoi Verify with match that matches more then one item FAILED\n");
                }
            }

            #endregion

            #region Contains

            public void BasicContains(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);

                for (int i = 0; i < items.Length; i++)
                {
                    Assert.True(list.Contains(items[i])); //"Should contain item."
                }
            }

            public void NonExistingValues(T[] itemsX, T[] itemsY)
            {
                SegmentedList<T> list = new SegmentedList<T>(itemsX);

                for (int i = 0; i < itemsY.Length; i++)
                {
                    Assert.False(list.Contains(itemsY[i])); //"Should not contain item"
                }
            }

            public void RemovedValues(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                for (int i = 0; i < items.Length; i++)
                {
                    list.Remove(items[i]);
                    Assert.False(list.Contains(items[i])); //"Should not contain item"
                }
            }

            public void AddRemoveValues(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                for (int i = 0; i < items.Length; i++)
                {
                    list.Add(items[i]);
                    list.Remove(items[i]);
                    list.Add(items[i]);
                    Assert.True(list.Contains(items[i])); //"Should contain item."
                }
            }

            public void MultipleValues(T[] items, int times)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);

                for (int i = 0; i < times; i++)
                {
                    list.Add(items[items.Length / 2]);
                }

                for (int i = 0; i < times + 1; i++)
                {
                    Assert.True(list.Contains(items[items.Length / 2])); //"Should contain item."
                    list.Remove(items[items.Length / 2]);
                }
                Assert.False(list.Contains(items[items.Length / 2])); //"Should not contain item"
            }
            public void ContainsNullWhenReference(T?[] items, T? value)
            {
                if ((object?)value != null)
                {
                    throw new ArgumentException("invalid argument passed to testcase");
                }

                SegmentedList<T?> list = new SegmentedList<T?>(items);
                list.Add(value);
                Assert.True(list.Contains(value)); //"Should contain item."
            }

            public void NonGenericIListBasicContains(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                IList _ilist = list;

                for (int i = 0; i < items.Length; i++)
                {
                    Assert.True(_ilist.Contains(items[i])); //"Should contain item."
                }
            }

            public void NonGenericIListNonExistingValues(T[] itemsX, T[] itemsY)
            {
                SegmentedList<T> list = new SegmentedList<T>(itemsX);
                IList _ilist = list;

                for (int i = 0; i < itemsY.Length; i++)
                {
                    Assert.False(_ilist.Contains(itemsY[i])); //"Should not contain item"
                }
            }

            public void NonGenericIListRemovedValues(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                IList _ilist = list;
                for (int i = 0; i < items.Length; i++)
                {
                    list.Remove(items[i]);
                    Assert.False(_ilist.Contains(items[i])); //"Should not contain item"
                }
            }

            public void NonGenericIListAddRemoveValues(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                IList _ilist = list;
                for (int i = 0; i < items.Length; i++)
                {
                    list.Add(items[i]);
                    list.Remove(items[i]);
                    list.Add(items[i]);
                    Assert.True(_ilist.Contains(items[i])); //"Should contain item."
                }
            }

            public void NonGenericIListMultipleValues(T[] items, int times)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                IList _ilist = list;

                for (int i = 0; i < times; i++)
                {
                    list.Add(items[items.Length / 2]);
                }

                for (int i = 0; i < times + 1; i++)
                {
                    Assert.True(_ilist.Contains(items[items.Length / 2])); //"Should contain item."
                    list.Remove(items[items.Length / 2]);
                }
                Assert.False(_ilist.Contains(items[items.Length / 2])); //"Should not contain item"
            }

            public void NonGenericIListContainsNullWhenReference(T?[] items, T? value)
            {
                if ((object?)value != null)
                {
                    throw new ArgumentException("invalid argument passed to testcase");
                }

                SegmentedList<T?> list = new SegmentedList<T?>(items);
                IList _ilist = list;
                list.Add(value);
                Assert.True(_ilist.Contains(value)); //"Should contain item."
            }

            public void NonGenericIListContainsTestParams()
            {
                SegmentedList<T> list = new SegmentedList<T>();
                IList _ilist = list;

                Assert.False(_ilist.Contains(new LinkedListNode<string>("rah")),
                    "Err_68850ahiuedpz Expected Contains to return false with invalid type");
            }

            #endregion

            #region Clear

            public void ClearEmptyList()
            {
                SegmentedList<T> list = new SegmentedList<T>();
                Assert.Equal(0, list.Count); //"Should be equal to 0"
                list.Clear();
                Assert.Equal(0, list.Count); //"Should be equal to 0."
            }
            public void ClearMultipleTimesEmptyList(int times)
            {
                SegmentedList<T> list = new SegmentedList<T>();
                Assert.Equal(0, list.Count); //"Should be equal to 0."
                for (int i = 0; i < times; i++)
                {
                    list.Clear();
                    Assert.Equal(0, list.Count); //"Should be equal to 0."
                }
            }
            public void ClearNonEmptyList(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                list.Clear();
                Assert.Equal(0, list.Count); //"Should be equal to 0."
            }

            public void ClearMultipleTimesNonEmptyList(T[] items, int times)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                for (int i = 0; i < times; i++)
                {
                    list.Clear();
                    Assert.Equal(0, list.Count); //"Should be equal to 0."
                }
            }

            public void NonGenericIListClearEmptyList()
            {
                SegmentedList<T> list = new SegmentedList<T>();
                IList _ilist = list;
                Assert.Equal(0, list.Count); //"Should be equal to 0."
                _ilist.Clear();
                Assert.Equal(0, list.Count); //"Should be equal to 0."
            }
            public void NonGenericIListClearMultipleTimesEmptyList(int times)
            {
                SegmentedList<T> list = new SegmentedList<T>();
                IList _ilist = list;
                Assert.Equal(0, list.Count); //"Should be equal to 0."
                for (int i = 0; i < times; i++)
                {
                    _ilist.Clear();
                    Assert.Equal(0, list.Count); //"Should be equal to 0."
                }
            }
            public void NonGenericIListClearNonEmptyList(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                IList _ilist = list;
                _ilist.Clear();
                Assert.Equal(0, list.Count); //"Should be equal to 0."
            }

            public void NonGenericIListClearMultipleTimesNonEmptyList(T[] items, int times)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                IList _ilist = list;
                for (int i = 0; i < times; i++)
                {
                    _ilist.Clear();
                    Assert.Equal(0, list.Count); //"Should be equal to 0."
                }
            }

            #endregion

            #region TrueForAll

            public void TrueForAll_VerifyVanilla(T[] items)
            {
                T? expectedItem = default(T);
                SegmentedList<T> list = new SegmentedList<T>();
                Predicate<T> expectedItemDelegate = delegate (T item) { return expectedItem == null ? item != null : !expectedItem.Equals(item); };
                bool typeNullable = default(T) == null;

                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                //[] Verify TrueForAll looks at every item
                for (int i = 0; i < items.Length; ++i)
                {
                    expectedItem = items[i];
                    Assert.False(list.TrueForAll(expectedItemDelegate)); //"Err_282308ahid Verify TrueForAll looks at every item FAILED\n"
                }

                //[] Verify TrueForAll returns true if the match returns true on every item
                Assert.True(list.TrueForAll(delegate (T item) { return true; }),
                        "Err_548ahid Verify TrueForAll returns true if the match returns true on every item FAILED\n");

                //[] Verify TrueForAll returns false if the match returns false on every item
                Assert.True((0 == items.Length) == list.TrueForAll(delegate (T item) { return false; }),
                        "Err_30848ahidi Verify TrueForAll returns " + (0 == items.Length) + " if the match returns false on every item FAILED\n");
            }

            public void TrueForAll_VerifyExceptions(T[] items)
            {
                var list = new SegmentedList<T>(items);
                Assert.True(list.TrueForAll(item => true));
                Assert.Throws<ArgumentNullException>(() => list.TrueForAll(null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"
            }

            #endregion

            #region ToArray

            public void BasicToArray(T[] items)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);

                T[] arr = list.ToArray();

                for (int i = 0; i < items.Length; i++)
                {
                    Assert.Equal(((object?)arr[i]), items[i]); //"Should be equal."
                }
            }

            public void EnsureNotUnderlyingToArray(T[] items, T item)
            {
                SegmentedList<T> list = new SegmentedList<T>(items);
                T[] arr = list.ToArray();
                list[0] = item;
                if (((object?)arr[0]) == null)
                    Assert.NotNull(list[0]); //"Should NOT be null"
                else
                    Assert.NotEqual(((object?)arr[0]), list[0]); //"Should NOT be equal."
            }

            #endregion
        }

        [Fact]
        public static void InsertTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;

            int[] intArr2 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr2[i] = i + 100;

            IntDriver.BasicInsert(new int[0], 1, 0, 3);
            IntDriver.BasicInsert(intArr1, 101, 50, 4);
            IntDriver.BasicInsert(intArr1, 100, 100, 5);
            IntDriver.BasicInsert(intArr1, 100, 99, 6);
            IntDriver.BasicInsert(intArr1, 50, 0, 7);
            IntDriver.BasicInsert(intArr1, 50, 1, 8);
            IntDriver.BasicInsert(intArr1, 100, 50, 50);

            IntDriver.NonGenericIListBasicInsert(new int[0], 1, 0, 3);
            IntDriver.NonGenericIListBasicInsert(intArr1, 101, 50, 4);
            IntDriver.NonGenericIListBasicInsert(intArr1, 100, 100, 5);
            IntDriver.NonGenericIListBasicInsert(intArr1, 100, 99, 6);
            IntDriver.NonGenericIListBasicInsert(intArr1, 50, 0, 7);
            IntDriver.NonGenericIListBasicInsert(intArr1, 50, 1, 8);
            IntDriver.NonGenericIListBasicInsert(intArr1, 100, 50, 50);

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();
            string[] stringArr2 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr2[i] = "SomeTestString" + (i + 100).ToString();

            StringDriver.BasicInsert(stringArr1, "strobia", 99, 2);
            StringDriver.BasicInsert(stringArr1, "strobia", 100, 3);
            StringDriver.BasicInsert(stringArr1, "strobia", 0, 4);
            StringDriver.BasicInsert(stringArr1, "strobia", 1, 5);
            StringDriver.BasicInsert(stringArr1, "strobia", 50, 51);
            StringDriver.BasicInsert(stringArr1, "strobia", 0, 100);
            StringDriver.BasicInsert(new string?[] { null, null, null, "strobia", null }, null, 2, 3);
            StringDriver.BasicInsert(new string?[] { null, null, null, null, null }, "strobia", 0, 5);
            StringDriver.BasicInsert(new string?[] { null, null, null, null, null }, "strobia", 5, 1);
            StringDriver.NonGenericIListBasicInsert(stringArr1, "strobia", 99, 2);
            StringDriver.NonGenericIListBasicInsert(stringArr1, "strobia", 100, 3);
            StringDriver.NonGenericIListBasicInsert(stringArr1, "strobia", 0, 4);
            StringDriver.NonGenericIListBasicInsert(stringArr1, "strobia", 1, 5);
            StringDriver.NonGenericIListBasicInsert(stringArr1, "strobia", 50, 51);
            StringDriver.NonGenericIListBasicInsert(stringArr1, "strobia", 0, 100);
            StringDriver.NonGenericIListBasicInsert(new string?[] { null, null, null, "strobia", null }, null, 2, 3);
            StringDriver.NonGenericIListBasicInsert(new string?[] { null, null, null, null, null }, "strobia", 0, 5);
            StringDriver.NonGenericIListBasicInsert(new string?[] { null, null, null, null, null }, "strobia", 5, 1);
        }

        [Fact]
        public static void InsertTests_negative()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;
            IntDriver.InsertValidations(intArr1);
            IntDriver.NonGenericIListInsertValidations(intArr1);

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();
            StringDriver.InsertValidations(stringArr1);
            StringDriver.NonGenericIListInsertValidations(stringArr1);
        }

        [Fact]
        public static void InsertRangeTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;

            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr2[i] = i + 100;
            }

            foreach (Func<int[], IEnumerable<int>> collectionGenerator in IntDriver.CollectionGenerators)
            {
                IntDriver.InsertRangeIEnumerable(new int[0], intArr1, 0, 1, collectionGenerator);
                IntDriver.InsertRangeIEnumerable(intArr1, intArr2, 0, 1, collectionGenerator);
                IntDriver.InsertRangeIEnumerable(intArr1, intArr2, 1, 1, collectionGenerator);
                IntDriver.InsertRangeIEnumerable(intArr1, intArr2, 99, 1, collectionGenerator);
                IntDriver.InsertRangeIEnumerable(intArr1, intArr2, 100, 1, collectionGenerator);
                IntDriver.InsertRangeIEnumerable(intArr1, intArr2, 50, 50, collectionGenerator);
            }

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr2[i] = "SomeTestString" + (i + 100).ToString();

            foreach (Func<string?[], IEnumerable<string?>> collectionGenerator in StringDriver.CollectionGenerators)
            {
                StringDriver.InsertRangeIEnumerable(new string[0], stringArr1, 0, 1, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(stringArr1, stringArr2, 0, 1, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(stringArr1, stringArr2, 1, 1, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(stringArr1, stringArr2, 99, 1, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(stringArr1, stringArr2, 100, 1, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(stringArr1, stringArr2, 50, 50, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(new string?[] { null, null, null, null }, stringArr2, 0, 1, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(new string?[] { null, null, null, null }, stringArr2, 4, 1, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(new string?[] { null, null, null, null }, new string?[] { null, null, null, null }, 0, 1, collectionGenerator);
                StringDriver.InsertRangeIEnumerable(new string?[] { null, null, null, null }, new string?[] { null, null, null, null }, 4, 50, collectionGenerator);
            }
        }

        [Fact]
        public static void InsertRangeTests_Negative()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            IntDriver.InsertRangeValidations(intArr1, IntDriver.ConstructTestEnumerable);
            StringDriver.InsertRangeValidations(stringArr1, StringDriver.ConstructTestEnumerable);
        }

        [Fact]
        public static void GetRangeTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;

            IntDriver.BasicGetRange(intArr1, 50, 50);
            IntDriver.BasicGetRange(intArr1, 0, 50);
            IntDriver.BasicGetRange(intArr1, 50, 25);
            IntDriver.BasicGetRange(intArr1, 0, 25);
            IntDriver.BasicGetRange(intArr1, 75, 25);
            IntDriver.BasicGetRange(intArr1, 0, 100);
            IntDriver.BasicGetRange(intArr1, 0, 99);
            IntDriver.BasicGetRange(intArr1, 1, 1);
            IntDriver.BasicGetRange(intArr1, 99, 1);
            IntDriver.EnsureRangeIsReference(intArr1, 101, 0, 10);
            IntDriver.EnsureThrowsAfterModification(intArr1, 10, 10, 10);

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            StringDriver.BasicGetRange(stringArr1, 50, 50);
            StringDriver.BasicGetRange(stringArr1, 0, 50);
            StringDriver.BasicGetRange(stringArr1, 50, 25);
            StringDriver.BasicGetRange(stringArr1, 0, 25);
            StringDriver.BasicGetRange(stringArr1, 75, 25);
            StringDriver.BasicGetRange(stringArr1, 0, 100);
            StringDriver.BasicGetRange(stringArr1, 0, 99);
            StringDriver.BasicGetRange(stringArr1, 1, 1);
            StringDriver.BasicGetRange(stringArr1, 99, 1);
            StringDriver.EnsureRangeIsReference(stringArr1, "SometestString101", 0, 10);
            StringDriver.EnsureThrowsAfterModification(stringArr1, "str", 10, 10);
        }

        [Fact]
        public static void GetRangeTests_Negative()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            StringDriver.GetRangeValidations(stringArr1);
            IntDriver.GetRangeValidations(intArr1);
        }

        [Fact]
        public static void ExistsTests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int[] intArray;
            string[] stringArray;
            int arraySize = 16;

            intArray = new int[arraySize];
            stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.Exists_Verify(new int[0]);
            intDriver.Exists_Verify(new int[] { 1 });
            intDriver.Exists_Verify(intArray);

            stringDriver.Exists_Verify(new string[0]);
            stringDriver.Exists_Verify(new string[] { "1" });
            stringDriver.Exists_Verify(stringArray);
        }

        [Fact]
        public static void ExistsTests_Negative()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int[] intArray;
            string[] stringArray;
            int arraySize = 16;

            intArray = new int[arraySize];
            stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.Exists_VerifyExceptions(intArray);
            stringDriver.Exists_VerifyExceptions(stringArray);
        }

        [Fact]
        public static void ContainsTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr1[i] = i;
            }

            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr2[i] = i + 10;
            }

            IntDriver.BasicContains(intArr1);
            IntDriver.NonExistingValues(intArr1, intArr2);
            IntDriver.RemovedValues(intArr1);
            IntDriver.AddRemoveValues(intArr1);
            IntDriver.MultipleValues(intArr1, 3);
            IntDriver.MultipleValues(intArr1, 5);
            IntDriver.MultipleValues(intArr1, 17);
            IntDriver.NonGenericIListBasicContains(intArr1);
            IntDriver.NonGenericIListNonExistingValues(intArr1, intArr2);
            IntDriver.NonGenericIListRemovedValues(intArr1);
            IntDriver.NonGenericIListAddRemoveValues(intArr1);
            IntDriver.NonGenericIListMultipleValues(intArr1, 3);
            IntDriver.NonGenericIListMultipleValues(intArr1, 5);
            IntDriver.NonGenericIListMultipleValues(intArr1, 17);
            IntDriver.NonGenericIListContainsTestParams();

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr1[i] = "SomeTestString" + i.ToString();
            }
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr2[i] = "SomeTestString" + (i + 10).ToString();
            }

            StringDriver.BasicContains(stringArr1);
            StringDriver.NonExistingValues(stringArr1, stringArr2);
            StringDriver.RemovedValues(stringArr1);
            StringDriver.AddRemoveValues(stringArr1);
            StringDriver.MultipleValues(stringArr1, 3);
            StringDriver.MultipleValues(stringArr1, 5);
            StringDriver.MultipleValues(stringArr1, 17);
            StringDriver.ContainsNullWhenReference(stringArr1, null);
            StringDriver.NonGenericIListBasicContains(stringArr1);
            StringDriver.NonGenericIListNonExistingValues(stringArr1, stringArr2);
            StringDriver.NonGenericIListRemovedValues(stringArr1);
            StringDriver.NonGenericIListAddRemoveValues(stringArr1);
            StringDriver.NonGenericIListMultipleValues(stringArr1, 3);
            StringDriver.NonGenericIListMultipleValues(stringArr1, 5);
            StringDriver.NonGenericIListMultipleValues(stringArr1, 17);
            StringDriver.NonGenericIListContainsNullWhenReference(stringArr1, null);
            StringDriver.NonGenericIListContainsTestParams();
        }

        [Fact]
        public static void ClearTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr[i] = i;
            }

            IntDriver.ClearEmptyList();
            IntDriver.ClearMultipleTimesEmptyList(1);
            IntDriver.ClearMultipleTimesEmptyList(10);
            IntDriver.ClearMultipleTimesEmptyList(100);
            IntDriver.ClearNonEmptyList(intArr);
            IntDriver.ClearMultipleTimesNonEmptyList(intArr, 2);
            IntDriver.ClearMultipleTimesNonEmptyList(intArr, 7);
            IntDriver.ClearMultipleTimesNonEmptyList(intArr, 31);
            IntDriver.NonGenericIListClearEmptyList();
            IntDriver.NonGenericIListClearMultipleTimesEmptyList(1);
            IntDriver.NonGenericIListClearMultipleTimesEmptyList(10);
            IntDriver.NonGenericIListClearMultipleTimesEmptyList(100);
            IntDriver.NonGenericIListClearNonEmptyList(intArr);
            IntDriver.NonGenericIListClearMultipleTimesNonEmptyList(intArr, 2);
            IntDriver.NonGenericIListClearMultipleTimesNonEmptyList(intArr, 7);
            IntDriver.NonGenericIListClearMultipleTimesNonEmptyList(intArr, 31);

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr[i] = "SomeTestString" + i.ToString();
            }

            StringDriver.ClearEmptyList();
            StringDriver.ClearMultipleTimesEmptyList(1);
            StringDriver.ClearMultipleTimesEmptyList(10);
            StringDriver.ClearMultipleTimesEmptyList(100);
            StringDriver.ClearNonEmptyList(stringArr);
            StringDriver.ClearMultipleTimesNonEmptyList(stringArr, 2);
            StringDriver.ClearMultipleTimesNonEmptyList(stringArr, 7);
            StringDriver.ClearMultipleTimesNonEmptyList(stringArr, 31);
            StringDriver.NonGenericIListClearEmptyList();
            StringDriver.NonGenericIListClearMultipleTimesEmptyList(1);
            StringDriver.NonGenericIListClearMultipleTimesEmptyList(10);
            StringDriver.NonGenericIListClearMultipleTimesEmptyList(100);
            StringDriver.NonGenericIListClearNonEmptyList(stringArr);
            StringDriver.NonGenericIListClearMultipleTimesNonEmptyList(stringArr, 2);
            StringDriver.NonGenericIListClearMultipleTimesNonEmptyList(stringArr, 7);
            StringDriver.NonGenericIListClearMultipleTimesNonEmptyList(stringArr, 31);
        }

        [Fact]
        public static void TrueForAllTests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int[] intArray;
            string[] stringArray;
            int arraySize = 16;

            intArray = new int[arraySize];
            stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.TrueForAll_VerifyVanilla(new int[0]);
            intDriver.TrueForAll_VerifyVanilla(new int[] { 1 });
            intDriver.TrueForAll_VerifyVanilla(intArray);

            stringDriver.TrueForAll_VerifyVanilla(new string[0]);
            stringDriver.TrueForAll_VerifyVanilla(new string[] { "1" });
            stringDriver.TrueForAll_VerifyVanilla(stringArray);
        }

        [Fact]
        public static void TrueForAllTests_Negative()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int[] intArray;
            string[] stringArray;
            int arraySize = 16;

            intArray = new int[arraySize];
            stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }
            intDriver.TrueForAll_VerifyExceptions(intArray);
            stringDriver.TrueForAll_VerifyExceptions(stringArray);
        }
    }
}
