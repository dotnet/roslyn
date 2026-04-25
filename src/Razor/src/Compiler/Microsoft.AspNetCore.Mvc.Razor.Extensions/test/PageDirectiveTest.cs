// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class PageDirectiveTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_3_0;

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
        var codeDocument = ProjectEngine.CreateCodeDocument("@page \"some-route-template\" Invalid");
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        Assert.True(PageDirective.TryGetPageDirective(documentNode, out var pageDirective));

        // Assert
        Assert.Equal("some-route-template", pageDirective.RouteTemplate);
        Assert.NotNull(pageDirective.DirectiveNode);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfPageIsImported()
    {
        // Arrange
        var importSource = RazorSourceDocument.Create("@page", "import.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument("Hello world", [importSource]);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        Assert.True(PageDirective.TryGetPageDirective(documentNode, out var pageDirective));

        // Assert
        Assert.Null(pageDirective.RouteTemplate);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsFalse_IfPageDoesNotHaveDirective()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("Hello world");
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act & Assert
        Assert.False(PageDirective.TryGetPageDirective(documentNode, out _));
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfPageDoesStartWithDirective()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("Hello @page");
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        Assert.True(PageDirective.TryGetPageDirective(documentNode, out var pageDirective));

        // Assert
        Assert.Null(pageDirective.RouteTemplate);
        Assert.NotNull(pageDirective.DirectiveNode);
    }

    [Fact]
    public void TryGetPageDirective_ReturnsTrue_IfContentHasDirective()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@page");
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        Assert.True(PageDirective.TryGetPageDirective(documentNode, out var pageDirective));

        // Assert
        Assert.Null(pageDirective.RouteTemplate);
    }

    [Fact]
    public void TryGetPageDirective_ParsesRouteTemplate()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@page \"some-route-template\"");
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        Assert.True(PageDirective.TryGetPageDirective(documentNode, out var pageDirective));

        // Assert
        Assert.Equal("some-route-template", pageDirective.RouteTemplate);
    }
}
