// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class BitArrayTests
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
                    int n = BitArray.BitsPerWord * a + b;
                    if (n < 0) continue;
                    BitArray arr = BitArray.AllSet(n);
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
                CheckRandomData(r1, r2, capacity);

            for (int i = 0; i < rounds; i++)
            {
                int capacity = r1.Next(maxBits);
                Assert.Equal(r2.Next(maxBits), capacity);
                CheckRandomData(r1, r2, capacity);
            }
        }

        private void CheckRandomData(Random r1, Random r2, int capacity)
        {
            BitArray d = BitArray.Create(capacity);
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
                CheckIntersection(capacity, r);
            for (int i = 0; i < rounds; i++)
            {
                CheckIntersection(r.Next(maxBits), r);
            }
        }

        private void CheckIntersection(int capacity, Random r)
        {
            BitArray b1 = BitArray.Empty, b2 = BitArray.Empty;
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
                CheckUnion(capacity, r);
            for (int i = 0; i < rounds; i++)
            {
                CheckUnion(r.Next(maxBits), r);
            }
        }

        private void CheckUnion(int capacity, Random r)
        {
            BitArray b1 = BitArray.Empty, b2 = BitArray.Empty;
            b1.EnsureCapacity(capacity);
            b2.EnsureCapacity(capacity);
            bool[] a1 = new bool[capacity], a2 = new bool[capacity];
            for (int i = 0; i < capacity; i++)
            {
                b1[i] = a1[i] = r.NextBool();
                b2[i] = a2[i] = r.NextBool();
            }
            b1.UnionWith(b2);
            for (int i = 0; i < capacity; i++)
            {
                a1[i] |= a2[i];
            }
            for (int i = 0; i < capacity; i++)
            {
                Assert.Equal(a1[i], b1[i]);
            }
        }

        [Fact]
        public void CheckTrueBits()
        {
            var r1 = new Random(seed);
            var r2 = new Random(seed);
            for (int capacity = 0; capacity < maxBits; capacity++)
            {
                CheckTrueBits(capacity, r1, r2);
            }
            for (int i = 0; i < rounds; i++)
            {
                var capacity = r1.Next(maxBits);
                Assert.Equal(capacity, r2.Next(maxBits));
                CheckTrueBits(capacity, r1, r2);
            }
        }

        private void CheckTrueBits(int capacity, Random r1, Random r2)
        {
            BitArray b = BitArray.Create(capacity);
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
    }

    static class RandomExtensions
    {
        public static bool NextBool(this Random self)
        {
            return self.Next(2) == 0;
        }
    }
}
