// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.InternalUtilities
{
    public class OneOrManyTests : TestBase
    {
        [Fact]
        public void Zero()
        {
            Verify(OneOrMany.Create(ImmutableArray<int>.Empty));
            Verify(new OneOrMany<int>(ImmutableArray<int>.Empty));
        }

        [Fact]
        public void One()
        {
            Verify(OneOrMany.Create(1), 1);
            Verify(OneOrMany.Create(ImmutableArray.Create(2)), 2);
            Verify(OneOrMany.Create(ImmutableArray<int>.Empty).Add(3), 3);
            Verify(new OneOrMany<int>(1), 1);
            Verify(new OneOrMany<int>(ImmutableArray.Create(2)), 2);
            Verify(new OneOrMany<int>(ImmutableArray<int>.Empty).Add(3), 3);
        }

        [Fact]
        public void Many()
        {
            Verify(OneOrMany.Create(ImmutableArray.Create(1, 2, 3)).Add(4), 1, 2, 3, 4);
            Verify(OneOrMany.Create(ImmutableArray.Create(1, 2, 3, 4)), 1, 2, 3, 4);
            Verify(OneOrMany.Create(ImmutableArray<int>.Empty).Add(1).Add(2).Add(3).Add(4), 1, 2, 3, 4);
            Verify(new OneOrMany<int>(ImmutableArray.Create(1, 2, 3)).Add(4), 1, 2, 3, 4);
            Verify(new OneOrMany<int>(ImmutableArray.Create(1, 2, 3, 4)), 1, 2, 3, 4);
            Verify(new OneOrMany<int>(ImmutableArray<int>.Empty).Add(1).Add(2).Add(3).Add(4), 1, 2, 3, 4);
        }

        private static void Verify<T>(OneOrMany<T> actual, params T[] expected)
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
