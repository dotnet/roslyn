// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class StringEscapingTests
{
    [Fact]
    public void TestEscaping()
    {
        Assert.Equal("abc", "abc".Escape('$', '?'));
        Assert.Equal($"abc${(int)'?':X2}", "abc?".Escape('$', '?'));
        Assert.Equal($"abc${(int)'$':X2}", "abc$".Escape('$', '?'));
        Assert.Equal($"abc${(int)'?':X2}def${(int)'!':X2}", "abc?def!".Escape('$', '?', '!'));
        Assert.Equal($"${(int)'?':X2}${(int)'!':X2}ab", "?!ab".Escape('$', '?', '!'));
    }

    [Fact]
    public void TestUnescaping()
    {
        Assert.Equal("abc", "abc".Unescape('$'));
        Assert.Equal("abc?", $"abc${(int)'?':X2}".Unescape('$'));
        Assert.Equal("abc$", $"abc${(int)'$':X2}".Unescape('$'));
        Assert.Equal("abc?def!", $"abc${(int)'?':X2}def${(int)'!':X2}".Unescape('$'));
        Assert.Equal("?!ab", $"${(int)'?':X2}${(int)'!':X2}ab".Unescape('$'));
    }
}
