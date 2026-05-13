// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Components;

public class ComponentDocumentClassifierPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorIntermediateNodeLoweringPhase>();
    }

    [Fact]
    public void Execute_SetsDocumentKind()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("some-content", filePath: "Test.razor");
        var codeDocument = ProjectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ComponentDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        Assert.Equal(ComponentDocumentClassifierPass.ComponentDocumentKind, documentNode.DocumentKind);
    }

    [Fact]
    public void ComponentDocumentClassifierPass_SetsNamespace()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.SetRootNamespace("MyApp");
        });

        var source = TestRazorSourceDocument.Create("some-content", filePath: "/MyApp/Test.razor", relativePath: "Test.razor");
        var codeDocument = projectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(projectEngine, codeDocument);

        // Act
        processor.ExecutePass<ComponentDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var namespaceNode = documentNode.GetNamespaceNode();

        Assert.Equal("MyApp", namespaceNode.Name);
    }

    [Fact]
    public void ComponentDocumentClassifierPass_SetsClass()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.SetRootNamespace("MyApp");
        });

        var source = TestRazorSourceDocument.Create("some-content", filePath: "/MyApp/Test.razor", relativePath: "Test.razor");
        var codeDocument = projectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(projectEngine, codeDocument);

        // Act
        processor.ExecutePass<ComponentDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal($"global::{ComponentsApi.ComponentBase.FullTypeName}", classNode.BaseType?.BaseType.Content);
        Assert.Equal<string>(["public", "partial"], classNode.Modifiers);
        Assert.Equal("Test", classNode.Name);
    }

    [Fact]
    public void ComponentDocumentClassifierPass_UsesRelativePathToGenerateTypeNameAndNamespace()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.SetRootNamespace("MyApp");
        });

        var relativePath = "/Pages/Announcements/Banner.razor";
        var source = TestRazorSourceDocument.Create("some-content", filePath: $"/MyApp{relativePath}", relativePath: relativePath);
        var codeDocument = projectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(projectEngine, codeDocument);

        // Act
        processor.ExecutePass<ComponentDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var namespaceNode = documentNode.GetNamespaceNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("Banner", classNode.Name);
        Assert.Equal("MyApp.Pages.Announcements", namespaceNode.Name);
    }

    [Fact]
    public void ComponentDocumentClassifierPass_SanitizesClassName()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.SetRootNamespace("My.+App");
        });

        var source = TestRazorSourceDocument.Create("some-content", filePath: @"x:\My.+App\path.with+invalid-chars.razor", relativePath: "path.with+invalid-chars.razor");
        var codeDocument = projectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(projectEngine, codeDocument);

        // Act
        processor.ExecutePass<ComponentDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var namespaceNode = documentNode.GetNamespaceNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("path_with_invalid_chars", classNode.Name);
        Assert.Equal("My._App", namespaceNode.Name);
    }

    [Fact]
    public void ComponentDocumentClassifierPass_SetsUpMainMethod()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("some-content", filePath: "Test.razor");
        var codeDocument = ProjectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ComponentDocumentClassifierPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var methodNode = documentNode.GetMethodNode();

        Assert.Equal(ComponentsApi.ComponentBase.BuildRenderTree, methodNode.Name);
        Assert.Equal("void", methodNode.ReturnType);
        Assert.Equal<string>(["protected", "override"], methodNode.Modifiers);
    }
}
