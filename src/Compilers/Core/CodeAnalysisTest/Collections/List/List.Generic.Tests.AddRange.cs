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
        // Has tests that pass a variably sized TestCollection and MyEnumerable to the AddRange function

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        public void AddRange(EnumerableType enumerableType, int listLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            List<T> list = GenericListFactory(listLength);
            List<T> listBeforeAdd = list.ToList();
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, list, enumerableLength, numberOfMatchingElements, numberOfDuplicateElements);
            list.AddRange(enumerable);

            // Check that the first section of the List is unchanged
            Assert.All(Enumerable.Range(0, listLength), index =>
            {
                Assert.Equal(listBeforeAdd[index], list[index]);
            });

            // Check that the added elements are correct
            Assert.All(Enumerable.Range(0, enumerableLength), index =>
            {
                Assert.Equal(enumerable.ElementAt(index), list[index + listLength]);
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void AddRange_NullEnumerable_ThrowsArgumentNullException(int count)
        {
            List<T> list = GenericListFactory(count);
            List<T> listBeforeAdd = list.ToList();
            Assert.Throws<ArgumentNullException>(() => list.AddRange(null));
            Assert.Equal(listBeforeAdd, list);
        }

        [Fact]
        public void AddRange_AddSelfAsEnumerable_ThrowsExceptionWhenNotEmpty()
        {
            List<T> list = GenericListFactory(0);

            // Succeeds when list is empty.
            list.AddRange(list);
            list.AddRange(list.Where(_ => true));

            // Succeeds when list has elements and is added as collection.
            list.Add(default);
            Assert.Equal(1, list.Count);
            list.AddRange(list);
            Assert.Equal(2, list.Count);
            list.AddRange(list);
            Assert.Equal(4, list.Count);

            // Fails version check when list has elements and is added as non-collection.
            Assert.Throws<InvalidOperationException>(() => list.AddRange(list.Where(_ => true)));
            Assert.Equal(5, list.Count);
            Assert.Throws<InvalidOperationException>(() => list.AddRange(list.Where(_ => true)));
            Assert.Equal(6, list.Count);
        }
    }
}
