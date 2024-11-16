// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class SpannableArrayBuilderTests
    {
        [Theory]
        [InlineData(new[] { 6, 5, 1, 2, 3, 2, 4, 5, 1, 7 })]
        public void Add(int[] toAdd)
        {
            var builder = new SpannableArrayBuilder<int>();

            foreach (var item in toAdd)
                builder.Add(item);

            Assert.Equal(toAdd.Length, builder.Count);
            for (int i = 0; i < toAdd.Length; i++)
                Assert.Equal(toAdd[i], builder[i]);
        }

        [Theory]
        [InlineData(new[] { 6, 5, 1, 2, 3, 2, 4, 5, 1, 7 })]
        public void AddRange(int[] toAdd)
        {
            var builder = new SpannableArrayBuilder<int>();
            builder.AddRange(toAdd);

            Assert.Equal(toAdd.Length, builder.Count);
            for (int i = 0; i < toAdd.Length; i++)
                Assert.Equal(toAdd[i], builder[i]);
        }

        [Theory]
        [InlineData(4, 6, 8)]
        [InlineData(4, 10, 10)]
        public void EnsureCapacity(int initialCapacity, int newSpecifiedCapacity, int newExpectedCapacity)
        {
            var builder = new SpannableArrayBuilder<int>(initialCapacity);

            Assert.Equal(initialCapacity, builder.Capacity);

            builder.EnsureCapacity(newSpecifiedCapacity);
            Assert.Equal(newExpectedCapacity, builder.Capacity);
        }

        [Theory]
        [InlineData(new[] { 6, 5, 1, 2, 3, 2, 4, 5, 1, 7 })]
        public void Clear(int[] toAdd)
        {
            var builder = new SpannableArrayBuilder<int>();
            builder.AddRange(toAdd);

            Assert.Equal(toAdd.Length, builder.Count);

            builder.Clear();
            Assert.Equal(0, builder.Count);
        }

        [Theory]
        [InlineData(new[] { 6, 5, 1, 2, 3, 2, 4, 5, 1, 7 })]
        public void AsSpan(int[] toAdd)
        {
            var builder = new SpannableArrayBuilder<int>();
            builder.AddRange(toAdd);

            var enumeratedValues = new List<int>();
            foreach (var item in builder.AsSpan())
                enumeratedValues.Add(item);

            Assert.True(enumeratedValues.SequenceEqual(toAdd));
        }

        [Fact]
        public void Pooling()
        {
            var builder1 = SpannableArrayBuilder<int>.GetInstance();
            var builder2 = SpannableArrayBuilder<int>.GetInstance();
            Assert.NotSame(builder1, builder2);

            builder2.Free();

            var builder3 = SpannableArrayBuilder<int>.GetInstance();
            Assert.Same(builder2, builder3);

            builder3.Free();
            builder1.Free();

            var builder4 = SpannableArrayBuilder<int>.GetInstance();
            var builder5 = SpannableArrayBuilder<int>.GetInstance();

            Assert.Same(builder4, builder2);
            Assert.Same(builder5, builder1);
        }
    }
}
