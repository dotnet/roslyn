// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/Common/tests/System/Collections/ICollection.Generic.Tests.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the generic
    /// ICollection interface
    /// </summary>
    public abstract class ICollection_Generic_Tests<T> : IEnumerable_Generic_Tests<T>
        where T : notnull
    {
        #region ICollection<T> Helper Methods

        /// <summary>
        /// Creates an instance of an ICollection{T} that can be used for testing.
        /// </summary>
        /// <returns>An instance of an ICollection{T} that can be used for testing.</returns>
        protected abstract ICollection<T> GenericICollectionFactory();

        /// <summary>
        /// Creates an instance of an ICollection{T} that can be used for testing.
        /// </summary>
        /// <param name="count">The number of unique items that the returned ICollection{T} contains.</param>
        /// <returns>An instance of an ICollection{T} that can be used for testing.</returns>
        protected virtual ICollection<T> GenericICollectionFactory(int count)
        {
            ICollection<T> collection = GenericICollectionFactory();
            AddToCollection(collection, count);
            return collection;
        }

        protected virtual bool DuplicateValuesAllowed => true;
        protected virtual bool DefaultValueWhenNotAllowed_Throws => true;
        protected virtual bool IsReadOnly => false;
        protected virtual bool IsReadOnly_ValidityValue => IsReadOnly;
        protected virtual bool AddRemoveClear_ThrowsNotSupported => false;
        protected virtual bool DefaultValueAllowed => true;
        protected virtual IEnumerable<T> InvalidValues => Array.Empty<T>();

        protected virtual void AddToCollection(ICollection<T> collection, int numberOfItemsToAdd)
        {
            int seed = 9600;
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            while (collection.Count < numberOfItemsToAdd)
            {
                T toAdd = CreateT(seed++);
                while (collection.Contains(toAdd, comparer) || InvalidValues.Contains(toAdd, comparer))
                    toAdd = CreateT(seed++);
                collection.Add(toAdd);
            }
        }

        protected virtual Type ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentException);

        #endregion

        #region IEnumerable<T> Helper Methods

        protected override IEnumerable<T> GenericIEnumerableFactory(int count)
        {
            return GenericICollectionFactory(count);
        }

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
        {
            if (!AddRemoveClear_ThrowsNotSupported && (operations & ModifyOperation.Add) == ModifyOperation.Add)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    var casted = (ICollection<T>)enumerable;
                    casted.Add(CreateT(2344));
                    return true;
                };
            }
            if (!AddRemoveClear_ThrowsNotSupported && (operations & ModifyOperation.Remove) == ModifyOperation.Remove)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    var casted = (ICollection<T>)enumerable;
                    if (casted.Count() > 0)
                    {
                        casted.Remove(casted.ElementAt(0));
                        return true;
                    }
                    return false;
                };
            }
            if (!AddRemoveClear_ThrowsNotSupported && (operations & ModifyOperation.Clear) == ModifyOperation.Clear)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    var casted = (ICollection<T>)enumerable;
                    if (casted.Count() > 0)
                    {
                        casted.Clear();
                        return true;
                    }
                    return false;
                };
            }
        }

        #endregion

        #region IsReadOnly

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_IsReadOnly_Validity(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            Assert.Equal(IsReadOnly_ValidityValue, collection.IsReadOnly);
        }

        #endregion

        #region Count

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Count_Validity(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            Assert.Equal(count, collection.Count);
        }

        #endregion

        #region Add

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_Generic_Add_DefaultValue(int count)
        {
            if (DefaultValueAllowed && !IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T?> collection = GenericICollectionFactory(count)!;
                collection.Add(default(T));
                Assert.Equal(count + 1, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_InvalidValueToMiddleOfCollection(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    ICollection<T> collection = GenericICollectionFactory(count);
                    collection.Add(invalidValue);
                    for (int i = 0; i < count; i++)
                        collection.Add(CreateT(i));
                    Assert.Equal(count * 2, collection.Count);
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_InvalidValueToBeginningOfCollection(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    ICollection<T> collection = GenericICollectionFactory(0);
                    collection.Add(invalidValue);
                    for (int i = 0; i < count; i++)
                        collection.Add(CreateT(i));
                    Assert.Equal(count, collection.Count);
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_InvalidValueToEndOfCollection(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    ICollection<T> collection = GenericICollectionFactory(count);
                    collection.Add(invalidValue);
                    Assert.Equal(count, collection.Count);
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_DuplicateValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DuplicateValuesAllowed)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                T duplicateValue = CreateT(700);
                collection.Add(duplicateValue);
                collection.Add(duplicateValue);
                Assert.Equal(count + 2, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_AfterCallingClear(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                collection.Clear();
                AddToCollection(collection, 5);
                Assert.Equal(5, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_AfterRemovingAnyValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                int seed = 840;
                ICollection<T> collection = GenericICollectionFactory(count);
                List<T> items = collection.ToList();
                T toAdd = CreateT(seed++);
                while (collection.Contains(toAdd))
                    toAdd = CreateT(seed++);
                collection.Add(toAdd);
                collection.Remove(toAdd);

                toAdd = CreateT(seed++);
                while (collection.Contains(toAdd))
                    toAdd = CreateT(seed++);

                collection.Add(toAdd);
                items.Add(toAdd);
                CollectionAsserts.EqualUnordered(items, collection);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_AfterRemovingAllItems(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                List<T> itemsToRemove = collection.ToList();
                for (int i = 0; i < count; i++)
                    collection.Remove(collection.ElementAt(0));
                collection.Add(CreateT(254));
                Assert.Equal(1, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_ToReadOnlyCollection(int count)
        {
            if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                Assert.Throws<NotSupportedException>(() => collection.Add(CreateT(0)));
                Assert.Equal(count, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Add_AfterRemoving(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                int seed = 840;
                ICollection<T> collection = GenericICollectionFactory(count);
                T toAdd = CreateT(seed++);
                while (collection.Contains(toAdd))
                    toAdd = CreateT(seed++);
                collection.Add(toAdd);
                collection.Remove(toAdd);
                collection.Add(toAdd);
            }
        }

        #endregion

        #region Clear

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Clear(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
            {
                Assert.Throws<NotSupportedException>(() => collection.Clear());
                Assert.Equal(count, collection.Count);
            }
            else
            {
                collection.Clear();
                Assert.Equal(0, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Clear_Repeatedly(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
            {
                Assert.Throws<NotSupportedException>(() => collection.Clear());
                Assert.Throws<NotSupportedException>(() => collection.Clear());
                Assert.Throws<NotSupportedException>(() => collection.Clear());
                Assert.Equal(count, collection.Count);
            }
            else
            {
                collection.Clear();
                collection.Clear();
                collection.Clear();
                Assert.Equal(0, collection.Count);
            }
        }

        #endregion

        #region Contains

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Contains_ValidValueOnCollectionNotContainingThatValue(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            int seed = 4315;
            T item = CreateT(seed++);
            while (collection.Contains(item))
                item = CreateT(seed++);
            Assert.False(collection.Contains(item));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Contains_ValidValueOnCollectionContainingThatValue(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            foreach (T item in collection)
                Assert.True(collection.Contains(item));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Contains_DefaultValueOnCollectionNotContainingDefaultValue(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            if (DefaultValueAllowed)
                Assert.False(collection.Contains(default(T)!));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_Generic_Contains_DefaultValueOnCollectionContainingDefaultValue(int count)
        {
            if (DefaultValueAllowed && !IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T?> collection = GenericICollectionFactory(count)!;
                collection.Add(default(T));
                Assert.True(collection.Contains(default(T)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Contains_ValidValueThatExistsTwiceInTheCollection(int count)
        {
            if (DuplicateValuesAllowed && !IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                T item = CreateT(12);
                collection.Add(item);
                collection.Add(item);
                Assert.Equal(count + 2, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Contains_InvalidValue_ThrowsArgumentException(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            Assert.All(InvalidValues, invalidValue =>
                Assert.Throws<ArgumentException>(() => collection.Contains(invalidValue))
            );
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_Generic_Contains_DefaultValueWhenNotAllowed(int count)
        {
            if (!DefaultValueAllowed && !IsReadOnly)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                if (DefaultValueWhenNotAllowed_Throws)
                    Assert.Throws<ArgumentNullException>("item", () => collection.Contains(default(T)!));
                else
                    Assert.False(collection.Contains(default(T)!));
            }
        }

        #endregion

        #region CopyTo

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_CopyTo_NullArray_ThrowsArgumentNullException(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            T[] array = new T[count];
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, int.MinValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_CopyTo_IndexEqualToArrayCount_ThrowsArgumentException(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            T[] array = new T[count];
            if (count > 0)
                Assert.Throws<ArgumentException>(() => collection.CopyTo(array, count));
            else
                collection.CopyTo(array, count); // does nothing since the array is empty
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowsAnyArgumentException(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            T[] array = new T[count];
            Assert.Throws(ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType, () => collection.CopyTo(array, count + 1));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_CopyTo_NotEnoughSpaceInOffsettedArray_ThrowsArgumentException(int count)
        {
            if (count > 0) // Want the T array to have at least 1 element
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                T[] array = new T[count];
                Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 1));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_CopyTo_ExactlyEnoughSpaceInArray(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            T[] array = new T[count];
            collection.CopyTo(array, 0);
            Assert.True(Enumerable.SequenceEqual(collection, array));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_CopyTo_ArrayIsLargerThanCollection(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            T[] array = new T[count * 3 / 2];
            collection.CopyTo(array, 0);
            Assert.True(Enumerable.SequenceEqual(collection, array.Take(count)));
        }

        #endregion

        #region Remove

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_OnReadOnlyCollection_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                Assert.Throws<NotSupportedException>(() => collection.Remove(CreateT(34543)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_DefaultValueNotContainedInCollection(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DefaultValueAllowed && !Enumerable.Contains(InvalidValues, default(T)))
            {
                int seed = count * 21;
                ICollection<T?> collection = GenericICollectionFactory(count)!;
                T? value = default(T);
                while (collection.Contains(value))
                {
                    collection.Remove(value);
                    count--;
                }
                Assert.False(collection.Remove(value));
                Assert.Equal(count, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_NonDefaultValueNotContainedInCollection(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                int seed = count * 251;
                ICollection<T> collection = GenericICollectionFactory(count);
                T value = CreateT(seed++);
                while (collection.Contains(value) || Enumerable.Contains(InvalidValues, value))
                    value = CreateT(seed++);
                Assert.False(collection.Remove(value));
                Assert.Equal(count, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void ICollection_Generic_Remove_DefaultValueContainedInCollection(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DefaultValueAllowed && !Enumerable.Contains(InvalidValues, default(T)))
            {
                int seed = count * 21;
                ICollection<T?> collection = GenericICollectionFactory(count)!;
                T? value = default(T);
                if (!collection.Contains(value))
                {
                    collection.Add(value);
                    count++;
                }
                Assert.True(collection.Remove(value));
                Assert.Equal(count - 1, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_NonDefaultValueContainedInCollection(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                int seed = count * 251;
                ICollection<T> collection = GenericICollectionFactory(count);
                T value = CreateT(seed++);
                if (!collection.Contains(value))
                {
                    collection.Add(value);
                    count++;
                }
                Assert.True(collection.Remove(value));
                Assert.Equal(count - 1, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_ValueThatExistsTwiceInCollection(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DuplicateValuesAllowed)
            {
                int seed = count * 90;
                ICollection<T> collection = GenericICollectionFactory(count);
                T value = CreateT(seed++);
                collection.Add(value);
                collection.Add(value);
                count += 2;
                Assert.True(collection.Remove(value));
                Assert.True(collection.Contains(value));
                Assert.Equal(count - 1, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_EveryValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                Assert.All(collection.ToList(), value =>
                {
                    Assert.True(collection.Remove(value));
                });
                Assert.Empty(collection);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_InvalidValue_ThrowsArgumentException(int count)
        {
            ICollection<T> collection = GenericICollectionFactory(count);
            Assert.All(InvalidValues, value =>
            {
                Assert.Throws<ArgumentException>(() => collection.Remove(value));
            });
            Assert.Equal(count, collection.Count);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_DefaultValueWhenNotAllowed(int count)
        {
            if (!DefaultValueAllowed && !IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                ICollection<T> collection = GenericICollectionFactory(count);
                if (DefaultValueWhenNotAllowed_Throws)
                    Assert.Throws<ArgumentNullException>(() => collection.Remove(default(T)!));
                else
                    Assert.False(collection.Remove(default(T)!));
            }
        }

        #endregion
    }
}
