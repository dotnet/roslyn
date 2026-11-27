// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class ConsolidatedMvcViewDocumentClassifierPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorIntermediateNodeLoweringPhase>();
    }

    [Fact]
    public void ConsolidatedMvcViewDocumentClassifierPass_SetsDifferentNamespace()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("some-content");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>(() => new(useConsolidatedMvcViews: true));

        // Assert
        var documentNode = processor.GetDocumentNode();
        var namespaceNode = documentNode.GetNamespaceNode();

        Assert.Equal("AspNetCoreGeneratedDocument", namespaceNode.Name);
    }

    [Fact]
    public void ConsolidatedMvcViewDocumentClassifierPass_SetsClass()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("some-content", filePath: "ignored", relativePath: "Test.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>(() => new(useConsolidatedMvcViews: true));

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var baseNode = Assert.IsType<BaseTypeWithModel>(classNode.BaseType);

        Assert.Equal("global::Microsoft.AspNetCore.Mvc.Razor.RazorPage", baseNode.BaseType.Content);
        Assert.NotNull(baseNode.ModelType);
        Assert.Equal("TModel", baseNode.ModelType.Content);
        Assert.Equal<string>(["internal", "sealed"], classNode.Modifiers);
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
        processor.ExecutePass<MvcViewDocumentClassifierPass>(() => new(useConsolidatedMvcViews: true));

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var baseNode = Assert.IsType<BaseTypeWithModel>(classNode.BaseType);

        Assert.Equal("global::Microsoft.AspNetCore.Mvc.Razor.RazorPage", baseNode.BaseType.Content);
        Assert.NotNull(baseNode.ModelType);
        Assert.Equal("TModel", baseNode.ModelType.Content);
        Assert.Equal<string>(["internal", "sealed"], classNode.Modifiers);
        AssertEx.Equal("AspNetCore_ec563e63d931b806184cb02f79875e4f3b21d1ca043ad06699424459128b58c0", classNode.Name);
    }

    [Theory]
    [InlineData("/Views/Home/Index.cshtml", "_Views_Home_Index")]
    [InlineData("/Areas/MyArea/Views/Home/About.cshtml", "_Areas_MyArea_Views_Home_About")]
    public void ConsolidatedMvcViewDocumentClassifierPass_UsesRelativePathToGenerateTypeName(string relativePath, string expected)
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("some-content", filePath: "ignored", relativePath: relativePath);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>(() => new(useConsolidatedMvcViews: true));

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(expected, classNode.Name);
        Assert.Equal<string>(["internal", "sealed"], classNode.Modifiers);
    }

    [Fact]
    public void ConsolidatedMvcViewDocumentClassifierPass_SetsUpExecuteAsyncMethod()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("some-content");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>(() => new(useConsolidatedMvcViews: true));

        // Assert
        var documentNode = processor.GetDocumentNode();
        var methodNode = documentNode.GetMethodNode();

        Assert.Equal("ExecuteAsync", methodNode.Name);
        Assert.Equal("global::System.Threading.Tasks.Task", methodNode.ReturnType);
        Assert.Equal<string>(["public", "async", "override"], methodNode.Modifiers);
    }
}
