// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class SegmentedArrayTests
    {
        public static IEnumerable<object[]> TestLengths
        {
            get
            {
                yield return new object[] { 1 };
                yield return new object[] { 10 };
                yield return new object[] { 100 };
                yield return new object[] { SegmentedArray<IntPtr>.TestAccessor.SegmentSize / 2 };
                yield return new object[] { SegmentedArray<IntPtr>.TestAccessor.SegmentSize };
                yield return new object[] { SegmentedArray<IntPtr>.TestAccessor.SegmentSize * 2 };
                yield return new object[] { 100000 };
            }
        }

        private static void ResetToSequence(SegmentedArray<IntPtr> array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = (IntPtr)i;
            }
        }

        [Fact]
        public void TestDefaultInstance()
        {
            var data = default(SegmentedArray<IntPtr>);
            Assert.Null(data.GetTestAccessor().Items);

            Assert.True(data.IsFixedSize);
            Assert.True(data.IsReadOnly);
            Assert.False(data.IsSynchronized);
            Assert.Equal(0, data.Length);
            Assert.Null(data.SyncRoot);

            Assert.Throws<NullReferenceException>(() => data[0]);
            Assert.Throws<NullReferenceException>(() => ((IReadOnlyList<IntPtr>)data)[0]);
            Assert.Throws<NullReferenceException>(() => ((IList<IntPtr>)data)[0]);
            Assert.Throws<NullReferenceException>(() => ((IList<IntPtr>)data)[0] = IntPtr.Zero);
            Assert.Throws<NullReferenceException>(() => ((IList)data)[0]);
            Assert.Throws<NullReferenceException>(() => ((IList)data)[0] = IntPtr.Zero);

            Assert.Equal(0, ((ICollection)data).Count);
            Assert.Equal(0, ((ICollection<IntPtr>)data).Count);
            Assert.Equal(0, ((IReadOnlyCollection<IntPtr>)data).Count);

            Assert.Throws<NullReferenceException>(() => data.Clone());
            Assert.Throws<NullReferenceException>(() => data.CopyTo(Array.Empty<IntPtr>(), 0));
            Assert.Throws<NullReferenceException>(() => ((ICollection<IntPtr>)data).CopyTo(Array.Empty<IntPtr>(), 0));

            var enumerator1 = data.GetEnumerator();
            Assert.Throws<NullReferenceException>(() => enumerator1.MoveNext());

            var enumerator2 = ((IEnumerable)data).GetEnumerator();
            Assert.Throws<NullReferenceException>(() => enumerator1.MoveNext());

            var enumerator3 = ((IEnumerable<IntPtr>)data).GetEnumerator();
            Assert.Throws<NullReferenceException>(() => enumerator1.MoveNext());

            Assert.Throws<NotSupportedException>(() => ((IList)data).Add(IntPtr.Zero));
            Assert.Throws<NotSupportedException>(() => ((ICollection<IntPtr>)data).Add(IntPtr.Zero));
            Assert.Throws<NotSupportedException>(() => ((ICollection<IntPtr>)data).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList)data).Insert(0, IntPtr.Zero));
            Assert.Throws<NotSupportedException>(() => ((IList<IntPtr>)data).Insert(0, IntPtr.Zero));
            Assert.Throws<NotSupportedException>(() => ((IList)data).Remove(IntPtr.Zero));
            Assert.Throws<NotSupportedException>(() => ((ICollection<IntPtr>)data).Remove(IntPtr.Zero));
            Assert.Throws<NotSupportedException>(() => ((IList)data).RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => ((IList<IntPtr>)data).RemoveAt(0));

            Assert.Throws<NullReferenceException>(() => ((IList)data).Clear());
            Assert.Throws<NullReferenceException>(() => ((IList)data).Contains(IntPtr.Zero));
            Assert.Throws<NullReferenceException>(() => ((ICollection<IntPtr>)data).Contains(IntPtr.Zero));
            Assert.Throws<NullReferenceException>(() => ((IList)data).IndexOf(IntPtr.Zero));
            Assert.Throws<NullReferenceException>(() => ((IList<IntPtr>)data).IndexOf(IntPtr.Zero));
        }

        [Fact]
        public void TestConstructor1()
        {
            Assert.Throws<ArgumentOutOfRangeException>("length", () => new SegmentedArray<byte>(-1));

            Assert.Empty(new SegmentedArray<byte>(0));
            Assert.Same(Array.Empty<byte[]>(), new SegmentedArray<byte>(0).GetTestAccessor().Items);
        }

        [Theory]
        [MemberData(nameof(TestLengths))]
        public void TestConstructor2(int length)
        {
            var data = new SegmentedArray<IntPtr>(length);
            Assert.Equal(length, data.Length);

            var items = data.GetTestAccessor().Items;
            Assert.Equal(length, items.Sum(item => item.Length));

            for (var i = 0; i < items.Length - 1; i++)
            {
                Assert.Equal(SegmentedArray<IntPtr>.TestAccessor.SegmentSize, items[i].Length);
                Assert.True(items[i].Length <= SegmentedArray<IntPtr>.TestAccessor.SegmentSize);
            }
        }

        [Theory]
        [MemberData(nameof(TestLengths))]
        public void TestBasicProperties(int length)
        {
            var data = new SegmentedArray<IntPtr>(length);

            Assert.True(data.IsFixedSize);
            Assert.True(data.IsReadOnly);
            Assert.False(data.IsSynchronized);
            Assert.Equal(length, data.Length);
            Assert.Same(data.GetTestAccessor().Items, data.SyncRoot);

            Assert.Equal(length, ((ICollection)data).Count);
            Assert.Equal(length, ((ICollection<IntPtr>)data).Count);
            Assert.Equal(length, ((IReadOnlyCollection<IntPtr>)data).Count);
        }

        [Theory]
        [MemberData(nameof(TestLengths))]
        public void TestIndexer(int length)
        {
            var data = new SegmentedArray<IntPtr>(length);
            ResetToSequence(data);

            for (var i = 0; i < length; i++)
            {
                data[i] = (IntPtr)i;
            }

            for (var i = 0; i < length; i++)
            {
                Assert.Equal((IntPtr)i, data[i]);
            }

            for (var i = 0; i < length; i++)
            {
                ref var value = ref data[i];
                Assert.Equal((IntPtr)i, data[i]);
                value = IntPtr.Add(value, 1);

                Assert.Equal((IntPtr)(i + 1), value);
                Assert.Equal((IntPtr)(i + 1), data[i]);
            }

            ResetToSequence(data);
            for (var i = 0; i < length; i++)
            {
                Assert.Equal((IntPtr)i, ((IReadOnlyList<IntPtr>)data)[i]);
                data[i] = IntPtr.Add(data[i], 1);
                Assert.Equal((IntPtr)(i + 1), ((IReadOnlyList<IntPtr>)data)[i]);
            }

            ResetToSequence(data);
            for (var i = 0; i < length; i++)
            {
                Assert.Equal((IntPtr)i, ((IList<IntPtr>)data)[i]);
                ((IList<IntPtr>)data)[i] = IntPtr.Add(data[i], 1);
                Assert.Equal((IntPtr)(i + 1), ((IList<IntPtr>)data)[i]);
            }

            ResetToSequence(data);
            for (var i = 0; i < length; i++)
            {
                Assert.Equal((IntPtr)i, ((IList)data)[i]);
                ((IList)data)[i] = IntPtr.Add(data[i], 1);
                Assert.Equal((IntPtr)(i + 1), ((IList)data)[i]);
            }
        }

        /// <summary>
        /// Verify that indexing and iteration match for an array with many segments.
        /// </summary>
        [Fact]
        public void TestIterateLargeArray()
        {
            var data = new SegmentedArray<Guid>(1000000);
            Assert.True(data.GetTestAccessor().Items.Length > 10);

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = Guid.NewGuid();
                Assert.NotEqual(Guid.Empty, data[i]);
            }

            var index = 0;
            foreach (var guid in data)
            {
                Assert.Equal(guid, data[index++]);
            }

            Assert.Equal(data.Length, index);
        }

        [Fact]
        public void CopyOverlappingEndOfSegment()
        {
            var array = new int[2 * SegmentedArray<int>.TestAccessor.SegmentSize];
            var segmented = new SegmentedArray<int>(2 * SegmentedArray<int>.TestAccessor.SegmentSize);
            initialize(array, segmented);
            Assert.Equal(array, segmented);

            var sourceStart = SegmentedArray<int>.TestAccessor.SegmentSize - 128;
            var destinationStart = SegmentedArray<int>.TestAccessor.SegmentSize - 60;
            var length = 256;
            Array.Copy(array, sourceStart, array, destinationStart, length);
            SegmentedArray.Copy(segmented, sourceStart, segmented, destinationStart, length);
            Assert.Equal(array, segmented);

            initialize(array, segmented);
            sourceStart = SegmentedArray<int>.TestAccessor.SegmentSize - 60;
            destinationStart = SegmentedArray<int>.TestAccessor.SegmentSize - 128;
            length = 256;
            Array.Copy(array, sourceStart, array, destinationStart, length);
            SegmentedArray.Copy(segmented, sourceStart, segmented, destinationStart, length);
            Assert.Equal(array, segmented);

            static void initialize(int[] array, SegmentedArray<int> segmented)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = i;
                    segmented[i] = i;
                }
            }
        }
    }
}
