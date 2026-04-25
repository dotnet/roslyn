// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorLanguageVersionTest
{
    [Fact]
    public void TryParseInvalid()
    {
        // Arrange
        var value = "not-version";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryParse10()
    {
        // Arrange
        var value = "1.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_1_0, version);
    }

    [Fact]
    public void TryParse11()
    {
        // Arrange
        var value = "1.1";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_1_1, version);
    }

    [Fact]
    public void TryParse20()
    {
        // Arrange
        var value = "2.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_2_0, version);
    }

    [Fact]
    public void TryParse21()
    {
        // Arrange
        var value = "2.1";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_2_1, version);
    }

    [Fact]
    public void TryParse30()
    {
        // Arrange
        var value = "3.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_3_0, version);
    }

    [Fact]
    public void TryParse50()
    {
        // Arrange
        var value = "5.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_5_0, version);
    }

    [Fact]
    public void TryParse60()
    {
        // Arrange
        var value = "6.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_6_0, version);
    }

    [Fact]
    public void TryParse70()
    {
        // Arrange
        var value = "7.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_7_0, version);
    }

    [Fact]
    public void TryParse80()
    {
        // Arrange
        var value = "8.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_8_0, version);
    }

    [Fact]
    public void TryParse90()
    {
        // Arrange
        var value = "9.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_9_0, version);
    }

    [Fact]
    public void TryParse100()
    {
        // Arrange
        var value = "10.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_10_0, version);
    }

    [Fact]
    public void TryParse110()
    {
        // Arrange
        var value = "11.0";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Version_11_0, version);
    }

    [Fact]
    public void TryParseLatest()
    {
        // Arrange
        var value = "Latest";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Latest, version);
        Assert.Same(RazorLanguageVersion.Version_9_0, version);
    }

    [Fact]
    public void TryParseExperimental()
    {
        // Arrange
        var value = "experimental";

        // Act
        var result = RazorLanguageVersion.TryParse(value, out var version);

        // Assert
        Assert.True(result);
        Assert.Same(RazorLanguageVersion.Experimental, version);
    }

    [Fact]
    public void PreviewPointsToNewestVersion()
    {
        // Arrange
        var v = RazorLanguageVersion.Parse("preview");
        var versions = typeof(RazorLanguageVersion).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.Name.StartsWith("Version_", StringComparison.Ordinal))
            .Select(f => f.GetValue(obj: null))
            .Cast<RazorLanguageVersion>();

        // Act & Assert
        Assert.NotEmpty(versions);
        foreach (var version in versions)
        {
            Assert.True(version.CompareTo(v) <= 0, $"RazorLanguageVersion {version} has a higher version than RazorLanguageVersion.Preview");
        }
    }
}
