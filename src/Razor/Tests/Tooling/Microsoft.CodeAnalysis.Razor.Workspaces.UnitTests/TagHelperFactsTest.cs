// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Editor.Razor;

public class TagHelperFactsTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void GetTagHelperBinding_DoesNotAllowOptOutCharacterPrefix()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var binding = TagHelperFacts.GetTagHelperBinding(
            documentContext,
            tagName: "!a",
            attributes: [],
            parentTag: null,
            parentIsTagHelper: false);

        Assert.Null(binding);
    }

    [Fact]
    public void GetTagHelperBinding_WorksAsExpected()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "a", static b => b
                    .RequiredAttribute(name: "asp-for"))
                .BoundAttribute<string>(name: "asp-for", propertyName: "AspFor")
                .BoundAttribute(name: "asp-route", propertyName: "AspRoute", typeName: typeof(IDictionary<,>).Namespace + "IDictionary<string, string>", static b => b
                    .AsDictionaryAttribute<string>("asp-route-"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("asp-for")
                    .TypeName(typeof(string).FullName)
                    .PropertyName("AspFor"))
                .Build(),
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var binding = TagHelperFacts.GetTagHelperBinding(
            documentContext,
            tagName: "a",
            attributes: [KeyValuePair.Create("asp-for", "Name")],
            parentTag: "p",
            parentIsTagHelper: false);

        Assert.NotNull(binding);
        var tagHelper = Assert.Single(binding.TagHelpers);
        Assert.Same(tagHelpers[0], tagHelper);
        var boundRule = Assert.Single(binding.GetBoundRules(tagHelper));
        Assert.Same(tagHelpers[0].TagMatchingRules[0], boundRule);
    }

    [Fact]
    public void GetBoundTagHelperAttributes_MatchesPrefixedAttributeName()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "a")
                .BoundAttribute<string>(name: "asp-for", propertyName: "AspFor")
                .BoundAttribute(name: "asp-route", propertyName: "AspRoute", typeName: typeof(IDictionary<,>).Namespace + "IDictionary<string, string>", static b => b
                    .AsDictionaryAttribute<string>("asp-route-"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);
        var binding = TagHelperFacts.GetTagHelperBinding(
            documentContext,
            tagName: "a",
            attributes: [],
            parentTag: null,
            parentIsTagHelper: false);

        Assert.NotNull(binding);

        var result = TagHelperFacts.GetBoundTagHelperAttributes(
            documentContext,
            attributeName: "asp-route-something",
            binding);

        Assert.Same(tagHelpers[0].BoundAttributes[^1], Assert.Single(result));
    }

    [Fact]
    public void GetBoundTagHelperAttributes_MatchesAttributeName()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "input")
                .BoundAttribute<string>(name: "asp-for", propertyName: "AspFor")
                .BoundAttribute<string>(name: "asp-extra", propertyName: "AspExtra")
                .Build()
        ];

        var expectedBoundAttributes = new[]
        {
            tagHelpers[0].BoundAttributes.First()
        };

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var binding = TagHelperFacts.GetTagHelperBinding(
            documentContext,
            tagName: "input",
            attributes: [],
            parentTag: null,
            parentIsTagHelper: false);

        Assert.NotNull(binding);

        var result = TagHelperFacts.GetBoundTagHelperAttributes(
            documentContext,
            attributeName: "asp-for",
            binding);

        Assert.Equal(expectedBoundAttributes, result);
    }

    [Fact]
    public void GetTagHelpersGivenTag_DoesNotAllowOptOutCharacterPrefix()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenTag(
            documentContext,
            tagName: "!strong",
            parentTag: null);

        Assert.Empty(result);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RequiresTagName()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "strong")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenTag(
            documentContext,
            tagName: "strong",
            parentTag: "p");

        Assert.Equal(tagHelpers, result);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnTagName()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "a", parentTagName: "div")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("TestType2", "TestAssembly")
                .TagMatchingRule(tagName: "strong", parentTagName: "div")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenTag(
            documentContext,
            tagName: "a",
            parentTag: "div");

        Assert.Same(tagHelpers[0], Assert.Single(result));
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnTagHelperPrefix()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "strong")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("TestType2", "TestAssembly")
                .TagMatchingRule(tagName: "thstrong")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(prefix: "th", tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenTag(
            documentContext,
            tagName: "thstrong",
            parentTag: "div");

        Assert.Same(tagHelpers[0], Assert.Single(result));
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnParent()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "strong", parentTagName: "div")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("TestType2", "TestAssembly")
                .TagMatchingRule(tagName: "strong", parentTagName: "p")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenTag(
            documentContext,
            tagName: "strong",
            parentTag: "div");

        Assert.Same(tagHelpers[0], Assert.Single(result));
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsRootParentTag()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenParent(
            documentContext,
            parentTag: null /* root */);

        Assert.Equal(tagHelpers, result);
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsRootParentTagForParentRestrictedTagHelperDescriptors()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "p", parentTagName: "body")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenParent(
            documentContext,
            parentTag: null /* root */);

        Assert.Same(tagHelpers[0], Assert.Single(result));
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsUnspecifiedParentTagHelpers()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenParent(
            documentContext,
            parentTag: "p");

        Assert.Equal(tagHelpers, result);
    }

    [Fact]
    public void GetTagHelpersGivenParent_RestrictsTagHelpersBasedOnParent()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRule(tagName: "p", parentTagName: "div")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("TestType2", "TestAssembly")
                .TagMatchingRule(tagName: "strong", parentTagName: "p")
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers);

        var result = TagHelperFacts.GetTagHelpersGivenParent(
            documentContext,
            parentTag: "div");

        Assert.Same(tagHelpers[0], Assert.Single(result));
    }
}
