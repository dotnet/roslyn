// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class DefaultTagHelperOptimizationPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorDirectiveClassifierPhase>();
    }

    [Fact]
    public void DefaultTagHelperOptimizationPass_Execute_ReplacesChildren()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Int32")
                .PropertyName("FooProp"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build();

        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<p foo=""17"" attr=""value"">",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<DefaultTagHelperOptimizationPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();

        var @class = documentNode.FindPrimaryClass();
        Assert.NotNull(@class);

        Assert.IsType<DefaultTagHelperRuntimeIntermediateNode>(@class.Children[0]);

        var fieldDeclaration = Assert.IsType<FieldDeclarationIntermediateNode>(@class.Children[1]);
        Assert.True(fieldDeclaration.IsTagHelperField);
        Assert.Equal("__TestTagHelper", fieldDeclaration.Name);
        Assert.Equal("global::TestTagHelper", fieldDeclaration.Type);
        Assert.Equal("private", fieldDeclaration.Modifiers[0]);

        var tagHelperNode = documentNode.GetTagHelperNode();
        Assert.Equal(5, tagHelperNode.Children.Count);

        var body = Assert.IsType<DefaultTagHelperBodyIntermediateNode>(tagHelperNode.Children[0]);
        Assert.Equal("p", body.TagName);
        Assert.Equal(TagMode.StartTagAndEndTag, body.TagMode);

        var create = Assert.IsType<DefaultTagHelperCreateIntermediateNode>(tagHelperNode.Children[1]);
        Assert.Equal("__TestTagHelper", create.FieldName);
        Assert.Equal("TestTagHelper", create.TypeName);
        Assert.Equal(tagHelper, create.TagHelper);

        var property = Assert.IsType<DefaultTagHelperPropertyIntermediateNode>(tagHelperNode.Children[2]);
        Assert.Equal("foo", property.AttributeName);
        Assert.Equal(AttributeStructure.DoubleQuotes, property.AttributeStructure);
        Assert.Equal(tagHelper.BoundAttributes[0], property.BoundAttribute);
        Assert.Equal("__TestTagHelper", property.FieldName);
        Assert.False(property.IsIndexerNameMatch);
        Assert.Equal("FooProp", property.PropertyName);
        Assert.Equal(tagHelper, property.TagHelper);

        var htmlAttribute = Assert.IsType<DefaultTagHelperHtmlAttributeIntermediateNode>(tagHelperNode.Children[3]);
        Assert.Equal("attr", htmlAttribute.AttributeName);
        Assert.Equal(AttributeStructure.DoubleQuotes, htmlAttribute.AttributeStructure);

        Assert.IsType<DefaultTagHelperExecuteIntermediateNode>(tagHelperNode.Children[4]);
    }
}
