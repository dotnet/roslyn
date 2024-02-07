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

            Assert.True(actual.SequenceEqual(expected));
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
        public void OneOrNone()
        {
            Verify(OneOrMany.OneOrNone(1), 1);
            Verify(OneOrMany.OneOrNone("x"), "x");
            Verify(OneOrMany.OneOrNone<string>(null));
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

            Verify(OneOrMany.Create(1, 2), 1, 2);
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
        public void RemoveAll()
        {
            Verify(OneOrMany<int>.Empty.RemoveAll(1));
            Verify(OneOrMany.Create(1).RemoveAll(1));
            Verify(OneOrMany.Create(2).RemoveAll(1), 2);
            Verify(OneOrMany.Create(1, 2).RemoveAll(1), 2);
            Verify(OneOrMany.Create(1, 2).RemoveAll(2), 1);
            Verify(OneOrMany.Create(1, 1).RemoveAll(1));
            Verify(OneOrMany.Create(ImmutableArray.Create(1, 1, 1, 2)).RemoveAll(1), 2);
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
        public void Select_WithArg()
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
            Assert.Equal(3, OneOrMany.Create(ImmutableArray.Create(1, 3)).FirstOrDefault(i => i > 2));
        }

        [Fact]
        public void FirstOrDefault_WithArg()
        {
            Assert.Equal(1, OneOrMany.Create(1).FirstOrDefault((i, a) => i < a, 2));
            Assert.Equal(0, OneOrMany.Create(1).FirstOrDefault((i, a) => i > a, 2));
            Assert.Equal(0, OneOrMany.Create(ImmutableArray<int>.Empty).FirstOrDefault((i, a) => i > a, 2));
            Assert.Equal(1, OneOrMany.Create(ImmutableArray.Create(1)).FirstOrDefault((i, a) => i < a, 2));
            Assert.Equal(0, OneOrMany.Create(ImmutableArray.Create(1)).FirstOrDefault((i, a) => i > a, 2));
            Assert.Equal(1, OneOrMany.Create(ImmutableArray.Create(1, 3)).FirstOrDefault((i, a) => i < a, 2));
            Assert.Equal(3, OneOrMany.Create(ImmutableArray.Create(1, 3)).FirstOrDefault((i, a) => i > a, 2));
        }

        [Fact]
        public void All()
        {
            Assert.True(OneOrMany<int>.Empty.All(_ => false));
            Assert.True(OneOrMany<int>.Empty.All(_ => true));

            Assert.False(OneOrMany.Create(1).All(i => i > 1));
            Assert.True(OneOrMany.Create(1).All(i => i > 0));

            Assert.False(OneOrMany.Create(1, 2).All(i => i > 1));
            Assert.True(OneOrMany.Create(1, 2).All(i => i > 0));
        }

        [Fact]
        public void All_WithArg()
        {
            Assert.True(OneOrMany<int>.Empty.All((_, _) => false, 0));
            Assert.True(OneOrMany<int>.Empty.All((_, _) => true, 0));

            Assert.False(OneOrMany.Create(1).All((i, a) => i > a, 1));
            Assert.True(OneOrMany.Create(1).All((i, a) => i > a, 0));

            Assert.False(OneOrMany.Create(1, 2).All((i, a) => i > a, 1));
            Assert.True(OneOrMany.Create(1, 2).All((i, a) => i > a, 0));
        }

        [Fact]
        public void Any()
        {
            Assert.False(OneOrMany<int>.Empty.Any());
            Assert.True(OneOrMany.Create(1).Any());
            Assert.True(OneOrMany.Create(1, 2).Any());
        }

        [Fact]
        public void Any_Predicate()
        {
            Assert.False(OneOrMany<int>.Empty.Any(_ => false));
            Assert.False(OneOrMany<int>.Empty.Any(_ => true));

            Assert.False(OneOrMany.Create(1).Any(i => i > 1));
            Assert.True(OneOrMany.Create(1).Any(i => i > 0));

            Assert.False(OneOrMany.Create(1, 2).Any(i => i < 0));
            Assert.True(OneOrMany.Create(1, 2).Any(i => i > 1));
        }

        [Fact]
        public void Any_Predicate_WithArg()
        {
            Assert.False(OneOrMany<int>.Empty.Any((_, _) => false, 0));
            Assert.False(OneOrMany<int>.Empty.Any((_, _) => true, 0));

            Assert.False(OneOrMany.Create(1).Any((i, a) => i > a, 1));
            Assert.True(OneOrMany.Create(1).Any((i, a) => i > a, 0));

            Assert.False(OneOrMany.Create(1, 2).Any((i, a) => i < a, 0));
            Assert.True(OneOrMany.Create(1, 2).Any((i, a) => i > a, 1));
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

        [Fact]
        public void SequenceEqual()
        {
            Assert.True(OneOrMany<int>.Empty.SequenceEqual(OneOrMany<int>.Empty));
            Assert.False(OneOrMany<int>.Empty.SequenceEqual(OneOrMany.Create(1)));
            Assert.False(OneOrMany<int>.Empty.SequenceEqual(OneOrMany.Create(1, 2)));
            Assert.False(OneOrMany.Create(1).SequenceEqual(OneOrMany<int>.Empty));
            Assert.False(OneOrMany.Create(1, 2).SequenceEqual(OneOrMany<int>.Empty));
            Assert.True(OneOrMany.Create(1).SequenceEqual(OneOrMany.Create(1)));
            Assert.False(OneOrMany.Create(1).SequenceEqual(OneOrMany.Create(2)));
            Assert.True(OneOrMany.Create(1, 2).SequenceEqual(OneOrMany.Create(1, 2)));
            Assert.False(OneOrMany.Create(1, 2).SequenceEqual(OneOrMany.Create(1, 0)));

            Assert.False(OneOrMany.Create(1, 2).SequenceEqual(OneOrMany.Create(ImmutableArray.Create(1, 2, 3))));
            Assert.True(OneOrMany.Create(1).SequenceEqual(OneOrMany.Create(ImmutableArray.Create(1))));

            Assert.True(OneOrMany<int>.Empty.SequenceEqual(new int[0]));
            Assert.False(OneOrMany<int>.Empty.SequenceEqual(new[] { 1 }));
            Assert.False(OneOrMany<int>.Empty.SequenceEqual(new[] { 1, 2 }));
            Assert.True(OneOrMany.Create(1).SequenceEqual(new[] { 1 }));
            Assert.False(OneOrMany.Create(1).SequenceEqual(new[] { 2 }));
            Assert.True(OneOrMany.Create(1, 2).SequenceEqual(new[] { 1, 2 }));
            Assert.False(OneOrMany.Create(1, 2).SequenceEqual(new[] { 1, 0 }));
            Assert.False(OneOrMany.Create(1, 2).SequenceEqual(new[] { 1, 2, 3 }));

            Assert.True(new int[0].SequenceEqual(OneOrMany<int>.Empty));
            Assert.False(new[] { 1 }.SequenceEqual(OneOrMany<int>.Empty));
            Assert.False(new[] { 1, 2 }.SequenceEqual(OneOrMany<int>.Empty));
            Assert.True(new[] { 1 }.SequenceEqual(OneOrMany.Create(1)));
            Assert.False(new[] { 1 }.SequenceEqual(OneOrMany.Create(2)));
            Assert.True(new[] { 1, 2 }.SequenceEqual(OneOrMany.Create(1, 2)));
            Assert.False(new[] { 1, 2 }.SequenceEqual(OneOrMany.Create(1, 0)));
            Assert.False(new[] { 1, 2 }.SequenceEqual(OneOrMany.Create(ImmutableArray.Create(1, 2, 3))));

            Assert.True(ImmutableArray<int>.Empty.SequenceEqual(OneOrMany<int>.Empty));
            Assert.False(ImmutableArray.Create(1).SequenceEqual(OneOrMany<int>.Empty));
            Assert.False(ImmutableArray.Create(1, 2).SequenceEqual(OneOrMany<int>.Empty));
            Assert.True(ImmutableArray.Create(1).SequenceEqual(OneOrMany.Create(1)));
            Assert.False(ImmutableArray.Create(1).SequenceEqual(OneOrMany.Create(2)));
            Assert.True(ImmutableArray.Create(1, 2).SequenceEqual(OneOrMany.Create(1, 2)));
            Assert.False(ImmutableArray.Create(1, 2).SequenceEqual(OneOrMany.Create(1, 0)));
            Assert.False(ImmutableArray.Create(1, 2).SequenceEqual(OneOrMany.Create(ImmutableArray.Create(1, 2, 3))));
        }

        [Fact]
        public void SequenceEqual_WithComparer()
        {
            var comparer = new TestEqualityComparer<int>((x, y) => x % 10 == y % 10);
            Assert.True(OneOrMany.Create(1).SequenceEqual(new[] { 11 }, comparer));
            Assert.True(OneOrMany.Create(1, 2).SequenceEqual(new[] { 11, 32 }, comparer));
            Assert.False(OneOrMany.Create(1, 2).SequenceEqual(new[] { 0, 1 }, comparer));
        }
    }
}
