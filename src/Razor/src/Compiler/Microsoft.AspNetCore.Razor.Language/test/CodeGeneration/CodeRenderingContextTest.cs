// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class CodeRenderingContextTest
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(9999, true)]
    public void GetDiagnostics_FiltersWarningsByLevel(int warningLevel, bool expectDiagnostic)
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZTest", "Test warning for '{0}'", RazorDiagnosticSeverity.Warning, warningLevel: 12);
        var diagnostic = RazorDiagnostic.Create(descriptor, SourceSpan.Undefined, "param");

        var documentNode = new DocumentIntermediateNode();
        documentNode.AddDiagnostic(diagnostic);

        var options = RazorCodeGenerationOptions.Default.WithRazorWarningLevel(warningLevel);
        using var context = new CodeRenderingContext(
            RuntimeNodeWriter.Instance,
            TestRazorSourceDocument.Create(),
            documentNode,
            options);

        // Act
        var diagnostics = context.GetDiagnostics();

        // Assert
        if (expectDiagnostic)
        {
            Assert.Contains(diagnostics, d => d.Id == "RZTest");
        }
        else
        {
            Assert.DoesNotContain(diagnostics, d => d.Id == "RZTest");
        }
    }

    [Fact]
    public void GetDiagnostics_AlwaysOnDiagnostics_NotFilteredAtAnyLevel()
    {
        // Arrange — level 0 diagnostics are always on
        var descriptor = new RazorDiagnosticDescriptor("RZAlways", "Always on: '{0}'", RazorDiagnosticSeverity.Warning);
        var diagnostic = RazorDiagnostic.Create(descriptor, SourceSpan.Undefined, "param");

        var documentNode = new DocumentIntermediateNode();
        documentNode.AddDiagnostic(diagnostic);

        var options = RazorCodeGenerationOptions.Default.WithRazorWarningLevel(0);
        using var context = new CodeRenderingContext(
            RuntimeNodeWriter.Instance,
            TestRazorSourceDocument.Create(),
            documentNode,
            options);

        // Act
        var diagnostics = context.GetDiagnostics();

        // Assert — level 0 diagnostic should be present even at warning level 0
        Assert.Contains(diagnostics, d => d.Id == "RZAlways");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public void GetDiagnostics_MultipleLevels_FiltersCorrectly(int warningLevel)
    {
        // Arrange — diagnostics at levels 0, 11, 12, and 13
        var alwaysOn = RazorDiagnostic.Create(
            new RazorDiagnosticDescriptor("RZ0", "Always", RazorDiagnosticSeverity.Warning),
            SourceSpan.Undefined);
        var level11 = RazorDiagnostic.Create(
            new RazorDiagnosticDescriptor("RZ11", "Level 11", RazorDiagnosticSeverity.Warning, warningLevel: 11),
            SourceSpan.Undefined);
        var level12 = RazorDiagnostic.Create(
            new RazorDiagnosticDescriptor("RZ12", "Level 12", RazorDiagnosticSeverity.Warning, warningLevel: 12),
            SourceSpan.Undefined);
        var level13 = RazorDiagnostic.Create(
            new RazorDiagnosticDescriptor("RZ13", "Level 13", RazorDiagnosticSeverity.Warning, warningLevel: 13),
            SourceSpan.Undefined);

        var documentNode = new DocumentIntermediateNode();
        documentNode.AddDiagnostic(alwaysOn);
        documentNode.AddDiagnostic(level11);
        documentNode.AddDiagnostic(level12);
        documentNode.AddDiagnostic(level13);

        var options = RazorCodeGenerationOptions.Default.WithRazorWarningLevel(warningLevel);
        using var context = new CodeRenderingContext(
            RuntimeNodeWriter.Instance,
            TestRazorSourceDocument.Create(),
            documentNode,
            options);

        // Act
        var diagnostics = context.GetDiagnostics();

        // Assert — always-on is always present
        Assert.Contains(diagnostics, d => d.Id == "RZ0");

        // Each level is present only when warningLevel >= that level
        Assert.Equal(warningLevel >= 11, diagnostics.Any(d => d.Id == "RZ11"));
        Assert.Equal(warningLevel >= 12, diagnostics.Any(d => d.Id == "RZ12"));
        Assert.Equal(warningLevel >= 13, diagnostics.Any(d => d.Id == "RZ13"));
    }
}
