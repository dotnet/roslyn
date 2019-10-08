// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Analyzer.Utilities.Extensions
{
    public class IEnumerableExensionsTests
    {
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [Theory]
        public void IEnumerableHasExactly2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasExactly(CreateIEnumerable(count), 2));
        }

        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        [Theory]
        public void IEnumerableHasMoreThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasMoreThan(CreateIEnumerable(count), 2));
        }

        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [Theory]
        public void IEnumerableHasFewerThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasFewerThan(CreateIEnumerable(count), 2));
        }

        private static IEnumerable<int> CreateIEnumerable(int count)
        {
            if (count < 0)
            {
                count = 0;
            }

            for (var i = count; i > 0; i--)
            {
                yield return 0;
            }
        }

        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [Theory]
        public void ICollectionHasExactly2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasExactly(new Collection(count), 2));
        }

        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        [Theory]
        public void ICollectionHasMoreThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasMoreThan(new Collection(count), 2));
        }

        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [Theory]
        public void ICollectionHasFewerThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasFewerThan(new Collection(count), 2));
        }

#pragma warning disable CA1010 // Collections should implement generic interface
        private class Collection : ICollection, IEnumerable<int>
        {
            public Collection(int count) => this.Count = count > 0 ? count : 0;
            public int Count { get; }
            public object SyncRoot => throw new NotImplementedException();
            public bool IsSynchronized => throw new NotImplementedException();
            public void CopyTo(Array array, int index) => throw new NotImplementedException();
            public IEnumerator GetEnumerator() => throw new NotImplementedException();
            IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
        }
#pragma warning restore CA1010 // Collections should implement generic interface

        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [Theory]
        public void IIntCollectionHasExactly2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasExactly(new IntCollection(count), 2));
        }

        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        [Theory]
        public void IIntCollectionHasMoreThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasMoreThan(new IntCollection(count), 2));
        }

        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [Theory]
        public void IIntCollectionHasFewerThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasFewerThan(new IntCollection(count), 2));
        }

        private class IntCollection : ICollection<int>
        {
            public IntCollection(int count) => this.Count = count > 0 ? count : 0;
            public int Count { get; }
            public bool IsReadOnly { get; }
            public void Add(int item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(int item) => throw new NotImplementedException();
            public void CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();
            public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
            public bool Remove(int item) => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}
