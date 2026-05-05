// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public class MvcViewDocumentClassifierPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_2_1;

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorIntermediateNodeLoweringPhase>();
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SetsDocumentKind()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("some-content");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        Assert.Equal("mvc.1.0.view", documentNode.DocumentKind);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_NoOpsIfDocumentKindIsAlreadySet()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("some-content");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        var documentNode = processor.GetDocumentNode();
        documentNode.DocumentKind = "some-value";

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        Assert.Equal("some-value", documentNode.DocumentKind);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SetsNamespace()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("some-content");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var namespaceNode = documentNode.GetNamespaceNode();

        Assert.Equal("AspNetCore", namespaceNode.Name);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SetsClass()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("some-content", filePath: "ignored", relativePath: "Test.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var baseNode = Assert.IsType<BaseTypeWithModel>(classNode.BaseType);

        Assert.Equal("global::Microsoft.AspNetCore.Mvc.Razor.RazorPage", baseNode.BaseType.Content);
        Assert.NotNull(baseNode.ModelType);
        Assert.Equal("TModel", baseNode.ModelType.Content);
        Assert.Equal<string>(["public"], classNode.Modifiers);
        Assert.Equal("Test", classNode.Name);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_NullFilePath_SetsClass()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("some-content", filePath: null, relativePath: null);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var baseNode = Assert.IsType<BaseTypeWithModel>(classNode.BaseType);

        Assert.Equal("global::Microsoft.AspNetCore.Mvc.Razor.RazorPage", baseNode.BaseType.Content);
        Assert.NotNull(baseNode.ModelType);
        Assert.Equal("TModel", baseNode.ModelType.Content);
        Assert.Equal<string>(["public"], classNode.Modifiers);
        Assert.Equal("AspNetCore_ec563e63d931b806184cb02f79875e4f3b21d1ca043ad06699424459128b58c0", classNode.Name);
    }

    [Theory]
    [InlineData("/Views/Home/Index.cshtml", "_Views_Home_Index")]
    [InlineData("/Areas/MyArea/Views/Home/About.cshtml", "_Areas_MyArea_Views_Home_About")]
    public void MvcViewDocumentClassifierPass_UsesRelativePathToGenerateTypeName(string relativePath, string expected)
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("some-content", filePath: "ignored", relativePath: relativePath);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(expected, classNode.Name);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_UsesAbsolutePath_IfRelativePathIsNotSet()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("some-content", filePath: @"x::\application\Views\Home\Index.cshtml", relativePath: null);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("x___application_Views_Home_Index", classNode.Name);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SanitizesClassName()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@page", filePath: @"x:\Test.cshtml", relativePath: "path.with+invalid-chars");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("path_with_invalid_chars", classNode.Name);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SetsUpExecuteAsyncMethod()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("some-content");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var methodNode = documentNode.GetMethodNode();

        Assert.Equal("ExecuteAsync", methodNode.Name);
        Assert.Equal("global::System.Threading.Tasks.Task", methodNode.ReturnType);
        Assert.Equal<string>(["public", "async", "override"], methodNode.Modifiers);
    }
}
