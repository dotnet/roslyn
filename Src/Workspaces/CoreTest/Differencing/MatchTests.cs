// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                new[] { KeyValuePair.Create(x1, x2), KeyValuePair.Create(x1, x2) });

            TestNode n;
            Assert.True(m.TryGetNewNode(x1, out n));
            Assert.Equal(n, x2);

            Assert.Throws<ArgumentException>(() => TestTreeComparer.Instance.ComputeMatch(oldRoot, newRoot, new[] { KeyValuePair.Create(x1, x1) }));

            Assert.Throws<ArgumentException>(() => TestTreeComparer.Instance.ComputeMatch(oldRoot, newRoot, new[] { KeyValuePair.Create(x1, x2), KeyValuePair.Create(x1, new TestNode(0, 0)) }));
        }
    }
}
