// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class List_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void BinarySearch_ForEveryItemWithoutDuplicates(int count)
        {
            List<T> list = GenericListFactory(count);
            foreach (T item in list)
                while (list.Count((value) => value.Equals(item)) > 1)
                    list.Remove(item);
            list.Sort();
            List<T> beforeList = list.ToList();

            Assert.All(Enumerable.Range(0, list.Count), index =>
            {
                Assert.Equal(index, list.BinarySearch(beforeList[index]));
                Assert.Equal(index, list.BinarySearch(beforeList[index], GetIComparer()));
                Assert.Equal(beforeList[index], list[index]);
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void BinarySearch_ForEveryItemWithDuplicates(int count)
        {
            if (count > 0)
            {
                List<T> list = GenericListFactory(count);
                list.Add(list[0]);
                list.Sort();
                List<T> beforeList = list.ToList();

                Assert.All(Enumerable.Range(0, list.Count), index =>
                {
                    Assert.True(list.BinarySearch(beforeList[index]) >= 0);
                    Assert.True(list.BinarySearch(beforeList[index], GetIComparer()) >= 0);
                    Assert.Equal(beforeList[index], list[index]);
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void BinarySearch_Validations(int count)
        {
            List<T> list = GenericListFactory(count);
            list.Sort();
            T element = CreateT(3215);
            AssertExtensions.Throws<ArgumentException>(null, () => list.BinarySearch(0, count + 1, element, GetIComparer())); //"Finding items longer than array should throw ArgumentException"
            Assert.Throws<ArgumentOutOfRangeException>(() => list.BinarySearch(-1, count, element, GetIComparer())); //"ArgumentOutOfRangeException should be thrown on negative index."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.BinarySearch(0, -1, element, GetIComparer())); //"ArgumentOutOfRangeException should be thrown on negative count."
            AssertExtensions.Throws<ArgumentException>(null, () => list.BinarySearch(count + 1, count, element, GetIComparer())); //"ArgumentException should be thrown on index greater than length of array."
        }
    }
}
