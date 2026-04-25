// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class CSharpIdentifierTest
{
    [Theory]
    [InlineData("TestFile.cshtml", "TestFile")]
    [InlineData("test-file.cshtml", "test_file")]
    [InlineData("Test File.cshtml", "Test_File")]
    [InlineData("123Test.cshtml", "_123Test")]
    [InlineData("Test123.cshtml", "Test123")]
    [InlineData("test.component.cshtml", "test_component")]
    [InlineData("", "")]
    [InlineData("NoExtension", "NoExtension")]
    [InlineData("Multiple.Dots.cshtml", "Multiple_Dots")]
    public void GetClassNameFromPath_ReturnsExpectedClassName(string path, string expected)
    {
        // Act
        var result = CSharpIdentifier.GetClassNameFromPath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ValidIdentifier", "ValidIdentifier")]
    [InlineData("valid123", "valid123")]
    [InlineData("123invalid", "_123invalid")]
    [InlineData("", "")]
    [InlineData("test-name", "test_name")]
    [InlineData("test name", "test_name")]
    [InlineData("test.name", "test_name")]
    [InlineData("test@name", "test_name")]
    [InlineData("test#name", "test_name")]
    [InlineData("test$name", "test_name")]
    [InlineData("test%name", "test_name")]
    [InlineData("_validStart", "_validStart")]
    [InlineData("@invalidStart", "_invalidStart")]
    public void SanitizeIdentifier_BasicCases_ReturnsExpectedResult(string input, string expected)
    {
        // Act
        var result = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeIdentifier_WithHighSurrogate_SkipsLowSurrogate()
    {
        // Arrange - Using surrogate pairs for characters outside the Basic Multilingual Plane
        // 𝕏 (Mathematical Double-Struck Capital X) = U+1D54F = High: 0xD835, Low: 0xDD4F
        var input = "test𝕏name";

        // Act
        var result = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        // The surrogate pair should be replaced with a single underscore, not two
        Assert.Equal("test_name", result);
    }

    [Fact]
    public void SanitizeIdentifier_WithMultipleSurrogatePairs_HandlesCorrectly()
    {
        // Arrange - Multiple surrogate pairs
        // 𝕏 (U+1D54F) and 𝔸 (U+1D538)
        var input = "𝕏test𝔸name";

        // Act
        var result = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        // Each surrogate pair should be replaced with a single underscore
        Assert.Equal("_test_name", result);
    }

    [Fact]
    public void SanitizeIdentifier_WithHighSurrogateAtEnd_HandlesCorrectly()
    {
        // Arrange - High surrogate at the end without low surrogate (invalid Unicode)
        var input = "test\uD835"; // High surrogate without low surrogate

        // Act
        var result = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        // Should handle gracefully and replace with underscore
        Assert.Equal("test_", result);
    }

    [Fact]
    public void SanitizeIdentifier_WithLowSurrogateOnly_HandlesCorrectly()
    {
        // Arrange - Low surrogate without high surrogate (invalid Unicode)
        var input = "test\uDD4F"; // Low surrogate without high surrogate

        // Act
        var result = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        // Should handle gracefully and replace with underscore
        Assert.Equal("test_", result);
    }

    [Fact]
    public void SanitizeIdentifier_WithValidUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange - Valid Unicode characters that are identifier parts
        var input = "testÀname"; // Latin Capital Letter A with Grave

        // Act
        var result = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        // Valid Unicode identifier characters should be preserved
        Assert.Equal("testÀname", result);
    }

    [Fact]
    public void SanitizeIdentifier_MixedSurrogatesAndValidChars_HandlesCorrectly()
    {
        // Arrange - Mix of surrogate pairs, valid chars, and invalid chars
        var input = "test𝕏-validÀ@𝔸end";

        // Act
        var result = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        // Surrogate pairs and invalid chars become underscores, valid chars preserved
        Assert.Equal("test__validÀ__end", result);
    }

    [Theory]
    [InlineData("𝕏", "_")] // Single surrogate pair
    [InlineData("𝕏𝔸", "__")] // Two surrogate pairs
    [InlineData("a𝕏b", "a_b")] // Surrogate pair between valid chars
    public void SanitizeIdentifier_SurrogatePairEdgeCases_HandlesCorrectly(string input, string expected)
    {
        // Act
        var result = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AppendSanitized_WithSurrogatePairs_AppendsCorrectly()
    {
        // Arrange
        var builder = new System.Text.StringBuilder();
        var input = "test𝕏name";

        // Act
        CSharpIdentifier.AppendSanitized(builder, input);

        // Assert
        Assert.Equal("test_name", builder.ToString());
    }

    [Fact]
    public void AppendSanitized_WithInvalidStartCharacter_PrependsUnderscore()
    {
        // Arrange
        var builder = new System.Text.StringBuilder();
        var input = "123valid";

        // Act
        CSharpIdentifier.AppendSanitized(builder, input);

        // Assert
        Assert.Equal("_123valid", builder.ToString());
    }
}
