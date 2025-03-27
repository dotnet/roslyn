// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v8.0.3/src/libraries/Common/tests/System/Collections/ICollection.NonGeneric.Tests.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the nongeneric
    /// ICollection interface
    /// </summary>
    public abstract class ICollection_NonGeneric_Tests : IEnumerable_NonGeneric_Tests
    {
        #region Helper methods

        /// <summary>
        /// Creates an instance of an ICollection that can be used for testing.
        /// </summary>
        /// <returns>An instance of an ICollection that can be used for testing.</returns>
        protected abstract ICollection NonGenericICollectionFactory();

        /// <summary>
        /// Creates an instance of an ICollection that can be used for testing.
        /// </summary>
        /// <param name="count">The number of unique items that the returned ICollection contains.</param>
        /// <returns>An instance of an ICollection that can be used for testing.</returns>
        protected virtual ICollection NonGenericICollectionFactory(int count)
        {
            ICollection collection = NonGenericICollectionFactory();
            AddToCollection(collection, count);
            return collection;
        }

        protected virtual bool DuplicateValuesAllowed => true;
        protected virtual bool IsReadOnly => false;
        protected virtual bool NullAllowed => true;
        protected virtual bool ExpectedIsSynchronized => false;
        protected virtual IEnumerable<object?> InvalidValues => new object?[0];

        protected abstract void AddToCollection(ICollection collection, int numberOfItemsToAdd);

        /// <summary>
        /// Used for the ICollection_NonGeneric_CopyTo_ArrayOfEnumType test where we try to call CopyTo
        /// on an Array of Enum values. Some implementations special-case for this and throw an ArgumentException,
        /// while others just throw an InvalidCastExcepton.
        /// </summary>
        protected virtual Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);

        /// <summary>
        /// Used for the ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType test where we try to call CopyTo
        /// on an Array of different reference values. Some implementations special-case for this and throw an ArgumentException,
        /// while others just throw an InvalidCastExcepton or an ArrayTypeMismatchException.
        /// </summary>
        protected virtual Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(ArgumentException);

        /// <summary>
        /// Used for the ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType test where we try to call CopyTo
        /// on an Array of different value values. Some implementations special-case for this and throw an ArgumentException,
        /// while others just throw an InvalidCastExcepton.
        /// </summary>
        protected virtual Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(ArgumentException);

        /// <summary>
        /// Used for the ICollection_NonGeneric_CopyTo_NonZeroLowerBound test where we try to call CopyTo
        /// on an Array of with a non-zero lower bound.
        /// Most implementations throw an ArgumentException, but others (e.g. SortedList) throw
        /// an ArgumentOutOfRangeException.
        /// </summary>
        protected virtual Type ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType => typeof(ArgumentException);

        /// <summary>
        /// Used for ICollection_NonGeneric_SyncRoot tests. Some implementations (e.g. ConcurrentDictionary)
        /// don't support the SyncRoot property of an ICollection and throw a NotSupportedException.
        /// </summary>
        protected virtual bool ICollection_NonGeneric_SupportsSyncRoot => true;

        /// <summary>
        /// Used for ICollection_NonGeneric_SyncRoot tests. Some implementations (e.g. TempFileCollection)
        /// return null for the SyncRoot property of an ICollection.
        /// </summary>
        protected virtual bool ICollection_NonGeneric_HasNullSyncRoot => false;

        /// <summary>
        /// Used for the ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowsArgumentException tests. Some
        /// implementations throw a different exception type (e.g. ArgumentOutOfRangeException).
        /// </summary>
        protected virtual Type ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentException);

        /// <summary>
        /// Used for the ICollection_NonGeneric_CopyTo_TwoDimensionArray_ThrowsException test. Some implementations
        /// throw a different exception type (e.g. RankException by ImmutableArray)
        /// </summary>
        protected virtual Type ICollection_NonGeneric_CopyTo_TwoDimensionArray_ThrowType => typeof(ArgumentException);

        #endregion

        #region IEnumerable Helper Methods

        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

        protected override IEnumerable NonGenericIEnumerableFactory(int count) => NonGenericICollectionFactory(count);

        #endregion

        #region Count

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_Count_Validity(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Assert.Equal(count, collection.Count);
        }

        #endregion

        #region IsSynchronized

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_IsSynchronized(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Assert.Equal(ExpectedIsSynchronized, collection.IsSynchronized);
        }

        #endregion

        #region SyncRoot

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_SyncRoot(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            if (ICollection_NonGeneric_SupportsSyncRoot)
            {
                Assert.Equal(ICollection_NonGeneric_HasNullSyncRoot, collection.SyncRoot == null);
                Assert.Same(collection.SyncRoot, collection.SyncRoot);
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => collection.SyncRoot);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_SyncRootUnique(int count)
        {
            if (ICollection_NonGeneric_SupportsSyncRoot && !ICollection_NonGeneric_HasNullSyncRoot)
            {
                ICollection collection1 = NonGenericICollectionFactory(count);
                ICollection collection2 = NonGenericICollectionFactory(count);
                if (!ReferenceEquals(collection1, collection2))
                {
                    Assert.NotSame(collection1.SyncRoot, collection2.SyncRoot);
                }
            }
        }

        #endregion

        #region CopyTo

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_CopyTo_NullArray_ThrowsArgumentNullException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_CopyTo_TwoDimensionArray_ThrowsException(int count)
        {
            if (count > 0)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                Array arr = new object[count, count];
                Assert.Equal(2, arr.Rank);
                Assert.Throws(ICollection_NonGeneric_CopyTo_TwoDimensionArray_ThrowType, () => collection.CopyTo(arr, 0));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_NonGeneric_CopyTo_NonZeroLowerBound(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Array arr = Array.CreateInstance(typeof(object), new int[1] { count }, new int[1] { 2 });
            Assert.Equal(1, arr.Rank);
            Assert.Equal(2, arr.GetLowerBound(0));
            Assert.Throws(ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType, () => collection.CopyTo(arr, 0));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType(int count)
        {
            if (count > 0)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                float[] array = new float[count * 3 / 2];

                Assert.Throws(ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType, () => collection.CopyTo(array, 0));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType(int count)
        {
            if (count > 0)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                StringBuilder[] array = new StringBuilder[count * 3 / 2];
                Assert.Throws(ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType, () => collection.CopyTo(array, 0));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_NonGeneric_CopyTo_ArrayOfEnumType(int count)
        {
            Array enumArr = Enum.GetValues(typeof(EnumerableType));
            if (count > 0 && count < enumArr.Length)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                Assert.Throws(ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType, () => collection.CopyTo(enumArr, 0));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, int.MinValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_NonGeneric_CopyTo_IndexEqualToArrayCount_ThrowsArgumentException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            if (count > 0)
                Assert.Throws<ArgumentException>(() => collection.CopyTo(array, count));
            else
                collection.CopyTo(array, count); // does nothing since the array is empty
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowsAnyArgumentException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);

            object[] array = new object[count];
            Assert.Throws(ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowType, () => collection.CopyTo(array, count + 1));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_NonGeneric_CopyTo_NotEnoughSpaceInOffsettedArray_ThrowsArgumentException(int count)
        {
            if (count > 0) // Want the T array to have at least 1 element
            {
                ICollection collection = NonGenericICollectionFactory(count);
                object[] array = new object[count];
                Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 1));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_CopyTo_ExactlyEnoughSpaceInArray(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            collection.CopyTo(array, 0);
            int i = 0;
            foreach (object obj in collection)
                Assert.Equal(array[i++], obj);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_CopyTo_ArrayIsLargerThanCollection(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count * 3 / 2];
            collection.CopyTo(array, 0);
            int i = 0;
            foreach (object obj in collection)
                Assert.Equal(array[i++], obj);
        }

        #endregion
    }
}
