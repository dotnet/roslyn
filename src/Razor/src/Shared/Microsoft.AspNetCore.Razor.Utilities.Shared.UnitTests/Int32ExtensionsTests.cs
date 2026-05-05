// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class Int32ExtensionsTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(99, 2)]
    [InlineData(100, 3)]
    [InlineData(999, 3)]
    [InlineData(1000, 4)]
    [InlineData(9999, 4)]
    [InlineData(10000, 5)]
    [InlineData(99999, 5)]
    [InlineData(100000, 6)]
    [InlineData(999999, 6)]
    [InlineData(1000000, 7)]
    [InlineData(9999999, 7)]
    [InlineData(10000000, 8)]
    [InlineData(99999999, 8)]
    [InlineData(100000000, 9)]
    [InlineData(999999999, 9)]
    [InlineData(1000000000, 10)]
    [InlineData(int.MaxValue, 10)]
    public void CountDigits_PositiveNumbers_ReturnsCorrectCount(int number, int expectedDigits)
    {
        // Act
        var result = number.CountDigits();

        // Assert
        Assert.Equal(expectedDigits, result);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(-9, 1)]
    [InlineData(-10, 2)]
    [InlineData(-99, 2)]
    [InlineData(-100, 3)]
    [InlineData(-999, 3)]
    [InlineData(-1000, 4)]
    [InlineData(-9999, 4)]
    [InlineData(-10000, 5)]
    [InlineData(-99999, 5)]
    [InlineData(-100000, 6)]
    [InlineData(-999999, 6)]
    [InlineData(-1000000, 7)]
    [InlineData(-9999999, 7)]
    [InlineData(-10000000, 8)]
    [InlineData(-99999999, 8)]
    [InlineData(-100000000, 9)]
    [InlineData(-999999999, 9)]
    [InlineData(-1000000000, 10)]
    [InlineData(int.MinValue, 10)]
    public void CountDigits_NegativeNumbers_ReturnsCorrectCount(int number, int expectedDigits)
    {
        // Act
        var result = number.CountDigits();

        // Assert
        Assert.Equal(expectedDigits, result);
    }

    [Fact]
    public void CountDigits_BoundaryValues_ReturnsCorrectCount()
    {
        // Test specific boundary values for each digit count
        Assert.Equal(1, 0.CountDigits());
        Assert.Equal(2, 10.CountDigits());
        Assert.Equal(3, 100.CountDigits());
        Assert.Equal(4, 1000.CountDigits());
        Assert.Equal(5, 10000.CountDigits());
        Assert.Equal(6, 100000.CountDigits());
        Assert.Equal(7, 1000000.CountDigits());
        Assert.Equal(8, 10000000.CountDigits());
        Assert.Equal(9, 100000000.CountDigits());
        Assert.Equal(10, 1000000000.CountDigits());
    }

    [Fact]
    public void CountDigits_ExtremeValues_ReturnsCorrectCount()
    {
        // Test extreme values
        Assert.Equal(10, int.MaxValue.CountDigits());
        Assert.Equal(10, int.MinValue.CountDigits());
    }
}
