// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class InheritsDirectivePassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    [Fact]
    public void Execute_SkipsDocumentWithNoClassNode()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@inherits Hello<World[]>");

        var documentNode = new DocumentIntermediateNode();
        documentNode.Children.Add(new DirectiveIntermediateNode() { Directive = FunctionsDirective.Directive });

        // Act
        ProjectEngine.ExecutePass<InheritsDirectivePass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            node => Assert.IsType<DirectiveIntermediateNode>(node));
    }

    [Fact]
    public void Execute_Inherits_SetsClassDeclarationBaseType()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("@inherits Hello<World[]>");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        processor.ExecutePhasesThrough<IRazorDocumentClassifierPhase>();

        // Act
        processor.ExecutePass<InheritsDirectivePass>();


        // Assert
        var documentNode = processor.GetDocumentNode();

        Children(
            documentNode,
            node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));

        var @namespace = documentNode.Children[0];
        Children(
            @namespace,
            node => Assert.IsType<ClassDeclarationIntermediateNode>(node));

        var @class = (ClassDeclarationIntermediateNode)@namespace.Children[0];
        Assert.Equal("Hello<World[]>", @class.BaseType?.BaseType.Content);
    }
}
