﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Differencing.UnitTests
{
    public class MatchTests
    {
        [Fact]
        public void KnownMatches()
        {
            TestNode x1, x2;

            var oldRoot = new TestNode(0, 1,
                x1 = new TestNode(1, 1));

            var newRoot = new TestNode(0, 1,
                x2 = new TestNode(1, 2));

            var m = TestTreeComparer.Instance.ComputeMatch(oldRoot, newRoot,
                new[] { KeyValuePairUtil.Create(x1, x2), KeyValuePairUtil.Create(x1, x2) });
            Assert.True(m.TryGetNewNode(x1, out var n));
            Assert.Equal(n, x2);

            Assert.Throws<ArgumentException>(() => TestTreeComparer.Instance.ComputeMatch(oldRoot, newRoot, new[] { KeyValuePairUtil.Create(x1, x1) }));

            Assert.Throws<ArgumentException>(() => TestTreeComparer.Instance.ComputeMatch(oldRoot, newRoot, new[] { KeyValuePairUtil.Create(x1, x2), KeyValuePairUtil.Create(x1, new TestNode(0, 0)) }));
        }

        [Fact]
        public void KnownMatchesDups()
        {
            TestNode x1, x2, y1, y2;

            var oldRoot = new TestNode(0, 1,
                x1 = new TestNode(1, 1),
                y1 = new TestNode(1, 4));

            var newRoot = new TestNode(0, 1,
                x2 = new TestNode(1, 2),
                y2 = new TestNode(1, 3));

            var m = TestTreeComparer.Instance.ComputeMatch(oldRoot, newRoot, new[]
            {
                KeyValuePairUtil.Create(x1, x2),
                KeyValuePairUtil.Create(y1, x2),
            });

            // the first one wins:
            Assert.True(m.TryGetNewNode(x1, out var n));
            Assert.Equal(x2, n);
            Assert.True(m.TryGetOldNode(x2, out n));
            Assert.Equal(x1, n);
            Assert.True(m.TryGetNewNode(y1, out n)); // matched
            Assert.Equal(y2, n);
        }

        [Fact]
        public void KnownMatchesRootMatch()
        {
            TestNode x1, x2;

            var oldRoot = new TestNode(0, 1,
                x1 = new TestNode(0, 1));

            var newRoot = new TestNode(0, 1,
                x2 = new TestNode(0, 2));

            var m = TestTreeComparer.Instance.ComputeMatch(oldRoot, newRoot, new[]
            {
                KeyValuePairUtil.Create(x1, newRoot),
            });

            // the root wins:
            Assert.True(m.TryGetNewNode(x1, out var n)); // matched
            Assert.Equal(x2, n);
            Assert.True(m.TryGetOldNode(newRoot, out n));
            Assert.Equal(oldRoot, n);
            Assert.True(m.TryGetNewNode(oldRoot, out n));
            Assert.Equal(newRoot, n);
        }
    }
}
