// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public abstract partial class ImmutableDictionaryBuilderTestBase : ImmutablesTestBase
    {
        [Fact]
        public void TryGetKey()
        {
            var builder = Empty<int>(StringComparer.OrdinalIgnoreCase)
                .Add("a", 1).ToBuilder();
            string actualKey;
            Assert.True(TryGetKeyHelper(builder, "a", out actualKey));
            Assert.Equal("a", actualKey);

            Assert.True(TryGetKeyHelper(builder, "A", out actualKey));
            Assert.Equal("a", actualKey);

            Assert.False(TryGetKeyHelper(builder, "b", out actualKey));
            Assert.Equal("b", actualKey);
        }
    }
}
