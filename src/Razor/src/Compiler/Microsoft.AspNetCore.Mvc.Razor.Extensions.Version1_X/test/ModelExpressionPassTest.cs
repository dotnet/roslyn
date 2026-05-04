// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public class ModelExpressionPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_1_1;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new RazorPageDocumentClassifierPass());
        builder.Features.Add(new MvcViewDocumentClassifierPass());
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorDocumentClassifierPhase>();
    }

    [Fact]
    public void ModelExpressionPass_NonModelExpressionProperty_Ignored()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Int32"))
            .TagMatchingRuleDescriptor(rule =>
                rule.RequireTagName("p"))
            .Build();

        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<p foo=""17"">",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ModelExpressionPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var tagHelperNode = documentNode.GetTagHelperNode();
        var setProperty = tagHelperNode.Children.OfType<TagHelperPropertyIntermediateNode>().Single();

        var token = Assert.IsAssignableFrom<CSharpIntermediateToken>(Assert.Single(setProperty.Children));
        Assert.Equal("17", token.Content);
    }

    [Fact]
    public void ModelExpressionPass_ModelExpressionProperty_SimpleExpression()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression"))
            .TagMatchingRuleDescriptor(rule =>
                rule.RequireTagName("p"))
            .Build();

        // Using \r\n here because we verify line mappings
        var codeDocument = ProjectEngine.CreateCodeDocument(
            "@addTagHelper TestTagHelper, TestAssembly\r\n<p foo=\"Bar\">",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ModelExpressionPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var tagHelperNode = documentNode.GetTagHelperNode();
        var setProperty = tagHelperNode.Children.OfType<TagHelperPropertyIntermediateNode>().Single();

        var expression = Assert.IsType<CSharpExpressionIntermediateNode>(Assert.Single(setProperty.Children));
        Assert.Equal("ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.Bar)", expression.GetCSharpContent());

        var originalNode = Assert.IsAssignableFrom<CSharpIntermediateToken>(expression.Children[2]);
        Assert.Equal("Bar", originalNode.Content);
        var source = Assert.NotNull(originalNode.Source);
        Assert.Equal(new SourceSpan("test.cshtml", 51, 1, 8, 3), source);
    }

    [Fact]
    public void ModelExpressionPass_ModelExpressionProperty_ComplexExpression()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression"))
            .TagMatchingRuleDescriptor(rule =>
                rule.RequireTagName("p"))
            .Build();

        // Using \r\n here because we verify line mappings
        var codeDocument = ProjectEngine.CreateCodeDocument(
            "@addTagHelper TestTagHelper, TestAssembly\r\n<p foo=\"@Bar\">",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ModelExpressionPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var tagHelperNode = documentNode.GetTagHelperNode();
        var setProperty = tagHelperNode.Children.OfType<TagHelperPropertyIntermediateNode>().Single();

        var expression = Assert.IsType<CSharpExpressionIntermediateNode>(Assert.Single(setProperty.Children));
        Assert.Equal("ModelExpressionProvider.CreateModelExpression(ViewData, __model => Bar)", expression.GetCSharpContent());

        var originalNode = Assert.IsAssignableFrom<CSharpIntermediateToken>(expression.Children[1]);
        Assert.Equal("Bar", originalNode.Content);
        var source = Assert.NotNull(originalNode.Source);
        Assert.Equal(new SourceSpan("test.cshtml", 52, 1, 9, 3), source);
    }
}
