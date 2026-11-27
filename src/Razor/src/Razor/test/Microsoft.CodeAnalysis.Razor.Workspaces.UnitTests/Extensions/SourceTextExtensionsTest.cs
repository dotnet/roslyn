// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test.Extensions;

public class SourceTextExtensionsTest
{
    [Fact]
    public void TryGetAbsoluteIndex_ClampsCharacterPastLineEnd()
    {
        var sourceText = SourceText.From("hello\r\nworld");

        var result = sourceText.TryGetAbsoluteIndex(line: 0, character: int.MaxValue, out var absoluteIndex);

        Assert.True(result);
        Assert.Equal(5, absoluteIndex);
    }

    [Fact]
    public void TryGetAbsoluteIndex_ClampsCharacterInsideLineBreakToLineEnd()
    {
        var sourceText = SourceText.From("hello\r\nworld");

        var result = sourceText.TryGetAbsoluteIndex(line: 0, character: 6, out var absoluteIndex);

        Assert.True(result);
        Assert.Equal(5, absoluteIndex);
    }

    [Fact]
    public void GetTextSpan_ClampsCharactersPastLineEnd()
    {
        var sourceText = SourceText.From("hello\r\nworld");

        var textSpan = sourceText.GetTextSpan(startLine: 0, startCharacter: 0, endLine: 0, endCharacter: int.MaxValue);

        Assert.Equal(new TextSpan(0, 5), textSpan);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/84104")]
    public void TryGetTextSpan_ReturnsFalse_WhenEndLinePastEnd()
    {
        var sourceText = SourceText.From("hello\r\nworld");

        var result = sourceText.TryGetTextSpan(
            startLine: 0,
            startCharacter: 0,
            endLine: 28,
            endCharacter: 0,
            out var textSpan);

        Assert.False(result);
        Assert.Equal(default, textSpan);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/84104")]
    public void TryGetTextSpan_ReturnsFalse_WhenEndBeforeStart()
    {
        var sourceText = SourceText.From("hello\r\nworld");

        var result = sourceText.TryGetTextSpan(
            startLine: 1,
            startCharacter: 0,
            endLine: 0,
            endCharacter: 0,
            out var textSpan);

        Assert.False(result);
        Assert.Equal(default, textSpan);
    }
}