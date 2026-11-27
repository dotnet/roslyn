// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class ParserHelpersTest
{
    [Theory]
    [InlineData("&amp;", "&")]
    [InlineData("&lt;", "<")]
    [InlineData("&gt;", ">")]
    [InlineData("&quot;", "\"")]
    [InlineData("&apos;", "'")]
    [InlineData("&nbsp;", "\u00A0")]
    [InlineData("&copy;", "\u00A9")]
    [InlineData("&reg;", "\u00AE")]
    [InlineData("&trade;", "\u2122")]
    [InlineData("&hellip;", "\u2026")]
    [InlineData("&mdash;", "\u2014")]
    [InlineData("&ndash;", "\u2013")]
    [InlineData("&laquo;", "\u00AB")]
    [InlineData("&raquo;", "\u00BB")]
    [InlineData("&Aacute;", "\u00C1")]
    [InlineData("&aacute;", "\u00E1")]
    [InlineData("&Alpha;", "\u0391")]
    [InlineData("&alpha;", "\u03B1")]
    public void TryGetHtmlEntity_ValidNamedEntities_ReturnsTrue(string input, string expectedReplacement)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.True(result);
        Assert.Equal(input, entity.ToString());
        Assert.Equal(expectedReplacement, replacement);
    }

    [Theory]
    [InlineData("&#65;", "A")]     // Basic decimal
    [InlineData("&#97;", "a")]     // Basic decimal
    [InlineData("&#32;", " ")]     // Space character
    [InlineData("&#169;", "\u00A9")] // Copyright symbol
    [InlineData("&#8364;", "\u20AC")] // Euro symbol
    [InlineData("&#65535;", "\uFFFF")] // Max BMP character
    [InlineData("&#8482;", "\u2122")] // Trademark symbol
    public void TryGetHtmlEntity_ValidDecimalEntities_ReturnsTrue(string input, string expectedReplacement)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.True(result);
        Assert.Equal(input, entity.ToString());
        Assert.Equal(expectedReplacement, replacement);
    }

    [Theory]
    [InlineData("&#x41;", "A")]      // Basic hex lowercase
    [InlineData("&#X41;", "A")]      // Basic hex uppercase
    [InlineData("&#x61;", "a")]      // Basic hex lowercase
    [InlineData("&#X61;", "a")]      // Basic hex uppercase
    [InlineData("&#x20;", " ")]      // Space character
    [InlineData("&#xa9;", "\u00A9")] // Copyright symbol
    [InlineData("&#XA9;", "\u00A9")] // Copyright symbol uppercase
    [InlineData("&#x20ac;", "\u20AC")] // Euro symbol
    [InlineData("&#XFFFF;", "\uFFFF")] // Max BMP character
    public void TryGetHtmlEntity_ValidHexEntities_ReturnsTrue(string input, string expectedReplacement)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.True(result);
        Assert.Equal(input, entity.ToString());
        Assert.Equal(expectedReplacement, replacement);
    }

    [Theory]
    [InlineData("&#0x41;", "A")]      // 0x prefix lowercase
    [InlineData("&#0X41;", "A")]      // 0x prefix uppercase
    [InlineData("&#0x61;", "a")]      // 0x prefix lowercase
    [InlineData("&#0X61;", "a")]      // 0x prefix uppercase
    [InlineData("&#0x20;", " ")]      // Space character
    [InlineData("&#0xa9;", "\u00A9")] // Copyright symbol
    [InlineData("&#0XA9;", "\u00A9")] // Copyright symbol uppercase
    public void TryGetHtmlEntity_InvalidHexEntitiesWithZeroPrefix_ReturnsTrue(string input, string expectedReplacement)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.True(result);
        Assert.Equal(input, entity.ToString());
        Assert.Equal(expectedReplacement, replacement);
    }

    [Theory]
    [InlineData("&")]              // Just ampersand
    [InlineData("&amp")]           // No semicolon
    [InlineData("&invalid;")]      // Invalid named entity
    [InlineData("&notvalid;")]     // Invalid named entity
    [InlineData("&xyz123;")]       // Invalid named entity
    [InlineData("&#;")]            // Empty numeric entity
    [InlineData("&#x;")]           // Empty hex entity
    [InlineData("&#0x;")]          // Empty 0x entity
    [InlineData("&#abc;")]         // Invalid decimal digits
    [InlineData("&#xghi;")]        // Invalid hex digits
    [InlineData("&#0xghi;")]       // Invalid 0x hex digits
    [InlineData("&amp extra")]     // No semicolon with extra content
    [InlineData("&am p;")]         // Space in entity name
    public void TryGetHtmlEntity_InvalidEntities_ReturnsFalse(string input)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.False(result);
        Assert.True(entity.IsEmpty);
        Assert.Null(replacement);
    }

    [Theory]
    [InlineData("&#0;")]           // Null character (below valid range)
    [InlineData("&#31;")]          // Control character (below valid range)
    [InlineData("&#55296;")]       // Surrogate range start (0xD800)
    [InlineData("&#57343;")]       // Surrogate range end (0xDFFF)
    [InlineData("&#65536;")]       // Above BMP (0x10000)
    [InlineData("&#1114111;")]     // Max Unicode (0x10FFFF)
    [InlineData("&#1114112;")]     // Above max Unicode
    [InlineData("&#x0;")]          // Hex null character
    [InlineData("&#xD800;")]       // Hex surrogate start
    [InlineData("&#xDFFF;")]       // Hex surrogate end
    [InlineData("&#x10000;")]      // Hex above BMP
    public void TryGetHtmlEntity_OutOfRangeNumericEntities_ReturnsFalse(string input)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.False(result);
        Assert.True(entity.IsEmpty);
        Assert.Null(replacement);
    }

    [Fact]
    public void TryGetHtmlEntity_EmptyInput_ReturnsFalse()
    {
        // Arrange
        var content = ReadOnlyMemory<char>.Empty;

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.False(result);
        Assert.True(entity.IsEmpty);
        Assert.Null(replacement);
    }

    [Theory]
    [InlineData("a")]              // Doesn't start with &
    [InlineData("hello")]          // Regular text
    [InlineData("123")]            // Numbers
    [InlineData("")]               // Empty string
    public void TryGetHtmlEntity_NotStartingWithAmpersand_ReturnsFalse(string input)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.False(result);
        Assert.True(entity.IsEmpty);
        Assert.Null(replacement);
    }

    [Theory]
    [InlineData("&amp;extra", "&amp;", "&")]
    [InlineData("&lt;more", "&lt;", "<")]
    [InlineData("&#65;text", "&#65;", "A")]
    [InlineData("&#x41;more", "&#x41;", "A")]
    [InlineData("&copy;right", "&copy;", "\u00A9")]
    public void TryGetHtmlEntity_EntityWithExtraContent_ParsesOnlyEntity(string input, string expectedEntity, string expectedReplacement)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedEntity, entity.ToString());
        Assert.Equal(expectedReplacement, replacement);
    }

    [Theory]
    [InlineData("&amp")]           // Valid entity name without semicolon
    [InlineData("&lt")]            // Valid entity name without semicolon
    [InlineData("&gt")]            // Valid entity name without semicolon
    [InlineData("&copy")]          // Valid entity name without semicolon
    [InlineData("&#65")]           // Valid numeric without semicolon
    [InlineData("&#x41")]          // Valid hex without semicolon
    public void TryGetHtmlEntity_ValidEntityWithoutSemicolon_ReturnsFalse(string input)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.False(result);
        Assert.True(entity.IsEmpty);
        Assert.Null(replacement);
    }

    [Theory]
    [InlineData("&amp; ")]         // Entity followed by space
    [InlineData("&amp;&lt;")]      // Multiple entities
    [InlineData("&amp;text")]      // Entity followed by text
    public void TryGetHtmlEntity_EntityFollowedByOtherCharacters_StopsAtSemicolon(string input)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.True(result);
        Assert.Equal("&amp;", entity.ToString());
        Assert.Equal("&", replacement);
    }

    [Theory]
    [InlineData("&a;")]            // Single character entity (invalid)
    [InlineData("&ab;")]           // Two character entity (invalid)
    [InlineData("&123;")]          // Numeric named entity (invalid)
    [InlineData("&a1b;")]          // Mixed alphanumeric (invalid)
    public void TryGetHtmlEntity_ShortInvalidNamedEntities_ReturnsFalse(string input)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.False(result);
        Assert.True(entity.IsEmpty);
        Assert.Null(replacement);
    }

    [Theory]
    [InlineData("&#-1;")]          // Negative number
    [InlineData("&#x-1;")]         // Negative hex
    [InlineData("&#0x-1;")]        // Negative 0x hex
    [InlineData("&#1.5;")]         // Decimal point
    [InlineData("&#x1.5;")]        // Decimal point in hex
    public void TryGetHtmlEntity_InvalidNumericFormats_ReturnsFalse(string input)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.False(result);
        Assert.True(entity.IsEmpty);
        Assert.Null(replacement);
    }

    [Fact]
    public void TryGetHtmlEntity_LongValidEntity_ReturnsTrue()
    {
        // Arrange
        var input = "&CounterClockwiseContourIntegral;"; // One of the longer entity names
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.True(result);
        Assert.Equal(input, entity.ToString());
        Assert.Equal("\u2233", replacement);
    }

    [Theory]
    // Decimal boundary tests
    [InlineData("&#33;", "!")]          // Boundary: 0x21 (valid)
    [InlineData("&#32;", " ")]          // Boundary: 0x20 (valid - minimum valid)
    [InlineData("&#31;")]               // Boundary: 0x1F (invalid - just below minimum)
    [InlineData("&#55295;", "\uD7FF")]  // Boundary: 0xD7FF (valid, just before surrogate range)
    [InlineData("&#55296;")]            // Boundary: 0xD800 (invalid - surrogate range start)
    [InlineData("&#57343;")]            // Boundary: 0xDFFF (invalid - surrogate range end)
    [InlineData("&#57344;", "\uE000")]  // Boundary: 0xE000 (valid, just after surrogate range)
    [InlineData("&#65535;", "\uFFFF")]  // Boundary: 0xFFFF (valid - maximum valid)
    [InlineData("&#65536;")]            // Boundary: 0x10000 (invalid - just above maximum)
    // Hexadecimal boundary tests
    [InlineData("&#x21;", "!")]         // Boundary: 0x21 (valid)
    [InlineData("&#x20;", " ")]         // Boundary: 0x20 (valid - minimum valid)
    [InlineData("&#x1f;")]              // Boundary: 0x1F (invalid - just below minimum)
    [InlineData("&#xd7ff;", "\uD7FF")]  // Boundary: 0xD7FF (valid, just before surrogate range)
    [InlineData("&#xd800;")]            // Boundary: 0xD800 (invalid - surrogate range start)
    [InlineData("&#xdfff;")]            // Boundary: 0xDFFF (invalid - surrogate range end)
    [InlineData("&#xe000;", "\uE000")]  // Boundary: 0xE000 (valid, just after surrogate range)
    [InlineData("&#xffff;", "\uFFFF")]  // Boundary: 0xFFFF (valid - maximum valid)
    [InlineData("&#x10000;")]           // Boundary: 0x10000 (invalid - just above maximum)
    public void TryGetHtmlEntity_BoundaryValues_HandledCorrectly(string input, string? expectedReplacement = null)
    {
        // Arrange
        var content = input.AsMemory();
        var shouldSucceed = expectedReplacement != null;

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        if (shouldSucceed)
        {
            Assert.True(result);
            Assert.Equal(input, entity.ToString());
            Assert.Equal(expectedReplacement, replacement);
        }
        else
        {
            Assert.False(result);
            Assert.True(entity.IsEmpty);
            Assert.Null(replacement);
        }
    }

    [Theory]
    [InlineData("&amp;", 5)]       // Full entity
    [InlineData("&amp;extra", 5)]  // Entity with extra content
    [InlineData("&#65;", 5)]       // Numeric entity
    [InlineData("&#x41;", 6)]      // Hex entity
    [InlineData("&#0x41;", 7)]     // 0x hex entity
    public void TryGetHtmlEntity_ReturnsCorrectEntityLength(string input, int expectedEntityLength)
    {
        // Arrange
        var content = input.AsMemory();

        // Act
        var result = ParserHelpers.TryGetHtmlEntity(content, out var entity, out var replacement);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedEntityLength, entity.Length);
        Assert.NotNull(replacement);
    }
}
