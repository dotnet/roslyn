// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/Common/tests/System/Collections/IEnumerable.Generic.Tests.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the generic
    /// IEnumerable interface.
    /// </summary>
    public abstract partial class IEnumerable_Generic_Tests<T> : TestBase<T>
        where T : notnull
    {
        #region IEnumerable<T> Helper Methods

        /// <summary>
        /// Creates an instance of an IEnumerable{T} that can be used for testing.
        /// </summary>
        /// <param name="count">The number of unique items that the returned IEnumerable{T} contains.</param>
        /// <returns>An instance of an IEnumerable{T} that can be used for testing.</returns>
        protected abstract IEnumerable<T> GenericIEnumerableFactory(int count);

        /// <summary>
        /// Modifies the given IEnumerable such that any enumerators for that IEnumerable will be
        /// invalidated.
        /// </summary>
        /// <param name="enumerable">An IEnumerable to modify</param>
        /// <returns>true if the enumerable was successfully modified. Else false.</returns>
        protected delegate bool ModifyEnumerable(IEnumerable<T> enumerable);

        /// <summary>
        /// To be implemented in the concrete collections test classes. Returns a set of ModifyEnumerable delegates
        /// that modify the enumerable passed to them.
        /// </summary>
        protected abstract IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations);

        protected virtual ModifyOperation ModifyEnumeratorThrows => ModifyOperation.Add | ModifyOperation.Insert | ModifyOperation.Overwrite | ModifyOperation.Remove | ModifyOperation.Clear;

        protected virtual ModifyOperation ModifyEnumeratorAllowed => ModifyOperation.None;

        /// <summary>
        /// The Reset method is provided for COM interoperability. It does not necessarily need to be
        /// implemented; instead, the implementer can simply throw a NotSupportedException.
        ///
        /// If Reset is not implemented, this property must return False. The default value is true.
        /// </summary>
        protected virtual bool ResetImplemented => true;

        /// <summary>
        /// When calling Current of the enumerator before the first MoveNext, after the end of the collection,
        /// or after modification of the enumeration, the resulting behavior is undefined. Tests are included
        /// to cover two behavioral scenarios:
        ///   - Throwing an InvalidOperationException
        ///   - Returning an undefined value.
        ///
        /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
        /// false.
        /// </summary>
        protected virtual bool Enumerator_Current_UndefinedOperation_Throws => false;

        /// <summary>
        /// Same as <see cref="Enumerator_Current_UndefinedOperation_Throws"/> but only on empty collections.
        /// </summary>
        protected virtual bool Enumerator_Current_UndefinedOperation_Throws_On_Empty => false;

        /// <summary>
        /// When calling MoveNext or Reset after modification of the enumeration, the resulting behavior is
        /// undefined. Tests are included to cover two behavioral scenarios:
        ///   - Throwing an InvalidOperationException
        ///   - Execute MoveNext or Reset.
        ///
        /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
        /// true.
        /// </summary>
        protected virtual bool Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException => true;

        /// <summary>
        /// Specifies whether this IEnumerable follows some sort of ordering pattern.
        /// </summary>
        protected virtual EnumerableOrder Order => EnumerableOrder.Sequential;

        /// <summary>
        /// An enum to allow specification of the order of the Enumerable. Used in validation for enumerables.
        /// </summary>
        protected enum EnumerableOrder
        {
            Unspecified,
            Sequential
        }

        #endregion

        #region Validation

        private void RepeatTest(
            Action<IEnumerator<T>, T[], int> testCode,
            int iters = 3)
        {
            IEnumerable<T> enumerable = GenericIEnumerableFactory(32);
            T[] items = enumerable.ToArray();
            IEnumerator<T> enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < iters; i++)
            {
                testCode(enumerator, items, i);
                if (!ResetImplemented)
                {
                    enumerator = enumerable.GetEnumerator();
                }
                else
                {
                    enumerator.Reset();
                }
            }
        }

        private void RepeatTest(
            Action<IEnumerator<T>, T[]> testCode,
            int iters = 3)
        {
            RepeatTest((e, i, it) => testCode(e, i), iters);
        }

        private void VerifyModifiedEnumerator(
            IEnumerator<T> enumerator,
            object expectedCurrent,
            bool expectCurrentThrow,
            bool atEnd)
        {
            if (expectCurrentThrow)
            {
                Assert.Throws<InvalidOperationException>(
                    () => enumerator.Current);
            }
            else
            {
                object? current = enumerator.Current;
                for (var i = 0; i < 3; i++)
                {
                    Assert.Equal(expectedCurrent, current);
                    current = enumerator.Current;
                }
            }

            Assert.Throws<InvalidOperationException>(
                () => enumerator.MoveNext());

            if (!!ResetImplemented)
            {
                Assert.Throws<InvalidOperationException>(
                    () => enumerator.Reset());
            }
        }

        private void VerifyEnumerator(
            IEnumerator<T> enumerator,
            T[] expectedItems)
        {
            VerifyEnumerator(
                enumerator,
                expectedItems,
                0,
                expectedItems.Length,
                true,
                true);
        }

        private void VerifyEnumerator(
            IEnumerator<T> enumerator,
            T[] expectedItems,
            int startIndex,
            int count,
            bool validateStart,
            bool validateEnd)
        {
            bool needToMatchAllExpectedItems = count - startIndex == expectedItems.Length;
            if (validateStart)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (Enumerator_Current_UndefinedOperation_Throws)
                    {
                        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                    }
                    else
                    {
                        var cur = enumerator.Current;
                    }
                }
            }

            int iterations;
            if (Order == EnumerableOrder.Unspecified)
            {
                var itemsVisited =
                    new BitArray(
                        needToMatchAllExpectedItems
                            ? count
                            : expectedItems.Length,
                        false);
                for (iterations = 0;
                     iterations < count && enumerator.MoveNext();
                     iterations++)
                {
                    object? currentItem = enumerator.Current;
                    var itemFound = false;
                    for (var i = 0; i < itemsVisited.Length; ++i)
                    {
                        if (!itemsVisited[i]
                            && Equals(
                                currentItem,
                                expectedItems[
                                    i
                                    + (needToMatchAllExpectedItems
                                           ? startIndex
                                           : 0)]))
                        {
                            itemsVisited[i] = true;
                            itemFound = true;
                            break;
                        }
                    }
                    Assert.True(itemFound, "itemFound");

                    for (var i = 0; i < 3; i++)
                    {
                        object? tempItem = enumerator.Current;
                        Assert.Equal(currentItem, tempItem);
                    }
                }
                if (needToMatchAllExpectedItems)
                {
                    for (var i = 0; i < itemsVisited.Length; i++)
                    {
                        Assert.True(itemsVisited[i]);
                    }
                }
                else
                {
                    var visitedItemCount = 0;
                    for (var i = 0; i < itemsVisited.Length; i++)
                    {
                        if (itemsVisited[i])
                        {
                            ++visitedItemCount;
                        }
                    }
                    Assert.Equal(count, visitedItemCount);
                }
            }
            else if (Order == EnumerableOrder.Sequential)
            {
                for (iterations = 0;
                     iterations < count && enumerator.MoveNext();
                     iterations++)
                {
                    object? currentItem = enumerator.Current;
                    Assert.Equal(expectedItems[iterations], currentItem);
                    for (var i = 0; i < 3; i++)
                    {
                        object? tempItem = enumerator.Current;
                        Assert.Equal(currentItem, tempItem);
                    }
                }
            }
            else
            {
                throw new ArgumentException(
                    "EnumerableOrder is invalid.");
            }
            Assert.Equal(count, iterations);

            if (validateEnd)
            {
                for (var i = 0; i < 3; i++)
                {
                    Assert.False(enumerator.MoveNext(), "enumerator.MoveNext() returned true past the expected end.");

                    if (Enumerator_Current_UndefinedOperation_Throws)
                    {
                        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                    }
                    else
                    {
                        var cur = enumerator.Current;
                    }
                }
            }
        }

        #endregion

        #region GetEnumerator()

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_GetEnumerator_NoExceptionsWhileGetting(int count)
        {
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            enumerable.GetEnumerator().Dispose();
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_GetEnumerator_ReturnsUniqueEnumerator(int count)
        {
            //Tests that the enumerators returned by GetEnumerator operate independently of one another
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            int iterations = 0;
            foreach (T item in enumerable)
                foreach (T item2 in enumerable)
                    foreach (T item3 in enumerable)
                        iterations++;
            Assert.Equal(count * count * count, iterations);
        }

        #endregion

        #region Enumerator.MoveNext

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_MoveNext_FromStartToFinish(int count)
        {
            int iterations = 0;
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                while (enumerator.MoveNext())
                    iterations++;
                Assert.Equal(count, iterations);
            }
        }

        /// <summary>
        /// For most collections, all calls to MoveNext after disposal of an enumerator will return false.
        /// Some collections (SortedList), however, treat a call to dispose as if it were a call to Reset. Since the docs
        /// specify neither of these as being strictly correct, we leave the method virtual.
        /// </summary>
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void Enumerator_MoveNext_AfterDisposal(int count)
        {
            IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator();
            for (int i = 0; i < count; i++)
                enumerator.MoveNext();
            enumerator.Dispose();
            Assert.False(enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_MoveNext_AfterEndOfCollection(int count)
        {
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                for (int i = 0; i < count; i++)
                    enumerator.MoveNext();
                Assert.False(enumerator.MoveNext());
                Assert.False(enumerator.MoveNext());
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    if (ModifyEnumerable(enumerable))
                    {
                        if (Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                        {
                            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                        }
                        else
                        {
                            enumerator.MoveNext();
                        }
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedBeforeEnumeration_Succeeds(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorAllowed), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    if (ModifyEnumerable(enumerable))
                    {
                        if (Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                        {
                            enumerator.MoveNext();
                        }
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    for (int i = 0; i < count / 2; i++)
                        enumerator.MoveNext();
                    if (ModifyEnumerable(enumerable))
                    {
                        if (Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                        {
                            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                        }
                        else
                        {
                            enumerator.MoveNext();
                        }
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedDuringEnumeration_Succeeds(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorAllowed), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    for (int i = 0; i < count / 2; i++)
                        enumerator.MoveNext();
                    if (ModifyEnumerable(enumerable))
                    {
                        enumerator.MoveNext();
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    while (enumerator.MoveNext()) ;
                    if (ModifyEnumerable(enumerable))
                    {
                        if (Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                        {
                            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                        }
                        else
                        {
                            enumerator.MoveNext();
                        }
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedAfterEnumeration_Succeeds(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorAllowed), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                        ;
                    if (ModifyEnumerable(enumerable))
                    {
                        enumerator.MoveNext();
                    }
                }
            });
        }

        [Fact]
        public void IEnumerable_Generic_Enumerator_MoveNextHitsAllItems()
        {
            RepeatTest(
                (enumerator, items) =>
                {
                    var iterations = 0;
                    while (enumerator.MoveNext())
                    {
                        iterations++;
                    }
                    Assert.Equal(items.Length, iterations);
                });
        }

        [Fact]
        public void IEnumerable_Generic_Enumerator_MoveNextFalseAfterEndOfCollection()
        {
            RepeatTest(
                (enumerator, items) =>
                {
                    while (enumerator.MoveNext())
                    {
                    }

                    Assert.False(enumerator.MoveNext());
                });
        }

        #endregion

        #region Enumerator.Current

        [Fact]
        public void IEnumerable_Generic_Enumerator_Current()
        {
            // Verify that current returns proper result.
            RepeatTest(
                (enumerator, items, iteration) =>
                {
                    if (iteration == 1)
                    {
                        VerifyEnumerator(
                            enumerator,
                            items,
                            0,
                            items.Length / 2,
                            true,
                            false);
                    }
                    else
                    {
                        VerifyEnumerator(enumerator, items);
                    }
                });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Current_ReturnsSameValueOnRepeatedCalls(int count)
        {
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    T current = enumerator.Current;
                    Assert.Equal(current, enumerator.Current);
                    Assert.Equal(current, enumerator.Current);
                    Assert.Equal(current, enumerator.Current);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Current_ReturnsSameObjectsOnDifferentEnumerators(int count)
        {
            // Ensures that the elements returned from enumeration are exactly the same collection of
            // elements returned from a previous enumeration
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            HashSet<T> firstValues = new HashSet<T>(count);
            HashSet<T> secondValues = new HashSet<T>(count);
            foreach (T item in enumerable)
                Assert.True(firstValues.Add(item));
            foreach (T item in enumerable)
                Assert.True(secondValues.Add(item));
            Assert.Equal(firstValues.Count, secondValues.Count);
            foreach (T item in firstValues)
                Assert.True(secondValues.Contains(item));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Current_BeforeFirstMoveNext_UndefinedBehavior(int count)
        {
            T current;
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
            {
                if (Enumerator_Current_UndefinedOperation_Throws)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else if (Enumerator_Current_UndefinedOperation_Throws_On_Empty && count == 0)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else
                    current = enumerator.Current;
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Current_AfterEndOfEnumerable_UndefinedBehavior(int count)
        {
            T current;
            IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
            using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
            {
                while (enumerator.MoveNext()) ;
                if (Enumerator_Current_UndefinedOperation_Throws)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else if (Enumerator_Current_UndefinedOperation_Throws_On_Empty && count == 0)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else
                    current = enumerator.Current;
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Current_ModifiedDuringEnumeration_UndefinedBehavior(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                T current;
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    if (ModifyEnumerable(enumerable))
                    {
                        if (Enumerator_Current_UndefinedOperation_Throws)
                            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                        else
                            current = enumerator.Current;
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Current_ModifiedDuringEnumeration_Succeeds(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorAllowed), ModifyEnumerable =>
            {
                T current;
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    if (ModifyEnumerable(enumerable))
                    {
                        current = enumerator.Current;
                    }
                }
            });
        }

        #endregion

        #region Enumerator.Reset

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Reset_BeforeIteration_Support(int count)
        {
            using (IEnumerator<T> enumerator = GenericIEnumerableFactory(count).GetEnumerator())
            {
                if (ResetImplemented)
                    enumerator.Reset();
                else
                    Assert.Throws<NotSupportedException>(() => enumerator.Reset());
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    if (ModifyEnumerable(enumerable))
                    {
                        if (Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                        {
                            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                        }
                        else
                        {
                            enumerator.Reset();
                        }
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedBeforeEnumeration_Succeeds(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorAllowed), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    if (ModifyEnumerable(enumerable))
                    {
                        enumerator.Reset();
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    for (int i = 0; i < count / 2; i++)
                        enumerator.MoveNext();
                    if (ModifyEnumerable(enumerable))
                    {
                        if (Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                        {
                            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                        }
                        else
                        {
                            enumerator.Reset();
                        }
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedDuringEnumeration_Succeeds(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorAllowed), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    for (int i = 0; i < count / 2; i++)
                        enumerator.MoveNext();
                    if (ModifyEnumerable(enumerable))
                    {
                        enumerator.Reset();
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    while (enumerator.MoveNext()) ;
                    if (ModifyEnumerable(enumerable))
                    {
                        if (Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                        {
                            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                        }
                        else
                        {
                            enumerator.Reset();
                        }
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_Generic_Enumerator_Reset_ModifiedAfterEnumeration_Succeeds(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorAllowed), ModifyEnumerable =>
            {
                IEnumerable<T> enumerable = GenericIEnumerableFactory(count);
                using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                        ;
                    if (ModifyEnumerable(enumerable))
                    {
                        enumerator.Reset();
                    }
                }
            });
        }

        [Fact]
        public void IEnumerable_Generic_Enumerator_Reset()
        {
            if (!ResetImplemented)
            {
                RepeatTest(
                    (enumerator, items) =>
                    {
                        Assert.Throws<NotSupportedException>(
                            () => enumerator.Reset());
                    });
                RepeatTest(
                    (enumerator, items, iter) =>
                    {
                        if (iter == 1)
                        {
                            VerifyEnumerator(
                                enumerator,
                                items,
                                0,
                                items.Length / 2,
                                true,
                                false);
                            for (var i = 0; i < 3; i++)
                            {
                                Assert.Throws<NotSupportedException>(
                                    () => enumerator.Reset());
                            }
                            VerifyEnumerator(
                                enumerator,
                                items,
                                items.Length / 2,
                                items.Length - (items.Length / 2),
                                false,
                                true);
                        }
                        else if (iter == 2)
                        {
                            VerifyEnumerator(enumerator, items);
                            for (var i = 0; i < 3; i++)
                            {
                                Assert.Throws<NotSupportedException>(
                                    () => enumerator.Reset());
                            }
                            VerifyEnumerator(
                                enumerator,
                                items,
                                0,
                                0,
                                false,
                                true);
                        }
                        else
                        {
                            VerifyEnumerator(enumerator, items);
                        }
                    });
            }
            else
            {
                RepeatTest(
                    (enumerator, items, iter) =>
                    {
                        if (iter == 1)
                        {
                            VerifyEnumerator(
                                enumerator,
                                items,
                                0,
                                items.Length / 2,
                                true,
                                false);
                            enumerator.Reset();
                            enumerator.Reset();
                        }
                        else if (iter == 3)
                        {
                            VerifyEnumerator(enumerator, items);
                            enumerator.Reset();
                            enumerator.Reset();
                        }
                        else
                        {
                            VerifyEnumerator(enumerator, items);
                        }
                    },
                    5);
            }
        }

        #endregion
    }
}
