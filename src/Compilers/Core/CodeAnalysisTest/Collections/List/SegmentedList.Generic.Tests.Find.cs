// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v8.0.3/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.Find.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
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
        private readonly Predicate<T?> _equalsDefaultDelegate = (T? item) => { return default(T) == null ? item == null : default(T)!.Equals(item); };
        private readonly Predicate<T?> _alwaysTrueDelegate = (T? item) => true;
        private readonly Predicate<T?> _alwaysFalseDelegate = (T? item) => false;

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindVerifyExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();

            //[] Verify Null match Find
            Assert.Throws<ArgumentNullException>(() => list.Find(null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            //[] Verify Null match FindLast
            Assert.Throws<ArgumentNullException>(() => list.FindLast(null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            //[] Verify Null match FindLastIndex
            Assert.Throws<ArgumentNullException>(() => list.FindLastIndex(null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            //[] Verify Null match FindAll
            Assert.Throws<ArgumentNullException>(() => list.FindAll(null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLastIndexInt_VerifyExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            Predicate<T> predicate = _alwaysTrueDelegate;

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.FindLastIndex(0, null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            /******************************************************************************
            index
            ******************************************************************************/
            //[] Verify index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(int.MinValue, predicate)); //"Err_948ahid Expected index=Int32.MinValue to throw ArgumentOutOfRangeException"

            if (0 < list.Count)
            {
                //[] Verify index=-1
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(-1, predicate)); //"Err_328ahuaw Expected index=-1 to throw ArgumentOutOfRangeException"
            }

            //[] Verify index=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count + 1, predicate)); //"Err_488ajdi Expected index=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count, predicate)); //"Err_9689ajis Expected index=list.Count to throw ArgumentOutOfRangeException"

            //[] Verify index=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(int.MaxValue, predicate)); //"Err_238ajwisa Expected index=Int32.MaxValue to throw ArgumentOutOfRangeException"
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindIndexIntInt_VerifyExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            Predicate<T> predicate = delegate (T item) { return true; };

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.FindIndex(0, 0, null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            /******************************************************************************
            index
            ******************************************************************************/
            //[] Verify index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(int.MinValue, 0, predicate)); //"Err_948ahid Expected index=Int32.MinValue to throw ArgumentOutOfRangeException"

            //[] Verify index=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(-1, 0, predicate)); //"Err_328ahuaw Expected index=-1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(list.Count + 1, 0, predicate)); //"Err_488ajdi Expected index=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(list.Count, 1, predicate)); //"Err_9689ajis Expected index=list.Count to throw ArgumentOutOfRangeException"

            //[] Verify index=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(int.MaxValue, 0, predicate)); //"Err_238ajwisa Expected index=Int32.MaxValue to throw ArgumentOutOfRangeException"

            /******************************************************************************
            count
            ******************************************************************************/
            //[] Verify count=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, int.MinValue, predicate)); //Err_948ahid Expected count=Int32.MinValue to throw ArgumentOutOfRangeException"

            //[] Verify count=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, -1, predicate)); //"Err_328ahuaw Expected count=-1 to throw ArgumentOutOfRangeException"

            //[] Verify count=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, list.Count + 1, predicate)); //"Err_488ajdi Expected count=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify count=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, int.MaxValue, predicate)); //"Err_238ajwisa Expected count=Int32.MaxValue to throw ArgumentOutOfRangeException"

            /******************************************************************************
            index and count
            ******************************************************************************/
            if (0 < count)
            {
                //[] Verify index=1 count=list.Length
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(1, count, predicate)); //"Err_018188avbiw Expected index=1 count=list.Length to throw ArgumentOutOfRangeException"

                //[] Verify index=0 count=list.Length + 1
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, count + 1, predicate)); //"Err_6848ajiodxbz Expected index=0 count=list.Length + 1 to throw ArgumentOutOfRangeException"
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLastIndexIntInt_VerifyExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            Predicate<T> predicate = _alwaysTrueDelegate;

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.FindLastIndex(0, 0, null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            /******************************************************************************
            index
            ******************************************************************************/
            //[] Verify index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(int.MinValue, 0, predicate)); //Err_948ahid Expected index=Int32.MinValue to throw ArgumentOutOfRangeException"

            if (0 < list.Count)
            {
                //[] Verify index=-1
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(-1, 0, predicate)); //"Err_328ahuaw Expected index=-1 to throw ArgumentOutOfRangeException"
            }

            //[] Verify index=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count + 1, 0, predicate)); //"Err_488ajdi Expected index=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count, 1, predicate)); //"Err_9689ajis Expected index=list.Count to throw ArgumentOutOfRangeException"

            //[] Verify index=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(int.MaxValue, 0, predicate)); //"Err_238ajwisa Expected index=Int32.MaxValue to throw ArgumentOutOfRangeException"

            /******************************************************************************
            count
            ******************************************************************************/
            //[] Verify count=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count - 1, int.MinValue, predicate)); //"Err_948ahid Expected count=Int32.MinValue to throw ArgumentOutOfRangeException"

            //[] Verify count=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count - 1, -1, predicate)); //"Err_328ahuaw Expected count=-1 to throw ArgumentOutOfRangeException"

            //[] Verify count=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count - 1, list.Count + 1, predicate)); //"Err_488ajdi Expected count=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify count=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count - 1, int.MaxValue, predicate)); //"Err_238ajwisa Expected count=Int32.MaxValue to throw ArgumentOutOfRangeException"

            /******************************************************************************
            index and count
            ******************************************************************************/
            if (0 < count)
            {
                //[] Verify index=1 count=list.Length
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(count - 2, count, predicate)); //"Err_018188avbiw Expected index=1 count=list.Length to throw ArgumentOutOfRangeException"

                //[] Verify index=0 count=list.Length + 1
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(count - 1, count + 1, predicate)); //"Err_6848ajiodxbz Expected index=0 count=list.Length + 1 to throw ArgumentOutOfRangeException"
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindIndexInt_VerifyExceptions(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            Predicate<T> predicate = delegate (T item) { return true; };

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.FindIndex(0, null!)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            /******************************************************************************
            index
            ******************************************************************************/
            //[] Verify index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(int.MinValue, predicate)); //"Err_948ahid Expected index=Int32.MinValue to throw ArgumentOutOfRangeException"

            //[] Verify index=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(-1, predicate)); //"Err_328ahuaw Expected index=-1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(list.Count + 1, predicate)); //"Err_488ajdi Expected index=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify index=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(int.MaxValue, predicate)); //"Err_238ajwisa Expected index=Int32.MaxValue to throw ArgumentOutOfRangeException"
        }

        #region Find

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Find_VerifyVanilla(int count)
        {
            SegmentedList<T?> list = GenericListFactory(count)!;
            SegmentedList<T?> beforeList = list.ToSegmentedList();
            T? expectedItem = default(T);
            T? foundItem;
            Predicate<T?> EqualsDelegate = (T? item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            //[] Verify Find returns the correct index
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                foundItem = list.Find(EqualsDelegate);

                Assert.Equal(expectedItem, foundItem); //"Err_282308ahid Verifying value returned from Find FAILED\n"
            }

            //[] Verify Find returns the first item if the match returns true on every item
            foundItem = list.Find(_alwaysTrueDelegate);
            Assert.Equal(0 < count ? beforeList[0] : default(T), foundItem); //"Err_548ahid Verify Find returns the first item if the match returns true on every item FAILED\n"

            //[] Verify Find returns T.Default if the match returns false on every item
            foundItem = list.Find(_alwaysFalseDelegate);
            Assert.Equal(default(T), foundItem); //"Err_30848ahidi Verify Find returns T.Default if the match returns false on every item FAILED\n"

            //[] Verify with default(T)
            list.Add(default(T));
            foundItem = list.Find((T? item) => { return item == null ? default(T) == null : item.Equals(default(T)); });
            Assert.Equal(default(T), foundItem); //"Err_541848ajodi Verify with default(T) FAILED\n"
            list.RemoveAt(list.Count - 1);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Find_VerifyDuplicates(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            T? foundItem;
            Predicate<T?> EqualsDelegate = (T? item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < count)
            {
                list.Add(beforeList[0]);

                //[] Verify first item is duplicated
                expectedItem = beforeList[0];
                foundItem = list.Find(EqualsDelegate);
                Assert.Equal(expectedItem, foundItem); //"Err_2879072qaiadf  Verify first item is duplicated FAILED\n"
            }

            if (1 < count)
            {
                list.Add(beforeList[1]);

                //[] Verify second item is duplicated
                expectedItem = beforeList[1];
                foundItem = list.Find(EqualsDelegate);
                Assert.Equal(expectedItem, foundItem); //"Err_4588ajdia Verify second item is duplicated FAILED\n"

                //[] Verify with match that matches more then one item
                expectedItem = beforeList[0];
                foundItem = list.Find(EqualsDelegate);
                Assert.Equal(expectedItem, foundItem); //"Err_4489ajodoi Verify with match that matches more then one item FAILED\n"
            }
        }

        [Fact]
        public void Find_ListSizeCanBeChanged()
        {
            SegmentedList<int> expectedList = new SegmentedList<int>() { 1, 2, 3, 2, 3, 4, 3, 4, 4 };

            SegmentedList<int> list = new SegmentedList<int>() { 1, 2, 3 };

            int result = list.Find(i =>
            {
                if (i < 4)
                {
                    list.Add(i + 1);
                }

                return false;
            });

            Assert.Equal(0, result);
            Assert.Equal(expectedList, list);
        }

        #endregion

        #region FindLast

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLast_VerifyVanilla(int count)
        {
            SegmentedList<T?> list = GenericListFactory(count)!;
            SegmentedList<T?> beforeList = list.ToSegmentedList();
            T? expectedItem = default(T);
            T? foundItem;
            Predicate<T?> EqualsDelegate = (T? item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < count; ++i)
                list.Add(beforeList[i]);

            //[] Verify FindLast returns the correct item
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                foundItem = list.FindLast(EqualsDelegate);

                Assert.Equal(expectedItem, foundItem); //"Err_282308ahid Verifying value returned from find FAILED\n"
            }

            //[] Verify FindLast returns the last item if the match returns true on every item
            foundItem = list.FindLast(_alwaysTrueDelegate);
            T? expected = 0 < count ? beforeList[count - 1] : default(T);
            Assert.Equal(expected, foundItem); //"Err_548ahid Verify FindLast returns the last item if the match returns true on every item FAILED\n"

            //[] Verify FindLast returns default(T) if the match returns false on every item
            foundItem = list.FindLast(_alwaysFalseDelegate);
            Assert.Equal(default(T), foundItem); //"Err_30848ahidi Verify FindLast returns t.default if the match returns false on every item FAILED\n"

            //[] Verify with default(T)
            list.Add(default(T));
            foundItem = list.FindLast((T? item) => { return item == null ? default(T) == null : item.Equals(default(T)); });
            Assert.Equal(default(T), foundItem); //"Err_541848ajodi Verify with default(T) FAILED\n"
            list.RemoveAt(list.Count - 1);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLast_VerifyDuplicates(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            T? foundItem;
            Predicate<T?> EqualsDelegate = (T? item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < count)
            {
                list.Add(beforeList[0]);

                //[] Verify first item is duplicated
                expectedItem = beforeList[0];
                foundItem = list.FindLast(EqualsDelegate);
                Assert.Equal(beforeList[0], foundItem); //"Err_2879072qaiadf  Verify first item is duplicated FAILED\n"
            }

            if (1 < count)
            {
                list.Add(beforeList[1]);

                //[] Verify second item is duplicated
                expectedItem = beforeList[1];
                foundItem = list.FindLast(EqualsDelegate);
                Assert.Equal(beforeList[1], foundItem); //"Err_4588ajdia Verify second item is duplicated FAILED\n"

                //[] Verify with match that matches more then one item
                foundItem = list.FindLast((T item) => { return item != null && (item.Equals(beforeList[0]) || item.Equals(beforeList[1])); });
                Assert.Equal(beforeList[1], foundItem); //"Err_4489ajodoi Verify with match that matches more then one item FAILED\n"
            }
        }

        #endregion

        #region FindIndex

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindIndex_VerifyVanilla(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDefaultDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < count; ++i)
                list.Add(beforeList[i]);

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                index = list.FindIndex(EqualsDefaultDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindIndex to return the same."
            }

            //[] Verify FindIndex returns 0 if the match returns true on every item
            int expected = count == 0 ? -1 : 0;
            index = list.FindIndex(_alwaysTrueDelegate);
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindIndex returns 0 if the match returns true on every item expected"

            //[] Verify FindIndex returns -1 if the match returns false on every item
            index = list.FindIndex(_alwaysFalseDelegate);
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindIndex returns -1 if the match returns false on every item"
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindIndex_VerifyDuplicates(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            T? expectedItem = default(T);
            int index;
            Predicate<T> EqualsDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < count)
            {
                list.Add(beforeList[0]);

                //[] Verify first item is duplicated
                expectedItem = beforeList[0];
                index = list.FindIndex(EqualsDelegate);
                Assert.Equal(0, index); //"Err_3282iahid Verify first item is duplicated"
            }

            if (1 < count)
            {
                list.Add(beforeList[1]);

                //[] Verify second item is duplicated
                expectedItem = beforeList[1];
                index = list.FindIndex(EqualsDelegate);
                Assert.Equal(1, index); //"Err_29892adewiu Verify second item is duplicated"
            }
        }

        #endregion

        #region FindIndex(int, pred<T>)

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindIndexInt_VerifyVanilla(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                index = list.FindIndex(0, EqualsDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindIndex to return the same"
            }

            //[] Verify FindIndex returns 0 if the match returns true on every item
            int expected = count == 0 ? -1 : 0;
            index = list.FindIndex(0, delegate (T item) { return true; });
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindIndex returns 0 if the match returns true on every item "

            //[] Verify FindIndex returns -1 if the match returns false on every item
            index = list.FindIndex(0, delegate (T item) { return false; });
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindIndex returns -1 if the match returns false on every item"

            //[] Verify FindIndex returns -1 if the index == count
            index = list.FindIndex(count, delegate (T item) { return true; });
            Assert.Equal(-1, index); //"Err_4858ajodoa Verify FindIndex returns -1 if the index == count"

            if (0 < count)
            {
                //[] Verify NEG FindIndex uses the index
                expectedItem = beforeList[0];
                index = list.FindIndex(1, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_548797ahjid Verify NEG FindIndex uses the index"
            }

            if (1 < count)
            {
                //[] Verify POS FindIndex uses the index LOWER
                expectedItem = beforeList[1];
                index = list.FindIndex(1, EqualsDelegate);
                Assert.Equal(1, index); //"Err_68797ahid Verify POS FindIndex uses the index LOWER"

                //[] Verify POS FindIndex uses the index UPPER
                expectedItem = beforeList[count - 1];
                index = list.FindIndex(1, EqualsDelegate);
                Assert.Equal(count - 1, index); //"Err_51488ajod Verify POS FindIndex uses the index UPPER"
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindIndexInt_VerifyDuplicates(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < count)
            {
                list.Add(beforeList[0]);

                //[] Verify first item is duplicated
                expectedItem = beforeList[0];
                index = list.FindIndex(0, EqualsDelegate);
                Assert.Equal(0, index); //"Err_3282iahid Verify first item is duplicated"

                //[] Verify first item is duplicated and index=1
                expectedItem = beforeList[0];
                index = list.FindIndex(1, EqualsDelegate);
                Assert.Equal(count, index); //"Err_8588ahidi Verify first item is duplicated and index=1"
            }

            if (1 < count)
            {
                list.Add(beforeList[1]);

                //[] Verify second item is duplicated
                expectedItem = beforeList[1];
                index = list.FindIndex(0, EqualsDelegate);
                Assert.Equal(1, index); //"Err_29892adewiu Verify second item is duplicated"

                //[] Verify second item is duplicated and index=2
                expectedItem = beforeList[1];
                index = list.FindIndex(2, EqualsDelegate);
                Assert.Equal(count + 1, index); //"Err_1580ahisdf Verify second item is duplicated and index=2 "
            }
        }

        #endregion

        #region FindIndex(int, int, pred<T>)

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindIndexIntInt_VerifyVanilla(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                index = list.FindIndex(0, count, delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); });
                Assert.Equal(i, index); //"Err_282308ahid Expected FindIndex to return the same."
            }

            //[] Verify FindIndex returns 0 if the match returns true on every item
            index = list.FindIndex(0, count, delegate (T item) { return true; });
            int expected = count == 0 ? -1 : 0;
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindIndex returns 0 if the match returns true on every item"

            //[] Verify FindIndex returns -1 if the match returns false on every item
            index = list.FindIndex(0, count, delegate (T item) { return false; });
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindIndex returns -1 if the match returns false on every item"

            //[] Verify FindIndex returns -1 if the index == count
            index = list.FindIndex(count, 0, delegate (T item) { return true; });
            Assert.Equal(-1, index); //"Err_4858ajodoa Verify FindIndex returns -1 if the index == count"

            if (0 < count)
            {
                //[] Verify NEG FindIndex uses the index
                expectedItem = beforeList[0];
                index = list.FindIndex(1, count - 1, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_548797ahjid Verify NEG FindIndex uses the index "

                //[] Verify NEG FindIndex uses the count
                expectedItem = beforeList[count - 1];
                index = list.FindIndex(0, count - 1, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_7894ahoid Verify NEG FindIndex uses the count "
            }

            if (1 < count)
            {
                //[] Verify POS FindIndex uses the index
                expectedItem = beforeList[1];
                index = list.FindIndex(1, count - 1, EqualsDelegate);
                Assert.Equal(1, index); //"Err_68797ahid Verify POS FindIndex uses the index"

                //[] Verify POS FindIndex uses the count
                expectedItem = beforeList[count - 2];
                index = list.FindIndex(0, count - 1, EqualsDelegate);
                Assert.Equal(count - 2, index); //"Err_28278ahdii Verify POS FindIndex uses the count"

                //[] Verify NEG FindIndex uses the index and count LOWER
                expectedItem = beforeList[0];
                index = list.FindIndex(1, count - 2, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_384984ahjiod Verify NEG FindIndex uses the index and count LOWER "

                //[] Verify NEG FindIndex uses the index and count UPPER
                expectedItem = beforeList[count - 1];
                index = list.FindIndex(1, count - 2, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_1489haidid Verify NEG FindIndex uses the index and count UPPER "

                //[] Verify POS FindIndex uses the index and count LOWER
                expectedItem = beforeList[1];
                index = list.FindIndex(1, count - 2, EqualsDelegate);
                Assert.Equal(1, index); //"Err_604890ahjid Verify POS FindIndex uses the index and count LOWER "

                //[] Verify POS FindIndex uses the index and count UPPER
                expectedItem = beforeList[count - 2];
                index = list.FindIndex(1, count - 2, EqualsDelegate);
                Assert.Equal(count - 2, index); //"Err_66844ahidd Verify POS FindIndex uses the index and count UPPER "
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindIndexIntInt_VerifyDuplicates(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < count)
            {
                list.Add(beforeList[0]);

                //[] Verify first item is duplicated
                expectedItem = beforeList[0];
                index = list.FindIndex(0, list.Count, EqualsDelegate);
                Assert.Equal(0, index); //"Err_3282iahid Verify first item is duplicated"

                //[] Verify first item is duplicated and index=1
                expectedItem = beforeList[0];
                index = list.FindIndex(1, list.Count - 1, EqualsDelegate);
                Assert.Equal(count, index); //"Err_8588ahidi Verify first item is duplicated and index=1"
            }

            if (1 < count)
            {
                list.Add(beforeList[1]);

                //[] Verify second item is duplicated
                expectedItem = beforeList[1];
                index = list.FindIndex(0, list.Count, EqualsDelegate);
                Assert.Equal(1, index); //"Err_29892adewiu Verify second item is duplicated"

                //[] Verify second item is duplicated and index=2
                expectedItem = beforeList[1];
                index = list.FindIndex(2, list.Count - 2, EqualsDelegate);
                Assert.Equal(count + 1, index); //"Err_1580ahisdf Verify second item is duplicated and index=2"
            }
        }

        #endregion

        #region FindLastIndex

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLastIndex_VerifyVanilla(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                index = list.FindLastIndex(EqualsDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindLastIndex to return the same."
            }

            //[] Verify FindLastIndex returns 0 if the match returns true on every item
            int expected = count == 0 ? -1 : count - 1;
            index = list.FindLastIndex(_alwaysTrueDelegate);
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindLastIndex returns 0 if the match returns true on every item"

            //[] Verify FindLastIndex returns -1 if the match returns false on every item
            index = list.FindLastIndex(_alwaysFalseDelegate);
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindLastIndex returns -1 if the match returns false on every item"
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLastIndex_VerifyDuplicates(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < count)
            {
                list.Add(beforeList[0]);

                //[] Verify first item is duplicated
                expectedItem = beforeList[0];
                index = list.FindLastIndex(EqualsDelegate);
                Assert.Equal(count, index); //"Err_3282iahid Verify first item is duplicated"
            }

            if (1 < count)
            {
                list.Add(beforeList[1]);

                //[] Verify second item is duplicated
                expectedItem = beforeList[1];
                index = list.FindLastIndex(EqualsDelegate);
                Assert.Equal(count + 1, index); //"Err_29892adewiu Verify second item is duplicated."
            }
        }

        #endregion

        #region FindLastIndex(int, pred<T>)

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLastIndexInt_VerifyVanilla(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                index = list.FindLastIndex(count - 1, EqualsDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindLastIndex to return the same."
            }

            //[] Verify FindLastIndex returns 0 if the match returns true on every item
            index = list.FindLastIndex(count - 1, _alwaysTrueDelegate);
            int expected = count == 0 ? -1 : count - 1;
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindLastIndex returns 0 if the match returns true on every item"

            //[] Verify FindLastIndex returns -1 if the match returns false on every item
            index = list.FindLastIndex(count - 1, _alwaysFalseDelegate);
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindLastIndex returns -1 if the match returns false on every item"

            //[] Verify FindLastIndex returns 0 if the index == 0
            expected = 0 < count ? count - 1 : -1;
            index = list.FindLastIndex(count - 1, _alwaysTrueDelegate);
            Assert.Equal(expected, index); //"Err_4858ajodoa Verify FindLastIndex returns 0 if the index == 0 "

            if (1 < count)
            {
                //[] Verify NEG FindLastIndex uses the index
                expectedItem = beforeList[count - 1];
                index = list.FindLastIndex(count - 2, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_548797ahjid Verify NEG FindLastIndex uses the index"

                //[] Verify POS FindLastIndex uses the index LOWER
                expectedItem = beforeList[0];
                index = list.FindLastIndex(count - 2, EqualsDelegate);
                Assert.Equal(0, index); //"Err_68797ahid Verify POS FindLastIndex uses the index LOWER"

                //[] Verify POS FindLastIndex uses the index UPPER
                expectedItem = beforeList[count - 2];
                expected = count - 2;
                index = list.FindLastIndex(count - 2, EqualsDelegate);
                Assert.Equal(expected, index); //"Err_51488ajod Verify POS FindLastIndex uses the index UPPER"
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLastIndexInt_VerifyDuplicates(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < count)
            {
                list.Add(beforeList[0]);

                //[] Verify first item is duplicated
                expectedItem = beforeList[0];
                index = list.FindLastIndex(list.Count - 1, EqualsDelegate);
                Assert.Equal(count, index); //"Err_3282iahid Verify first item is duplicated"

                //[] Verify first item is duplicated and index is on less then the index of the last duplicate
                expectedItem = beforeList[0];
                index = list.FindLastIndex(count - 1, EqualsDelegate);
                Assert.Equal(0, index); //"Err_8588ahidi Verify first item is duplicated and index is on less then the index of the last duplicate"
            }

            if (1 < count)
            {
                list.Add(beforeList[1]);

                //[] Verify second item is duplicated
                expectedItem = beforeList[1];
                index = list.FindLastIndex(list.Count - 1, EqualsDelegate);
                Assert.Equal(list.Count - 1, index); //"Err_29892adewiu Verify second item is duplicated"

                //[] Verify second item is duplicated and index is on less then the index of the last duplicate
                expectedItem = beforeList[1];
                index = list.FindLastIndex(list.Count - 3, EqualsDelegate);
                Assert.Equal(1, index); //"Err_1580ahisdf Verify second item is duplicated and index is on less then the index of the last duplicate"
            }
        }

        #endregion

        #region FindLastIndex(int, int, pred<T>)

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLastIndexIntInt_VerifyVanilla(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < count; ++i)
                list.Add(beforeList[i]);

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                index = list.FindLastIndex(count - 1, count, EqualsDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindLastIndex to be the same."
            }

            //[] Verify FindLastIndex returns 0 if the match returns true on every item
            int expected = count == 0 ? -1 : count - 1;
            index = list.FindLastIndex(count - 1, count, _alwaysTrueDelegate);
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindLastIndex returns 0 if the match returns true on every item"

            //[] Verify FindLastIndex returns -1 if the match returns false on every item
            index = list.FindLastIndex(count - 1, count, _alwaysFalseDelegate);
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindLastIndex returns -1 if the match returns false on every item"

            if (0 < count)
            {
                //[] Verify FindLastIndex returns -1 if the index == 0
                index = list.FindLastIndex(0, 0, _alwaysTrueDelegate);
                Assert.Equal(-1, index); //"Err_298298ahdi Verify FindLastIndex returns -1 if the index=0"

                //[] Verify NEG FindLastIndex uses the count
                expectedItem = beforeList[0];
                index = list.FindLastIndex(count - 1, count - 1, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_7894ahoid Verify NEG FindLastIndex uses the count"
            }

            if (1 < count)
            {
                //[] Verify NEG FindLastIndex uses the index
                expectedItem = beforeList[count - 1];
                index = list.FindLastIndex(count - 2, count - 1, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_548797ahjid Verify NEG FindLastIndex uses the index"

                //[] Verify POS FindLastIndex uses the index
                expectedItem = beforeList[count - 2];
                index = list.FindLastIndex(count - 2, count - 1, EqualsDelegate);
                Assert.Equal(count - 2, index); //"Err_68797ahid Verify POS FindLastIndex uses the index"

                //[] Verify POS FindLastIndex uses the count
                expectedItem = beforeList[count - 2];
                index = list.FindLastIndex(count - 1, count - 1, EqualsDelegate);
                Assert.Equal(count - 2, index); //"Err_28278ahdii Verify POS FindLastIndex uses the count"

                //[] Verify NEG FindLastIndex uses the index and count LOWER
                expectedItem = beforeList[0];
                index = list.FindLastIndex(count - 2, count - 2, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_384984ahjiod Verify NEG FindLastIndex uses the index and count LOWER"

                //[] Verify NEG FindLastIndex uses the index and count UPPER
                expectedItem = beforeList[count - 1];
                index = list.FindLastIndex(count - 2, count - 2, EqualsDelegate);
                Assert.Equal(-1, index); //"Err_1489haidid Verify NEG FindLastIndex uses the index and count UPPER"

                //[] Verify POS FindLastIndex uses the index and count LOWER
                expectedItem = beforeList[1];
                index = list.FindLastIndex(count - 2, count - 2, EqualsDelegate);
                Assert.Equal(1, index); //"Err_604890ahjid Verify POS FindLastIndex uses the index and count LOWER"

                //[] Verify POS FindLastIndex uses the index and count UPPER
                expectedItem = beforeList[count - 2];
                index = list.FindLastIndex(count - 2, count - 2, EqualsDelegate);
                Assert.Equal(count - 2, index); //"Err_66844ahidd Verify POS FindLastIndex uses the index and count UPPER"
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindLastIndexIntInt_VerifyDuplicates(int count)
        {
            T? expectedItem = default(T);
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            int index;
            Predicate<T> EqualsDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < count)
            {
                list.Add(beforeList[0]);

                //[] Verify first item is duplicated
                expectedItem = beforeList[0];
                index = list.FindLastIndex(list.Count - 1, list.Count, EqualsDelegate);
                Assert.Equal(list.Count - 1, index); //"Err_3282iahid Verify first item is duplicated"
            }

            if (1 < count)
            {
                list.Add(beforeList[1]);

                //[] Verify second item is duplicated
                expectedItem = beforeList[1];
                index = list.FindLastIndex(list.Count - 1, list.Count, EqualsDelegate);
                Assert.Equal(list.Count - 1, index); //"Err_29892adewiu Verify second item is duplicated"
            }
        }

        #endregion

        #region FindAll

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindAll_VerifyVanilla(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            T? expectedItem = default(T);
            Predicate<T> EqualsDelegate = (value) => expectedItem == null ? value == null : expectedItem.Equals(value);

            //[] Verify FindAll returns the correct List with one item
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                SegmentedList<T> results = list.FindAll(EqualsDelegate);
                VerifyList(results, beforeList.Where((value) => EqualsDelegate(value)).ToSegmentedList());
            }

            //[] Verify FindAll returns an List with all of the items if the predicate always returns true
            VerifyList(list.FindAll(_alwaysTrueDelegate), beforeList);

            //[] Verify FindAll returns an empty List if the match returns false on every item
            VerifyList(list.FindAll(_alwaysFalseDelegate), new SegmentedList<T>());
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void FindAll_VerifyDuplicates(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            for (int i = 0; i < count / 2; i++)
                list.Add(list[i]);
            SegmentedList<T> beforeList = list.ToSegmentedList();
            T? expectedItem = default(T);
            Predicate<T> EqualsDelegate = (value) => expectedItem == null ? value == null : expectedItem.Equals(value);
            //[] Verify FindAll returns the correct List with one item
            for (int i = 0; i < count; ++i)
            {
                expectedItem = beforeList[i];
                SegmentedList<T> results = list.FindAll(EqualsDelegate);
                VerifyList(results, beforeList.Where((value) => EqualsDelegate(value)).ToSegmentedList());
            }

            //[] Verify FindAll returns an List with all of the items if the predicate always returns true
            VerifyList(list.FindAll(_alwaysTrueDelegate), beforeList);

            //[] Verify FindAll returns an empty List if the match returns false on every item
            VerifyList(list.FindAll(_alwaysFalseDelegate), new SegmentedList<T>());
        }

        [Fact]
        public void FindAll_ListSizeCanBeChanged()
        {
            SegmentedList<int> list = new SegmentedList<int>() { 1, 2, 3 };
            SegmentedList<int> expectedList = new SegmentedList<int>() { 1, 2, 3, 2, 3, 4, 3, 4, 4 };

            SegmentedList<int> result = list.FindAll(i =>
            {
                if (i < 4)
                {
                    list.Add(i + 1);
                }

                return true;
            });

            Assert.Equal(expectedList, result);
            Assert.Equal(expectedList, list);
        }

        #endregion
    }
}
