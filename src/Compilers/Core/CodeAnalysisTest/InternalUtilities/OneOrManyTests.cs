// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.InternalUtilities
{
    public class OneOrManyTests : TestBase
    {
        private static void Verify<T>(OneOrMany<T> actual, params T[] expected)
            where T : notnull
        {
            Assert.Equal(actual.Count, expected.Length);
            int n = actual.Count;
            int i;
            for (i = 0; i < n; i++)
            {
                Assert.Equal(actual[i], expected[i]);
            }
            i = 0;
            foreach (var value in actual)
            {
                Assert.Equal(value, expected[i]);
                i++;
            }
            Assert.Equal(n, i);
        }

        [Fact]
        public void CreateZero()
        {
            Verify(OneOrMany.Create(ImmutableArray<int>.Empty));
            Verify(new OneOrMany<int>(ImmutableArray<int>.Empty));
        }

        [Fact]
        public void CreateOne()
        {
            Verify(OneOrMany.Create(1), 1);
            Verify(OneOrMany.Create(ImmutableArray.Create(2)), 2);
            Verify(OneOrMany.Create(ImmutableArray<int>.Empty).Add(3), 3);
            Verify(new OneOrMany<int>(1), 1);
            Verify(new OneOrMany<int>(ImmutableArray.Create(2)), 2);
            Verify(new OneOrMany<int>(ImmutableArray<int>.Empty).Add(3), 3);
        }

        [Fact]
        public void CreateArray()
        {
            Verify(OneOrMany.Create(ImmutableArray.Create(1, 2, 3)).Add(4), 1, 2, 3, 4);
            Verify(OneOrMany.Create(ImmutableArray.Create(1, 2, 3, 4)), 1, 2, 3, 4);
            Verify(OneOrMany.Create(ImmutableArray<int>.Empty).Add(1).Add(2).Add(3).Add(4), 1, 2, 3, 4);
            Verify(new OneOrMany<int>(ImmutableArray.Create(1, 2, 3)).Add(4), 1, 2, 3, 4);
            Verify(new OneOrMany<int>(ImmutableArray.Create(1, 2, 3, 4)), 1, 2, 3, 4);
            Verify(new OneOrMany<int>(ImmutableArray<int>.Empty).Add(1).Add(2).Add(3).Add(4), 1, 2, 3, 4);
            Verify(OneOrMany.Create(ImmutableArray.Create(1)).Add(4), 1, 4);
            Verify(OneOrMany.Create(ImmutableArray.Create(1)), 1);
        }

        [Fact]
        public void Contains()
        {
            Assert.True(OneOrMany.Create(1).Contains(1));
            Assert.False(OneOrMany.Create(1).Contains(0));

            Assert.False(OneOrMany.Create(ImmutableArray<int>.Empty).Contains(0));

            Assert.True(OneOrMany.Create(ImmutableArray.Create(1)).Contains(1));
            Assert.False(OneOrMany.Create(ImmutableArray.Create(1)).Contains(0));

            Assert.True(OneOrMany.Create(ImmutableArray.Create(1, 2)).Contains(1));
            Assert.True(OneOrMany.Create(ImmutableArray.Create(1, 2)).Contains(2));
            Assert.False(OneOrMany.Create(ImmutableArray.Create(1, 2)).Contains(0));
        }

        [Fact]
        public void Select()
        {
            Verify(OneOrMany.Create(1).Select(i => i + 1), 2);
            Verify(OneOrMany.Create(ImmutableArray<int>.Empty).Select(i => i + 1));
            Verify(OneOrMany.Create(ImmutableArray.Create(1)).Select(i => i + 1), 2);
            Verify(OneOrMany.Create(ImmutableArray.Create(1, 2)).Select(i => i + 1), 2, 3);
        }

        [Fact]
        public void SelectWithArg()
        {
            Verify(OneOrMany.Create(1).Select((i, a) => i + a, 1), 2);
            Verify(OneOrMany.Create(ImmutableArray<int>.Empty).Select((i, a) => i + a, 1));
            Verify(OneOrMany.Create(ImmutableArray.Create(1)).Select((i, a) => i + a, 1), 2);
            Verify(OneOrMany.Create(ImmutableArray.Create(1, 2)).Select((i, a) => i + a, 1), 2, 3);
        }

        [Fact]
        public void FirstOrDefault()
        {
            Assert.Equal(1, OneOrMany.Create(1).FirstOrDefault(i => i < 2));
            Assert.Equal(0, OneOrMany.Create(1).FirstOrDefault(i => i > 2));
            Assert.Equal(0, OneOrMany.Create(ImmutableArray<int>.Empty).FirstOrDefault(i => i > 2));
            Assert.Equal(1, OneOrMany.Create(ImmutableArray.Create(1)).FirstOrDefault(i => i < 2));
            Assert.Equal(0, OneOrMany.Create(ImmutableArray.Create(1)).FirstOrDefault(i => i > 2));
            Assert.Equal(1, OneOrMany.Create(ImmutableArray.Create(1, 3)).FirstOrDefault(i => i < 2));
            Assert.Equal(2, OneOrMany.Create(ImmutableArray.Create(1, 3)).FirstOrDefault(i => i > 2));
        }

        [Fact]
        public void Errors()
        {
            var single = OneOrMany.Create(123);
            var quad = OneOrMany.Create(ImmutableArray.Create<int>(10, 20, 30, 40));

            Assert.Throws<IndexOutOfRangeException>(() => single[1]);
            Assert.Throws<IndexOutOfRangeException>(() => single[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => quad[5]);
            Assert.Throws<IndexOutOfRangeException>(() => quad[-1]);
            Assert.Throws<ArgumentNullException>(() => OneOrMany.Create(default(ImmutableArray<int>)));
        }
    }
}
