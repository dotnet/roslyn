// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultTagHelperResolutionPhaseTest
{
    [Fact]
    public void MergeSourceSpans_SameLine_ReturnsCorrectSpan()
    {
        // Arrange
        var filePath = "test.razor";
        var first = new SourceSpan(filePath, absoluteIndex: 10, lineIndex: 2, characterIndex: 5, length: 3, lineCount: 0, endCharacterIndex: 8);
        var last  = new SourceSpan(filePath, absoluteIndex: 15, lineIndex: 2, characterIndex: 10, length: 4, lineCount: 0, endCharacterIndex: 14);

        // Act
        var result = DefaultTagHelperResolutionPhase.MergeSourceSpans(first, last);

        // Assert
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal(10, result.AbsoluteIndex);    // starts at first
        Assert.Equal(2, result.LineIndex);          // same line as first
        Assert.Equal(5, result.CharacterIndex);     // same column as first
        Assert.Equal(9, result.Length);             // (15 + 4) - 10 = 9
        Assert.Equal(0, result.LineCount);          // 2 + 0 - 2 = 0 (same line)
        Assert.Equal(14, result.EndCharacterIndex); // taken from last
    }

    [Fact]
    public void MergeSourceSpans_MultiLine_ReturnsCorrectSpan()
    {
        // Arrange
        var filePath = "test.razor";
        // first spans lines 1-2 (lineCount = 1 means it crosses into the next line)
        var first = new SourceSpan(filePath, absoluteIndex: 0, lineIndex: 1, characterIndex: 0, length: 10, lineCount: 1, endCharacterIndex: 5);
        // last is on line 3 (lineIndex = 3)
        var last  = new SourceSpan(filePath, absoluteIndex: 20, lineIndex: 3, characterIndex: 2, length: 5, lineCount: 0, endCharacterIndex: 7);

        // Act
        var result = DefaultTagHelperResolutionPhase.MergeSourceSpans(first, last);

        // Assert
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal(0, result.AbsoluteIndex);     // starts at first
        Assert.Equal(1, result.LineIndex);          // line of first
        Assert.Equal(0, result.CharacterIndex);     // column of first
        Assert.Equal(25, result.Length);            // (20 + 5) - 0 = 25
        Assert.Equal(2, result.LineCount);          // (3 + 0) - 1 = 2
        Assert.Equal(7, result.EndCharacterIndex);  // end column from last
    }

    [Fact]
    public void MergeSourceSpans_AdjacentSpans_ReturnsCorrectSpan()
    {
        // Arrange
        var filePath = "test.razor";
        var first = new SourceSpan(filePath, absoluteIndex: 5, lineIndex: 0, characterIndex: 5, length: 3, lineCount: 0, endCharacterIndex: 8);
        // last starts right where first ends
        var last  = new SourceSpan(filePath, absoluteIndex: 8, lineIndex: 0, characterIndex: 8, length: 4, lineCount: 0, endCharacterIndex: 12);

        // Act
        var result = DefaultTagHelperResolutionPhase.MergeSourceSpans(first, last);

        // Assert
        Assert.Equal(5, result.AbsoluteIndex);
        Assert.Equal(7, result.Length);             // (8 + 4) - 5 = 7
        Assert.Equal(0, result.LineCount);
        Assert.Equal(12, result.EndCharacterIndex);
    }

    [Fact]
    public void MergeSourceSpans_SameSpan_ReturnsEquivalentSpan()
    {
        // Arrange
        var filePath = "test.razor";
        var span = new SourceSpan(filePath, absoluteIndex: 10, lineIndex: 1, characterIndex: 3, length: 5, lineCount: 0, endCharacterIndex: 8);

        // Act — first and last are the same span
        var result = DefaultTagHelperResolutionPhase.MergeSourceSpans(span, span);

        // Assert
        Assert.Equal(10, result.AbsoluteIndex);
        Assert.Equal(1, result.LineIndex);
        Assert.Equal(3, result.CharacterIndex);
        Assert.Equal(5, result.Length);             // (10 + 5) - 10 = 5
        Assert.Equal(0, result.LineCount);          // 1 + 0 - 1 = 0
        Assert.Equal(8, result.EndCharacterIndex);
    }

    [Fact]
    public void MergeSourceSpans_NullFilePath_PreservesNullFilePath()
    {
        // Arrange — file path is null (e.g. for in-memory content)
        var first = new SourceSpan(filePath: null, absoluteIndex: 0, lineIndex: 0, characterIndex: 0, length: 3, lineCount: 0, endCharacterIndex: 3);
        var last  = new SourceSpan(filePath: null, absoluteIndex: 5, lineIndex: 0, characterIndex: 5, length: 2, lineCount: 0, endCharacterIndex: 7);

        // Act
        var result = DefaultTagHelperResolutionPhase.MergeSourceSpans(first, last);

        // Assert
        Assert.Null(result.FilePath);
        Assert.Equal(0, result.AbsoluteIndex);
        Assert.Equal(7, result.Length);             // (5 + 2) - 0 = 7
    }
}
