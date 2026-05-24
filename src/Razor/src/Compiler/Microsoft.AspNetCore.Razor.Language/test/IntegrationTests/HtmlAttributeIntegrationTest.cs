// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class HtmlAttributeIntegrationTest() : IntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void HtmlWithDataDashAttribute()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetRequiredDocumentNode());
    }

    [Fact]
    public void HtmlWithConditionalAttribute()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetRequiredDocumentNode());
    }
}
