// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public class ViewComponentTagHelperPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_1_1;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new MvcViewDocumentClassifierPass());
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorDocumentClassifierPhase>();

        // We also expect the default tag helper pass to run first.
        processor.ExecutePass<DefaultTagHelperOptimizationPass>();
    }

    [Fact]
    public void ViewComponentTagHelperPass_Execute_IgnoresRegularTagHelper()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Int32"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build();

        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<p foo=""17"">",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ViewComponentTagHelperPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal(3, classNode.Children.Count); // No class node created for a VCTH

        foreach (var child in classNode.Children)
        {
            Assert.IsNotType<ViewComponentTagHelperIntermediateNode>(child);
        }
    }

    [Fact]
    public void ViewComponentTagHelperPass_Execute_CreatesViewComponentTagHelper()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateViewComponent("TestTagHelper", "TestAssembly")
            .TypeName("__Generated__TagCloudViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("TagCloud", TypeNameObject.From("TagCloud")))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Int32")
                .PropertyName("Foo"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("tagcloud"))
            .Build();

        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<tagcloud foo=""17"">",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ViewComponentTagHelperPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var tagHelperNode = documentNode.GetTagHelperNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("AspNetCore.test.__Generated__TagCloudViewComponentTagHelper", Assert.IsType<DefaultTagHelperCreateIntermediateNode>(tagHelperNode.Children[1]).TypeName);
        Assert.Equal("Foo", Assert.IsType<DefaultTagHelperPropertyIntermediateNode>(tagHelperNode.Children[2]).PropertyName);

        Assert.Equal(4, classNode.Children.Count);
        Assert.IsType<ViewComponentTagHelperIntermediateNode>(classNode.Children.Last());
    }

    [Fact]
    public void ViewComponentTagHelperPass_Execute_CreatesViewComponentTagHelper_WithIndexer()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateViewComponent("TestTagHelper", "TestAssembly")
            .TypeName("__Generated__TagCloudViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("TagCloud", TypeNameObject.From("TagCloud")))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                .PropertyName("Tags")
                .AsDictionaryAttribute("foo-", "System.Int32"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("tagcloud"))
            .Build();

        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<tagcloud tag-foo=""17"">",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ViewComponentTagHelperPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var tagHelperNode = documentNode.GetTagHelperNode();

        Assert.Equal("AspNetCore.test.__Generated__TagCloudViewComponentTagHelper", Assert.IsType<DefaultTagHelperCreateIntermediateNode>(tagHelperNode.Children[1]).TypeName);
        Assert.IsType<DefaultTagHelperHtmlAttributeIntermediateNode>(tagHelperNode.Children[2]);

        Assert.Equal(4, classNode.Children.Count);
        Assert.IsType<ViewComponentTagHelperIntermediateNode>(classNode.Children[3]);
    }

    [Fact]
    public void ViewComponentTagHelperPass_Execute_CreatesViewComponentTagHelper_Nested()
    {
        // Arrange
        var tagHelper1 = TagHelperDescriptorBuilder.CreateTagHelper("PTestTagHelper", "TestAssembly")
            .TypeName("PTestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .PropertyName("Foo")
                .Name("Foo")
                .TypeName("System.Int32"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build();

        var tagHelper2 = TagHelperDescriptorBuilder.CreateViewComponent("TestTagHelper", "TestAssembly")
            .TypeName("__Generated__TagCloudViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("TagCloud", TypeNameObject.From("TagCloud")))
            .BoundAttributeDescriptor(attribute => attribute
                .PropertyName("Foo")
                .Name("Foo")
                .TypeName("System.Int32"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("tagcloud"))
            .Build();

        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@addTagHelper *, TestAssembly
<p foo=""17""><tagcloud foo=""17""></p>",
            [tagHelper1, tagHelper2]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ViewComponentTagHelperPass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var outerTagHelper = documentNode.GetTagHelperNode();
        var viewComponentTagHelper = outerTagHelper.Children[0].GetTagHelperNode();
        var classNode = documentNode.GetClassNode();

        Assert.Equal("PTestTagHelper", Assert.IsType<DefaultTagHelperCreateIntermediateNode>(outerTagHelper.Children[1]).TypeName);
        Assert.Equal("Foo", Assert.IsType<DefaultTagHelperPropertyIntermediateNode>(outerTagHelper.Children[2]).PropertyName);

        Assert.Equal(
            "AspNetCore.test.__Generated__TagCloudViewComponentTagHelper",
            Assert.IsType<DefaultTagHelperCreateIntermediateNode>(viewComponentTagHelper.Children[1]).TypeName);
        Assert.Equal("Foo", Assert.IsType<DefaultTagHelperPropertyIntermediateNode>(viewComponentTagHelper.Children[2]).PropertyName);

        Assert.Equal(5, classNode.Children.Count);
        Assert.IsType<ViewComponentTagHelperIntermediateNode>(classNode.Children.Last());
    }
}
