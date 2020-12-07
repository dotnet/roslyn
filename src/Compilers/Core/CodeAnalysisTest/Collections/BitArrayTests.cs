// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class BitVectorTests
    {
        private const int seed = 128129347;
        private const int rounds = 200;
        private const int maxBits = 132;

        [Fact]
        public void UpperBitsUnset()
        {
            for (int a = 0; a < 5; a++) // number of words
            {
                for (int b = -1; b < 2; b++) // number of bits more or less than that number of words
                {
                    int n = BitVector.BitsPerWord * a + b;
                    if (n < 0) continue;
                    BitVector arr = BitVector.AllSet(n);
                    if (n > 0) Assert.True(arr[n - 1]);
                    Assert.False(arr[n]);
                }
            }
        }

        [Fact]
        public void CheckRandomData()
        {
            var r1 = new Random(seed);
            var r2 = new Random(seed);

            for (int capacity = 0; capacity < maxBits; capacity++)
                CheckRandomDataCore(r1, r2, capacity);

            for (int i = 0; i < rounds; i++)
            {
                int capacity = r1.Next(maxBits);
                Assert.Equal(r2.Next(maxBits), capacity);
                CheckRandomDataCore(r1, r2, capacity);
            }
        }

        private void CheckRandomDataCore(Random r1, Random r2, int capacity)
        {
            BitVector d = BitVector.Create(capacity);
            Assert.Equal(capacity, d.Capacity);
            for (int i1 = 0; i1 < capacity; i1++)
                d[i1] = r1.NextBool();
            Assert.Equal(capacity, d.Capacity);

            for (int i2 = 0; i2 < capacity; i2++)
                Assert.Equal(d[i2], r2.NextBool());
        }

        [Fact]
        public void CheckIntersection()
        {
            var r = new Random(seed);
            for (int capacity = 0; capacity < maxBits; capacity++)
                CheckIntersectionCore(capacity, r);
            for (int i = 0; i < rounds; i++)
            {
                CheckIntersectionCore(r.Next(maxBits), r);
            }
        }

        private void CheckIntersectionCore(int capacity, Random r)
        {
            BitVector b1 = BitVector.Empty, b2 = BitVector.Empty;
            b1.EnsureCapacity(capacity);
            b2.EnsureCapacity(capacity);
            bool[] a1 = new bool[capacity], a2 = new bool[capacity];
            for (int i = 0; i < capacity; i++)
            {
                b1[i] = a1[i] = r.NextBool();
                b2[i] = a2[i] = r.NextBool();
            }
            bool changed = b1.IntersectWith(b2);
            bool changed2 = false;
            for (int i = 0; i < capacity; i++)
            {
                bool a = a1[i];
                a1[i] &= a2[i];
                changed2 |= (a != a1[i]);
            }
            for (int i = 0; i < capacity; i++)
            {
                Assert.Equal(a1[i], b1[i]);
            }
            Assert.Equal(changed, changed2);
        }

        [Fact]
        public void CheckUnion()
        {
            var r = new Random(seed);
            for (int capacity = 0; capacity < maxBits; capacity++)
            {
                CheckUnionCore(capacity, capacity, r);
            }

            for (int i = 0; i < rounds; i++)
            {
                CheckUnionCore(r.Next(maxBits), r.Next(maxBits), r);
            }
        }

        private void CheckUnionCore(int capacity1, int capacity2, Random r)
        {
            BitVector b1 = BitVector.Empty, b2 = BitVector.Empty;
            b1.EnsureCapacity(capacity1);
            b2.EnsureCapacity(capacity2);

            var maxCapacity = Math.Max(capacity1, capacity2);
            bool[] a1 = new bool[maxCapacity],
                   a2 = new bool[maxCapacity];

            for (int i = 0; i < capacity1; i++)
            {
                b1[i] = a1[i] = r.NextBool();
            }

            for (int i = 0; i < capacity2; i++)
            {
                b2[i] = a2[i] = r.NextBool();
            }

            bool changed = b1.UnionWith(b2);
            bool changed2 = false;

            for (int i = 0; i < maxCapacity; i++)
            {
                bool a = a1[i];
                a1[i] |= a2[i];
                changed2 |= (a != a1[i]);
            }
            for (int i = 0; i < maxCapacity; i++)
            {
                Assert.Equal(a1[i], b1[i]);
            }
            Assert.Equal(changed2, changed);
        }

        [Fact]
        public void CheckTrueBits()
        {
            var r1 = new Random(seed);
            var r2 = new Random(seed);
            for (int capacity = 0; capacity < maxBits; capacity++)
            {
                CheckTrueBitsCore(capacity, r1, r2);
            }
            for (int i = 0; i < rounds; i++)
            {
                var capacity = r1.Next(maxBits);
                Assert.Equal(capacity, r2.Next(maxBits));
                CheckTrueBitsCore(capacity, r1, r2);
            }
        }

        [Fact]
        public void CheckWords()
        {
            for (int capacity = 0; capacity < maxBits; capacity++)
            {
                BitVector b = BitVector.Create(capacity);
                for (int i = 0; i < capacity; i++)
                {
                    b[i] = false;
                }

                var required = BitVector.WordsRequired(capacity);
                var count = BitVector.AllSet(capacity).Words().Count();

                Assert.Equal(required, count);
            }
        }

        [Fact]
        public void CheckIsTrue()
        {
            var r1 = new Random(seed);

            for (int capacity = 0; capacity < maxBits; capacity++)
            {
                BitVector b = BitVector.Create(capacity);
                for (int i = 0; i < capacity; i++)
                {
                    b[i] = r1.NextBool();
                }

                var index = 0;
                foreach (var word in b.Words())
                {
                    for (var i = 0; i < BitVector.BitsPerWord; i++)
                    {
                        if (index >= capacity)
                        {
                            break;
                        }

                        Assert.Equal(b[index], BitVector.IsTrue(word, index));

                        index++;
                    }
                }
            }
        }

        [Fact]
        public void CheckWordsRequired()
        {
            for (int capacity = 0; capacity < maxBits; capacity++)
            {
                var required = BitVector.WordsRequired(capacity);
                var count = BitVector.AllSet(capacity).Words().Count();

                Assert.Equal(count, required);
            }
        }

        private void CheckTrueBitsCore(int capacity, Random r1, Random r2)
        {
            BitVector b = BitVector.Create(capacity);
            for (int i = 0; i < capacity; i++)
            {
                b[i] = r1.NextBool();
            }

            IEnumerable<int> i1 = b.TrueBits();
            IEnumerator<int> i2 = i1.GetEnumerator();
            for (int i = 0; i < capacity; i++)
            {
                if (r2.NextBool())
                {
                    Assert.True(i2.MoveNext());
                    Assert.Equal(i2.Current, i);
                }
            }

            Assert.False(i2.MoveNext());
            i2.Dispose();
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 128)]
        [InlineData(65, 64)]
        [InlineData(65, 65)]
        [InlineData(65, 128)]
        public void IndexerGet(int capacity, int index)
        {
            var b = BitVector.Create(capacity);
            Assert.Equal(capacity, b.Capacity);

            Assert.False(b[index]);
            Assert.Equal(capacity, b.Capacity);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 128)]
        [InlineData(65, 64)]
        [InlineData(65, 65)]
        [InlineData(65, 128)]
        public void IndexerSet(int capacity, int index)
        {
            var b = BitVector.Create(capacity);
            Assert.Equal(capacity, b.Capacity);
            capacity = Math.Max(capacity, index + 1);

            b[index] = true;
            Assert.True(b[index]);
            Assert.Equal(capacity, b.Capacity);

            b[index] = false;
            Assert.False(b[index]);
            Assert.Equal(capacity, b.Capacity);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, -128)]
        [InlineData(65, -1)]
        [InlineData(65, -128)]
        public void IndexerGet_OutOfRange(int capacity, int index)
        {
            var b = BitVector.Create(capacity);
            Assert.Equal(capacity, b.Capacity);

            Assert.Throws<IndexOutOfRangeException>(() => _ = b[index]);
            Assert.Equal(capacity, b.Capacity);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, -128)]
        [InlineData(65, -1)]
        [InlineData(65, -128)]
        public void IndexerSet_OutOfRange(int capacity, int index)
        {
            var b = BitVector.Create(capacity);
            Assert.Equal(capacity, b.Capacity);

            Assert.Throws<IndexOutOfRangeException>(() => b[index] = false);
            Assert.Equal(capacity, b.Capacity);

            Assert.Throws<IndexOutOfRangeException>(() => b[index] = true);
            Assert.Equal(capacity, b.Capacity);
        }
    }

    internal static class RandomExtensions
    {
        public static bool NextBool(this Random self)
        {
            return self.Next(2) == 0;
        }
    }
}
