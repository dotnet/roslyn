// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/ImmutableDictionaryBuilderTestBase.nonnetstandard.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public abstract partial class ImmutableDictionaryBuilderTestBase : ImmutablesTestBase
    {
        [Fact]
        public void TryGetKey()
        {
            var builder = Empty<int>(StringComparer.OrdinalIgnoreCase)
                .Add("a", 1).ToBuilder();
            Assert.True(TryGetKeyHelper(builder, "a", out string actualKey));
            Assert.Equal("a", actualKey);

            Assert.True(TryGetKeyHelper(builder, "A", out actualKey));
            Assert.Equal("a", actualKey);

            Assert.False(TryGetKeyHelper(builder, "b", out actualKey));
            Assert.Equal("b", actualKey);
        }
    }
}
