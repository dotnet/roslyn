// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public class InjectDirectiveTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_2_1;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        // Notice we're not registering the InjectDirective.Pass here so we can run it on demand.
        builder.AddDirective(InjectDirective.Directive);
        builder.AddDirective(ModelDirective.Directive);

        builder.Features.Add(new RazorPageDocumentClassifierPass());
        builder.Features.Add(new MvcViewDocumentClassifierPass());
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorDocumentClassifierPhase>();
    }

    [Fact]
    public void InjectDirectivePass_Execute_DefinesProperty()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@inject PropertyType PropertyName
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<InjectDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(2, classNode.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(classNode.Children[1]);
        Assert.Equal("PropertyType", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    [Fact]
    public void InjectDirectivePass_Execute_DedupesPropertiesByName()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@inject PropertyType PropertyName
@inject PropertyType2 PropertyName
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<InjectDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(2, classNode.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(classNode.Children[1]);
        Assert.Equal("PropertyType2", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    [Fact]
    public void InjectDirectivePass_Execute_ExpandsTModel_WithDynamic()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@inject PropertyType<TModel> PropertyName
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<InjectDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(2, classNode.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(classNode.Children[1]);
        Assert.Equal("PropertyType<dynamic>", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    [Fact]
    public void InjectDirectivePass_Execute_ExpandsTModel_WithModelTypeFirst()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@model ModelType
@inject PropertyType<TModel> PropertyName
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<InjectDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(2, classNode.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(classNode.Children[1]);
        Assert.Equal("PropertyType<ModelType>", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }

    [Fact]
    public void InjectDirectivePass_Execute_ExpandsTModel_WithModelType()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@inject PropertyType<TModel> PropertyName
@model ModelType
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<InjectDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(2, classNode.Children.Count);

        var node = Assert.IsType<InjectIntermediateNode>(classNode.Children[1]);
        Assert.Equal("PropertyType<ModelType>", node.TypeName);
        Assert.Equal("PropertyName", node.MemberName);
    }
}
