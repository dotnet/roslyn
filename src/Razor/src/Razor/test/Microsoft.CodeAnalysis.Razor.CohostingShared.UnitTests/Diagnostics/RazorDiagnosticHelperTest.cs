// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Diagnostics;

public class RazorDiagnosticHelperTest
{
    [Fact]
    public void Convert_Converts()
    {
        // Arrange
        var razorDiagnostic = RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported("test");
        var sourceText = SourceText.From(string.Empty);

        // Act
        var diagnostic = RazorDiagnosticHelper.ConvertToVSDiagnostic(razorDiagnostic, sourceText, documentSnapshot: null);

        // Assert
        Assert.Equal(razorDiagnostic.Id, diagnostic.Code);
        Assert.Equal(razorDiagnostic.GetMessage(CultureInfo.InvariantCulture), diagnostic.Message);
        Assert.Null(diagnostic.Range);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void ConvertSeverity_ErrorReturnsError()
    {
        // Arrange
        var expectedSeverity = DiagnosticSeverity.Error;

        // Act
        var severity = RazorDiagnosticHelper.ConvertSeverity(RazorDiagnosticSeverity.Error);

        // Assert
        Assert.Equal(expectedSeverity, severity);
    }

    [Fact]
    public void ConvertSeverity_WarningReturnsWarning()
    {
        // Arrange
        var expectedSeverity = DiagnosticSeverity.Warning;

        // Act
        var severity = RazorDiagnosticHelper.ConvertSeverity(RazorDiagnosticSeverity.Warning);

        // Assert
        Assert.Equal(expectedSeverity, severity);
    }

    [Fact]
    public void ConvertSpanToRange_ReturnsConvertedRange()
    {
        // Arrange
        var sourceSpan = new SourceSpan(3, 0, 3, 4);
        var sourceText = SourceText.From("Hello World");
        var expectedRange = LspFactory.CreateSingleLineRange(line: 0, character: 3, length: 4);

        // Act
        var range = RazorDiagnosticHelper.ConvertSpanToRange(sourceSpan, sourceText);

        // Assert
        Assert.Equal("lo W", sourceText.ToString(sourceText.GetTextSpan(range)));
        Assert.Equal(expectedRange, range);
    }

    [Fact]
    public void ConvertSpanToRange_StartsOutsideOfDocument_EmptyDocument_NormalizesTo0()
    {
        // Arrange
        var sourceText = SourceText.From(string.Empty);
        var sourceSpan = new SourceSpan(5, 0, 5, 4);
        var expectedRange = LspFactory.DefaultRange;

        // Act
        var range = RazorDiagnosticHelper.ConvertSpanToRange(sourceSpan, sourceText);

        // Assert
        Assert.Equal("", sourceText.ToString(sourceText.GetTextSpan(range)));
        Assert.Equal(expectedRange, range);
    }

    [Fact]
    public void ConvertSpanToRange_StartsOutsideOfDocument_NormalizesToEnd()
    {
        // Arrange
        var sourceText = SourceText.From("Hello World");
        var sourceSpan = new SourceSpan(sourceText.Length + 5, 0, sourceText.Length + 5, 4);
        var expectedRange = LspFactory.CreateZeroWidthRange(0, 11);

        // Act
        var range = RazorDiagnosticHelper.ConvertSpanToRange(sourceSpan, sourceText);

        // Assert
        Assert.Equal("", sourceText.ToString(sourceText.GetTextSpan(range)));
        Assert.Equal(expectedRange, range);
    }

    [Fact]
    public void ConvertSpanToRange_EndsOutsideOfDocument_NormalizesToEnd()
    {
        // Arrange
        var sourceText = SourceText.From("Hello World");
        var sourceSpan = new SourceSpan(6, 0, 6, 15);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 0, character: 6, length: 5);

        // Act
        var range = RazorDiagnosticHelper.ConvertSpanToRange(sourceSpan, sourceText);

        // Assert
        Assert.Equal("World", sourceText.ToString(sourceText.GetTextSpan(range)));
        Assert.Equal(expectedRange, range);
    }

    [Fact]
    public void ConvertSpanToRange_ReturnsNullIfSpanIsUndefined()
    {
        // Arrange
        var sourceSpan = SourceSpan.Undefined;
        var sourceText = SourceText.From(string.Empty);

        // Act
        var range = RazorDiagnosticHelper.ConvertSpanToRange(sourceSpan, sourceText);

        // Assert
        Assert.Null(range);
    }
}
