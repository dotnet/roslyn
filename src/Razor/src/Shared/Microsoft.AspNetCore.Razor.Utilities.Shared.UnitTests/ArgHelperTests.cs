// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class ArgHelperTests
{
    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("test")]
    public void ThrowIfNull(string? s, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfNull(s), exceptionType);
    }

    [Fact]
    public unsafe void ThrowIfNull_NullPtr_Throws()
    {
        byte* ptr = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            ArgHelper.ThrowIfNull(ptr);
        });
    }

    [Fact]
    public unsafe void ThrowIfNull_Ptr_DoesNotThrow()
    {
        fixed (byte* ptr = new byte[8])
        {
            ArgHelper.ThrowIfNull(ptr);
        }
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("    ")]
    [InlineData("test")]
    public void ThrowIfNullOrEmpty(string? s, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfNullOrEmpty(s), exceptionType);
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("    ", typeof(ArgumentException))]
    [InlineData("test")]
    public void ThrowIfNullOrWhiteSpace(string? s, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfNullOrWhiteSpace(s), exceptionType);
    }

    [Theory]
    [InlineData(null, null, typeof(ArgumentOutOfRangeException))]
    [InlineData(null, "test")]
    [InlineData("test", null)]
    [InlineData("test", "test", typeof(ArgumentOutOfRangeException))]
    [InlineData("test", "TeSt")]
    public void ThrowIfEqual(string? s1, string? s2, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfEqual(s1, s2), exceptionType);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "test", typeof(ArgumentOutOfRangeException))]
    [InlineData("test", null, typeof(ArgumentOutOfRangeException))]
    [InlineData("test", "test")]
    [InlineData("test", "TeSt", typeof(ArgumentOutOfRangeException))]
    public void ThrowIfNotEqual(string? s1, string? s2, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfNotEqual(s1, s2), exceptionType);
    }

    [Theory]
    [InlineData(0, typeof(ArgumentOutOfRangeException))]
    [InlineData(-1)]
    [InlineData(1)]
    public void ThrowIfZero(int v, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfZero(v), exceptionType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1, typeof(ArgumentOutOfRangeException))]
    [InlineData(1)]
    public void ThrowIfNegative(int v, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfNegative(v), exceptionType);
    }

    [Theory]
    [InlineData(0, typeof(ArgumentOutOfRangeException))]
    [InlineData(-1, typeof(ArgumentOutOfRangeException))]
    [InlineData(1)]
    public void ThrowIfNegativeOrZero(int v, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfNegativeOrZero(v), exceptionType);
    }

    [Theory]
    [InlineData(42, 42)]
    [InlineData(41, 42)]
    [InlineData(43, 42, typeof(ArgumentOutOfRangeException))]
    public void ThrowIfGreaterThan(int v1, int v2, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfGreaterThan(v1, v2), exceptionType);
    }

    [Theory]
    [InlineData(42, 42, typeof(ArgumentOutOfRangeException))]
    [InlineData(41, 42)]
    [InlineData(43, 42, typeof(ArgumentOutOfRangeException))]
    public void ThrowIfGreaterThanOrEqual(int v1, int v2, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfGreaterThanOrEqual(v1, v2), exceptionType);
    }

    [Theory]
    [InlineData(42, 42)]
    [InlineData(41, 42, typeof(ArgumentOutOfRangeException))]
    [InlineData(43, 42)]
    public void ThrowIfLessThan(int v1, int v2, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfLessThan(v1, v2), exceptionType);
    }

    [Theory]
    [InlineData(42, 42, typeof(ArgumentOutOfRangeException))]
    [InlineData(41, 42, typeof(ArgumentOutOfRangeException))]
    [InlineData(43, 42)]
    public void ThrowIfLessThanOrEqual(int v1, int v2, Type? exceptionType = null)
    {
        Verify(() => ArgHelper.ThrowIfLessThanOrEqual(v1, v2), exceptionType);
    }

    private static void Verify(Action a, Type? exceptionType = null)
    {
        if (exceptionType is not null)
        {
            Assert.Throws(exceptionType, a);
        }
        else
        {
            a();
        }
    }
}
