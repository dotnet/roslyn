// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language;

public class DirectiveRemovalOptimizationPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        var directive = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, d => d.AddStringToken());
        builder.AddDirective(directive);
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorDirectiveClassifierPhase>();
    }

    [Fact]
    public void Execute_Custom_RemovesDirectiveNodeFromDocument()
    {
        // Arrange
        var content = "@custom \"Hello\"";
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<DirectiveRemovalOptimizationPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        Children(documentNode,
            node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));

        var @namespace = documentNode.Children[0];
        Children(@namespace,
            node => Assert.IsType<ClassDeclarationIntermediateNode>(node));

        var @class = @namespace.Children[0];
        var method = SingleChild<MethodDeclarationIntermediateNode>(@class);
        Assert.Empty(method.Children);
    }

    [Fact]
    public void Execute_MultipleCustomDirectives_RemovesDirectiveNodesFromDocument()
    {
        // Arrange
        var content = """
            @custom "Hello"
            @custom "World"
            """;
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<DirectiveRemovalOptimizationPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        Children(documentNode,
            node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));

        var @namespace = documentNode.Children[0];
        Children(@namespace,
            node => Assert.IsType<ClassDeclarationIntermediateNode>(node));

        var @class = @namespace.Children[0];
        var method = SingleChild<MethodDeclarationIntermediateNode>(@class);
        Assert.Empty(method.Children);
    }

    [Fact]
    public void Execute_DirectiveWithError_PreservesDiagnosticsAndRemovesDirectiveNodeFromDocument()
    {
        // Arrange
        var content = "@custom \"Hello\"";
        var expectedDiagnostic = RazorDiagnostic.Create(new RazorDiagnosticDescriptor("RZ9999", "Some diagnostic message.", RazorDiagnosticSeverity.Error));
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Add the diagnostic to the directive node.
        var directiveNode = documentNode.FindDescendantNodes<DirectiveIntermediateNode>().Single();
        directiveNode.AddDiagnostic(expectedDiagnostic);

        // Act
        processor.ExecutePass<DirectiveRemovalOptimizationPass>();

        // Assert
        var diagnostic = Assert.Single(documentNode.Diagnostics);
        Assert.Equal(expectedDiagnostic, diagnostic);

        Children(documentNode,
            node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));

        var @namespace = documentNode.Children[0];
        Children(@namespace,
            node => Assert.IsType<ClassDeclarationIntermediateNode>(node));

        var @class = @namespace.Children[0];
        var method = SingleChild<MethodDeclarationIntermediateNode>(@class);
        Assert.Empty(method.Children);
    }
}
