// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

// This is just a basic integration test. There are detailed tests for the VCTH visitor and descriptor factory.
public class ViewComponentTagHelperProducerTest
{
    [Fact]
    public void DescriptorProvider_FindsVCTH()
    {
        // Arrange
        var code = @"
        public class StringParameterViewComponent
        {
            public string Invoke(string foo, string bar) => null;
        }
";

        var compilation = MvcShim.BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

        var projectEngine = RazorProjectEngine.CreateEmpty(static b =>
        {
            b.Features.Add(new ViewComponentTagHelperProducer.Factory());
            b.Features.Add(new TagHelperDiscoveryService());
        });

        Assert.True(projectEngine.Engine.TryGetFeature(out ITagHelperDiscoveryService? service));

        var expectedDescriptor = TagHelperDescriptorBuilder.CreateViewComponent("__Generated__StringParameterViewComponentTagHelper", TestCompilation.AssemblyName)
            .TypeName("__Generated__StringParameterViewComponentTagHelper")
            .Metadata(new ViewComponentMetadata("StringParameter", TypeNameObject.From("StringParameter")))
            .DisplayName("StringParameterViewComponentTagHelper")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("vc:string-parameter")
                .RequireAttributeDescriptor(attribute => attribute.Name("foo"))
                .RequireAttributeDescriptor(attribute => attribute.Name("bar")))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("foo")
                .PropertyName("foo")
                .TypeName(typeof(string).FullName)
                .DisplayName("string StringParameterViewComponentTagHelper.foo"))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("bar")
                .PropertyName("bar")
                .TypeName(typeof(string).FullName)
                .DisplayName("string StringParameterViewComponentTagHelper.bar"))
            .Build();

        // Act
        var result = service.GetTagHelpers(compilation);

        // Assert
        Assert.Single(result, d => d.Equals(expectedDescriptor));
    }
}
