// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class DefaultMetadataIdentifierFeatureTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new DefaultMetadataIdentifierFeature());
    }

    [Fact]
    public void GetIdentifier_ReturnsNull_ForNullRelativePath()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("content", filePath: "Test.cshtml", relativePath: null);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        Assert.True(ProjectEngine.Engine.TryGetFeature<IMetadataIdentifierFeature>(out var feature));

        // Act
        var result = feature.GetIdentifier(codeDocument, source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetIdentifier_ReturnsNull_ForEmptyRelativePath()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("content", filePath: "Test.cshtml", relativePath: string.Empty);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        Assert.True(ProjectEngine.Engine.TryGetFeature<IMetadataIdentifierFeature>(out var feature));

        // Act
        var result = feature.GetIdentifier(codeDocument, source);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Test.cshtml", "/Test.cshtml")]
    [InlineData("/Test.cshtml", "/Test.cshtml")]
    [InlineData("\\Test.cshtml", "/Test.cshtml")]
    [InlineData("\\About\\Test.cshtml", "/About/Test.cshtml")]
    [InlineData("\\About\\Test\\cshtml", "/About/Test/cshtml")]
    public void GetIdentifier_SanitizesRelativePath(string relativePath, string expected)
    {
        // Arrange
        var sourceDocument = TestRazorSourceDocument.Create("content", filePath: "Test.cshtml", relativePath: relativePath);
        var codeDocument = ProjectEngine.CreateCodeDocument(sourceDocument);

        Assert.True(ProjectEngine.Engine.TryGetFeature<IMetadataIdentifierFeature>(out var feature));

        // Act
        var result = feature.GetIdentifier(codeDocument, sourceDocument);

        // Assert
        Assert.Equal(expected, result);
    }
}
