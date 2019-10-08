// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Analyzer.Utilities.Extensions
{
    public class ImmutableArrayExensionsTests
    {
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [Theory]
        public void HasExactly2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, global::System.Collections.Immutable.ImmutableArrayExtensions.HasExactly(CreateImmutableArray(count), 2));
        }

        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        [Theory]
        public void HasMoreThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, global::System.Collections.Immutable.ImmutableArrayExtensions.HasMoreThan(CreateImmutableArray(count), 2));
        }

        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [Theory]
        public void HasFewerThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, global::System.Collections.Immutable.ImmutableArrayExtensions.HasFewerThan(CreateImmutableArray(count), 2));
        }

        private static ImmutableArray<int> CreateImmutableArray(int count)
        {
            if (count > 0)
            {
                var builder = ImmutableArray.CreateBuilder<int>(count);
                builder.AddRange(Enumerable.Range(0, count));
                return builder.ToImmutable();
            }
            return ImmutableArray<int>.Empty;
        }
    }
}
