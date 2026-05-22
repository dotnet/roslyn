// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class RazorPageDocumentClassifierPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_3_0;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        PageDirective.Register(builder);
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorIntermediateNodeLoweringPhase>();
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_LogsErrorForImportedPageDirectives()
    {
        // Arrange
        var sourceSpan = new SourceSpan(
            filePath: "import.cshtml",
            absoluteIndex: 0,
            lineIndex: 0,
            characterIndex: 0,
            length: 5,
            lineCount: 0,
            endCharacterIndex: 5);

        var expectedDiagnostic = RazorExtensionsDiagnosticFactory.CreatePageDirective_CannotBeImported(sourceSpan);

        var source = TestRazorSourceDocument.Create("<p>Hello World</p>", filePath: "main.cshtml");
        var importSource = TestRazorSourceDocument.Create("@page", filePath: "import.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source, [importSource]);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        var pageDirectives = documentNode.FindDirectiveReferences(PageDirective.Directive);
        var directive = Assert.Single(pageDirectives);
        var diagnostic = Assert.Single(directive.Node.Diagnostics);
        Assert.Equal(expectedDiagnostic, diagnostic);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_LogsErrorIfDirectiveNotAtTopOfFile()
    {
        // Arrange
        var sourceSpan = new SourceSpan(
            "test.cshtml",
            absoluteIndex: 14 + Environment.NewLine.Length * 2,
            lineIndex: 2,
            characterIndex: 0,
            length: 5 + Environment.NewLine.Length,
            lineCount: 1,
            endCharacterIndex: 0);

        var expectedDiagnostic = RazorExtensionsDiagnosticFactory.CreatePageDirective_MustExistAtTheTopOfFile(sourceSpan);

        var content = """
            
            @somethingelse
            @page
            
            """;
        var codeDocument = ProjectEngine.CreateCodeDocument(content);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        var pageDirectives = documentNode.FindDirectiveReferences(PageDirective.Directive);
        var directive = Assert.Single(pageDirectives);
        var diagnostic = Assert.Single(directive.Node.Diagnostics);
        Assert.Equal(expectedDiagnostic, diagnostic);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_DoesNotLogErrorIfCommentAndWhitespaceBeforeDirective()
    {
        // Arrange
        var content = @"
@* some comment *@

@page
";
        var codeDocument = ProjectEngine.CreateCodeDocument(content);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        var pageDirectives = documentNode.FindDirectiveReferences(PageDirective.Directive);
        var directive = Assert.Single(pageDirectives);
        Assert.Empty(directive.Node.Diagnostics);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_SetsDocumentKind()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@page");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        Assert.Equal("mvc.1.0.razor-page", documentNode.DocumentKind);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_NoOpsIfDocumentKindIsAlreadySet()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@page");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        var documentNode = processor.GetDocumentNode();
        documentNode.DocumentKind = "some-value";

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        Assert.Equal("some-value", documentNode.DocumentKind);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_NoOpsIfPageDirectiveIsMalformed()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@page+1");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        var documentNode = processor.GetDocumentNode();
        documentNode.DocumentKind = "some-value";

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        Assert.Equal("some-value", documentNode.DocumentKind);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_SetsNamespace()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@page");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var namespaceNode = documentNode.GetNamespaceNode();

        Assert.Equal("AspNetCore", namespaceNode.Name);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_SetsClass()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@page", filePath: "ignored", relativePath: "Test.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("global::Microsoft.AspNetCore.Mvc.RazorPages.Page", classNode.BaseType?.BaseType.Content);
        Assert.Equal<string>(["public"], classNode.Modifiers);
        Assert.Equal("Test", classNode.Name);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_NullFilePath_SetsClass()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@page", filePath: null, relativePath: null);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("global::Microsoft.AspNetCore.Mvc.RazorPages.Page", classNode.BaseType?.BaseType.Content);
        Assert.Equal<string>(["public"], classNode.Modifiers);
        AssertEx.Equal("AspNetCore_c3b458108610c1a2aa6eede0a5685ede853e036732db515609b2a23ca15359e1", classNode.Name);
    }

    [Theory]
    [InlineData("/Views/Home/Index.cshtml", "_Views_Home_Index")]
    [InlineData("/Areas/MyArea/Views/Home/About.cshtml", "_Areas_MyArea_Views_Home_About")]
    public void RazorPageDocumentClassifierPass_UsesRelativePathToGenerateTypeName(string relativePath, string expected)
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@page", filePath: "ignored", relativePath: relativePath);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(expected, classNode.Name);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_UsesAbsolutePath_IfRelativePathIsNotSet()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@page", filePath: @"x::\application\Views\Home\Index.cshtml", relativePath: null);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("x___application_Views_Home_Index", classNode.Name);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_SanitizesClassName()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@page", filePath: @"x:\Test.cshtml", relativePath: "path.with+invalid-chars");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("path_with_invalid_chars", classNode.Name);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_SetsUpExecuteAsyncMethod()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@page");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var methodNode = documentNode.GetMethodNode();

        Assert.Equal("ExecuteAsync", methodNode.Name);
        Assert.Equal("global::System.Threading.Tasks.Task", methodNode.ReturnType);
        Assert.Equal<string>(["public", "async", "override"], methodNode.Modifiers);
    }

    [Fact]
    public void RazorPageDocumentClassifierPass_AddsRouteTemplateMetadata()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@page \"some-route\"", filePath: "ignored", relativePath: "Test.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<RazorPageDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var extensionNode = documentNode.GetExtensionNode();
        var attributeNode = Assert.IsType<RazorCompiledItemMetadataAttributeIntermediateNode>(extensionNode);

        Assert.Equal("RouteTemplate", attributeNode.Key);
        Assert.Equal("some-route", attributeNode.Value);
    }
}
