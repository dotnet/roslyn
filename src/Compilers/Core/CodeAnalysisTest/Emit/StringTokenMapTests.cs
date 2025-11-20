// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Emit;

public sealed class StringTokenMapTests
{
    [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/2641964")]
    public void UniqueStrings()
    {
        var map = new StringTokenMap(initialHeapSize: 0);
        var a1 = new string('a', 5);
        var a2 = new string('a', 5);
        Assert.Equal(a1, a2);
        Assert.NotSame(a1, a2);
        Assert.True(map.TryGetOrAddToken(a1, out var token1));
        Assert.True(map.TryGetOrAddToken(a2, out var token2));
        var b = "b";
        Assert.True(map.TryGetOrAddToken(b, out var token3));
        Assert.True(map.TryGetOrAddToken(b, out var token4));
        Assert.Equal((0u, 0u, 1u, 1u), (token1, token2, token3, token4));
        Assert.Equal(["aaaaa", "b"], map.CopyValues());
        Assert.Equal("aaaaa", map.GetValue(0));
        Assert.Equal("b", map.GetValue(1));
        Assert.Throws<IndexOutOfRangeException>(() => map.GetValue(2));
    }
}
