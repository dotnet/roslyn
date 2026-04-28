// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class CreateNewOnMetadataUpdateAttributePassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_6_0;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        PageDirective.Register(builder);
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorIntermediateNodeLoweringPhase>();
    }

    [Fact]
    public void Execute_AddsAttributes()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("Hello world", filePath: "ignored", relativePath: "Test.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<MvcViewDocumentClassifierPass>();
        processor.ExecutePass<CreateNewOnMetadataUpdateAttributePass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var extensionNodes = documentNode.GetExtensionNodes();

        Assert.Collection(
            extensionNodes,
            node =>
            {
                var attributeNode = Assert.IsType<RazorCompiledItemMetadataAttributeIntermediateNode>(node);
                Assert.Equal("Identifier", attributeNode.Key);
                Assert.Equal("/Test.cshtml", attributeNode.Value);
            },
            node =>
            {
                Assert.IsType<CreateNewOnMetadataUpdateAttributePass.CreateNewOnMetadataUpdateAttributeIntermediateNode>(node);
            });
    }

    [Fact]
    public void Execute_NoOpsForBlazorComponents()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("Hello world", filePath: "ignored", relativePath: "Test.razor");
        var codeDocument = ProjectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<DefaultDocumentClassifierPass>();
        processor.ExecutePass<CreateNewOnMetadataUpdateAttributePass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var extensionNodes = documentNode.GetExtensionNodes();

        Assert.Empty(extensionNodes);
    }
}
