// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public class PageDirectiveTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_2_1;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        PageDirective.Register(builder);
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorDocumentClassifierPhase>();
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfPageIsMalformed()
    {
        // Arrange
        var content = "@page \"some-route-template\" Invalid";
        var source = TestRazorSourceDocument.Create(content, filePath: "file");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        var result = PageDirective.TryGetPageDirective(documentNode, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Equal("some-route-template", pageDirective.RouteTemplate);
        Assert.NotNull(pageDirective.DirectiveNode);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfPageIsImported()
    {
        // Arrange
        var content = "Hello world";
        var source = TestRazorSourceDocument.Create(content, filePath: "file");
        var importSource = TestRazorSourceDocument.Create("@page", filePath: "imports.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source, [importSource]);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        var result = PageDirective.TryGetPageDirective(documentNode, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Null(pageDirective.RouteTemplate);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsFalse_IfPageDoesNotHaveDirective()
    {
        // Arrange
        var content = "Hello world";
        var source = TestRazorSourceDocument.Create(content, filePath: "file");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        var result = PageDirective.TryGetPageDirective(documentNode, out var pageDirective);

        // Assert
        Assert.False(result);
        Assert.Null(pageDirective);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfPageDoesStartWithDirective()
    {
        // Arrange
        var content = "Hello @page";
        var source = TestRazorSourceDocument.Create(content, filePath: "file");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        var result = PageDirective.TryGetPageDirective(documentNode, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Null(pageDirective.RouteTemplate);
        Assert.NotNull(pageDirective.DirectiveNode);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfContentHasDirective()
    {
        // Arrange
        var content = "@page";
        var source = TestRazorSourceDocument.Create(content, filePath: "file");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        var result = PageDirective.TryGetPageDirective(documentNode, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Null(pageDirective.RouteTemplate);
    }

    [Fact]
    public void TryGetPageDirective_ParsesRouteTemplate()
    {
        // Arrange
        var content = "@page \"some-route-template\"";
        var source = TestRazorSourceDocument.Create(content, filePath: "file");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        var result = PageDirective.TryGetPageDirective(documentNode, out var pageDirective);

        // Assert
        Assert.True(result);
        Assert.Equal("some-route-template", pageDirective.RouteTemplate);
    }
}
