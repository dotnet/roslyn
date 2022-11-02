// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Debugging;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class CustomDebugInfoReaderTests
{
    [Theory]
    [InlineData(new byte[0], "")]
    [InlineData(new byte[] { 0x00, 0x00 }, "")]
    [InlineData(new byte[] { (byte)'a', 0x00 }, "a")]
    [InlineData(new byte[] { (byte)'a', 0x00, 0x00, 0x00 }, "a")]
    public void DecodeForwardIteratorRecord(byte[] bytes, string expected)
    {
        Assert.Equal(expected, CustomDebugInfoReader.DecodeForwardIteratorRecord(bytes.ToImmutableArray()));
    }

    [Theory]
    [InlineData(new byte[] { 0x00 })]
    [InlineData(new byte[] { (byte)'a', })]
    [InlineData(new byte[] { (byte)'a', 0x00, 0x00 })]
    public void DecodeForwardIteratorRecord_Invalid(byte[] bytes)
    {
        Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.DecodeForwardIteratorRecord(bytes.ToImmutableArray()));
    }
}
