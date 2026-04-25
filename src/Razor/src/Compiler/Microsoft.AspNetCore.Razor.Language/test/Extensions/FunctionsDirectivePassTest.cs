// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class FunctionsDirectivePassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorDocumentClassifierPhase>();
    }

    [Fact]
    public void Execute_SkipsDocumentWithNoClassNode()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@functions { var value = true; }");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode();
        documentNode.Children.Add(new DirectiveIntermediateNode() { Directive = FunctionsDirective.Directive });

        // Act
        ProjectEngine.ExecutePass<FunctionsDirectivePass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            node => Assert.IsType<DirectiveIntermediateNode>(node));
    }

    [Fact]
    public void Execute_AddsStatementsToClassLevel()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("@functions { var value = true; }");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<FunctionsDirectivePass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        Children(
            documentNode,
            node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));

        var @namespace = documentNode.Children[0];
        Children(
            @namespace,
            node => Assert.IsType<ClassDeclarationIntermediateNode>(node));

        var @class = @namespace.Children[0];
        Children(
            @class,
            node => Assert.IsType<MethodDeclarationIntermediateNode>(node),
            node => CSharpCode(" var value = true; ", node));

        var method = @class.Children[0];
        Assert.Empty(method.Children);
    }

    [Fact]
    public void Execute_ComponentCodeDirective_AddsStatementsToClassLevel()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.AddDirective(ComponentCodeDirective.Directive);
        });

        var source = TestRazorSourceDocument.Create("@code { var value = true; }");
        var codeDocument = projectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(projectEngine, codeDocument);

        // Act
        processor.ExecutePass<FunctionsDirectivePass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        Children(
            documentNode,
            node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));

        var @namespace = documentNode.Children[0];
        Children(
            @namespace,
            node => Assert.IsType<ClassDeclarationIntermediateNode>(node));

        var @class = @namespace.Children[0];
        Children(
            @class,
            node => Assert.IsType<MethodDeclarationIntermediateNode>(node),
            node => CSharpCode(" var value = true; ", node));

        var method = @class.Children[0];
        Assert.Empty(method.Children);
    }

    [Fact]
    public void Execute_FunctionsAndComponentCodeDirective_AddsStatementsToClassLevel()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.AddDirective(ComponentCodeDirective.Directive);
        });

        var source = TestRazorSourceDocument.Create(@"
@functions { var value1 = true; }
@code { var value2 = true; }
@functions { var value3 = true; }");
        var codeDocument = projectEngine.CreateCodeDocument(source, RazorFileKind.Component);
        var processor = CreateCodeDocumentProcessor(projectEngine, codeDocument);

        // Act
        processor.ExecutePass<FunctionsDirectivePass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        Children(
            documentNode,
            node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));

        var @namespace = documentNode.Children[0];
        Children(
            @namespace,
            node => Assert.IsType<ClassDeclarationIntermediateNode>(node));

        var @class = @namespace.Children[0];
        Children(
            @class,
            node => Assert.IsType<MethodDeclarationIntermediateNode>(node),
            node => CSharpCode(" var value1 = true; ", node),
            node => CSharpCode(" var value2 = true; ", node),
            node => CSharpCode(" var value3 = true; ", node));

        var method = @class.Children[0];
        Children(
            method,
            node => Assert.IsType<HtmlContentIntermediateNode>(node));
    }
}
