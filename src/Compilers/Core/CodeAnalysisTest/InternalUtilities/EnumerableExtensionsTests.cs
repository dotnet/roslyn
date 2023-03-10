// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Xunit;

namespace Roslyn.Utilities.UnitTests.InternalUtilities
{
    using System.Linq;

    public class EnumerableExtensionsTests
    {
        [Fact]
        public void SequenceEqual()
        {
            bool comparer(int x, int y) => x == y;
            Assert.True(EnumerableExtensions.SequenceEqual((IEnumerable<int>)null, null, comparer));
            Assert.False(EnumerableExtensions.SequenceEqual(new[] { 1 }, null, comparer));
            Assert.False(EnumerableExtensions.SequenceEqual(null, new[] { 1 }, comparer));

            Assert.True(EnumerableExtensions.SequenceEqual(new[] { 1 }, new[] { 1 }, comparer));
            Assert.False(EnumerableExtensions.SequenceEqual(new int[0], new[] { 1 }, comparer));
            Assert.False(EnumerableExtensions.SequenceEqual(new[] { 1 }, new int[0], comparer));
            Assert.False(EnumerableExtensions.SequenceEqual(new[] { 1, 2, 3 }, new[] { 1, 3, 2 }, comparer));
            Assert.True(EnumerableExtensions.SequenceEqual(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, comparer));
        }
    }
}

