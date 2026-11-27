// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test;

public class RangeExtensionsTests
{
    [Fact]
    public void CompareTo_StartAndEndAreSame_ReturnsZero()
    {
        // Arrange
        var range1 = LspFactory.CreateRange(1, 2, 3, 4);
        var range2 = LspFactory.CreateRange(1, 2, 3, 4);

        // Act
        var result = range1.CompareTo(range2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CompareTo_StartOfThisRangeIsBeforeOther_ReturnsNegative()
    {
        // Arrange
        var range1 = LspFactory.CreateRange(1, 2, 3, 4);
        var range2 = LspFactory.CreateRange(2, 2, 3, 4);

        // Act
        var result = range1.CompareTo(range2);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void CompareTo_EndOfThisRangeIsBeforeOther_ReturnsNegative()
    {
        // Arrange
        var range1 = LspFactory.CreateRange(1, 2, 3, 4);
        var range2 = LspFactory.CreateRange(1, 2, 4, 4);

        // Act
        var result = range1.CompareTo(range2);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void CompareTo_StartOfThisRangeIsAfterOther_ReturnsPositive()
    {
        // Arrange
        var range1 = LspFactory.CreateRange(2, 2, 3, 4);
        var range2 = LspFactory.CreateRange(1, 2, 3, 4);

        // Act
        var result = range1.CompareTo(range2);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void CompareTo_EndOfThisRangeIsAfterOther_ReturnsPositive()
    {
        // Arrange
        var range1 = LspFactory.CreateRange(1, 2, 4, 4);
        var range2 = LspFactory.CreateRange(1, 2, 3, 4);

        // Act
        var result = range1.CompareTo(range2);

        // Assert
        Assert.True(result > 0);
    }
}
