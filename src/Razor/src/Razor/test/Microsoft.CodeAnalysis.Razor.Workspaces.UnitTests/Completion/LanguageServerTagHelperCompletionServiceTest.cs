// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class LanguageServerTagHelperCompletionServiceTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1452432")]
    public void GetAttributeCompletions_OnlyIndexerNamePrefix()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("FormTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "form")
                .BoundAttributeDescriptor(attribute => attribute
                    .TypeName("System.Collections.Generic.IDictionary<System.String, System.String>")
                    .PropertyName("RouteValues")
                    .AsDictionary("asp-route-", typeof(string).FullName))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["asp-route-..."] = [tagHelpers[0].BoundAttributes[^1]]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: [],
            attributes: [],
            currentTagName: "form");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_BoundDictionaryAttribute_ReturnsPrefixIndexerAndFullSetter()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("FormTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "form")
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("asp-all-route-data")
                    .TypeName("System.Collections.Generic.IDictionary<System.String, System.String>")
                    .PropertyName("RouteValues")
                    .AsDictionary("asp-route-", typeof(string).FullName))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["asp-all-route-data"] = [tagHelpers[0].BoundAttributes[^1]],
            ["asp-route-..."] = [tagHelpers[0].BoundAttributes[^1]]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: [],
            attributes: [],
            currentTagName: "form");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_RequiredBoundDictionaryAttribute_ReturnsPrefixIndexerAndFullSetter()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("FormTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "form", static b => b
                    .RequiredAttribute(name: "asp-route-", nameComparison: RequiredAttributeNameComparison.PrefixMatch))
                .TagMatchingRule(tagName: "form", static b => b
                    .RequiredAttribute(name: "asp-all-route-data"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("asp-all-route-data")
                    .TypeName("System.Collections.Generic.IDictionary<System.String, System.String>")
                    .PropertyName("RouteValues")
                    .AsDictionary("asp-route-", typeof(string).FullName))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["asp-all-route-data"] = [tagHelpers[0].BoundAttributes[^1]],
            ["asp-route-..."] = [tagHelpers[0].BoundAttributes[^1]]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: [],
            attributes: [],
            currentTagName: "form");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_DoesNotReturnCompletionsForAlreadySuppliedAttributes()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "repeat"))
                .BoundAttribute<bool>(name: "visible", propertyName: "Visible")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .BoundAttribute<string>(name: "class", propertyName: "Class")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["onclick"] = [],
            ["visible"] = [tagHelpers[0].BoundAttributes[^1]]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["onclick"],
            attributes: [
                KeyValuePair.Create("class", "something"),
                KeyValuePair.Create("repeat", "4")],
            currentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_ReturnsCompletionForAlreadySuppliedAttribute_IfCurrentAttributeMatches()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "repeat"))
                .BoundAttribute<bool>(name: "visible", propertyName: "Visible")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .BoundAttribute<string>(name: "class", propertyName: "Class")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["onclick"] = [],
            ["visible"] = [tagHelpers[0].BoundAttributes[^1]]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["onclick"],
            attributes: [
                KeyValuePair.Create("class", "something"),
                KeyValuePair.Create("repeat", "4"),
                KeyValuePair.Create("visible", "false")],
            currentTagName: "div",
            currentAttributeName: "visible");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_DoesNotReturnAlreadySuppliedAttribute_IfCurrentAttributeDoesNotMatch()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "repeat"))
                .BoundAttribute<bool>(name: "visible", propertyName: "Visible")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .BoundAttribute<string>(name: "class", propertyName: "Class")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["onclick"] = []
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["onclick"],
            attributes: [
                KeyValuePair.Create("class", "something"),
                KeyValuePair.Create("repeat", "4"),
                KeyValuePair.Create("visible", "false")],
            currentTagName: "div",
            currentAttributeName: "repeat");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_PossibleDescriptorsReturnUnboundRequiredAttributesWithExistingCompletions()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "repeat"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "*", static b => b
                    .RequiredAttribute(name: "class"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
            ["onclick"] = [],
            ["repeat"] = []
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["onclick", "class"],
            currentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_PossibleDescriptorsReturnBoundRequiredAttributesWithExistingCompletions()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "repeat"))
                .BoundAttribute<bool>(name: "repeat", propertyName: "Repeat")
                .BoundAttribute<bool>(name: "visible", propertyName: "Visible")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "*", static b => b
                    .RequiredAttribute(name: "class"))
                .BoundAttribute<string>(name: "class", propertyName: "Class")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [.. tagHelpers[1].BoundAttributes],
            ["onclick"] = [],
            ["repeat"] = [tagHelpers[0].BoundAttributes[0]]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["onclick"],
            currentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_AppliedDescriptorsReturnAllBoundAttributesWithExistingCompletionsForSchemaTags()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute(name: "repeat"))
                .BoundAttribute<bool>(name: "repeat", propertyName: "Repeat")
                .BoundAttribute<bool>(name: "visible", propertyName: "Visible")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "*", static b => b
                    .RequiredAttribute(name: "class"))
                .BoundAttribute<string>(name: "class", propertyName: "Class")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .BoundAttribute<bool>(name: "visible", propertyName: "Visible")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["onclick"] = [],
            ["class"] = [.. tagHelpers[1].BoundAttributes],
            ["repeat"] = [tagHelpers[0].BoundAttributes[0]],
            ["visible"] = [tagHelpers[0].BoundAttributes[^1], tagHelpers[2].BoundAttributes[0]]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["class", "onclick"],
            currentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_AppliedTagOutputHintDescriptorsReturnBoundAttributesWithExistingCompletionsForNonSchemaTags()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("CustomTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "custom")
                .BoundAttribute<bool>(name: "repeat", propertyName: "Repeat")
                .TagOutputHint("div")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
            ["repeat"] = [.. tagHelpers[0].BoundAttributes]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["class"],
            currentTagName: "custom");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_AppliedDescriptorsReturnBoundAttributesCompletionsForNonSchemaTags()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("CustomTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "custom")
                .BoundAttribute<bool>(name: "repeat", propertyName: "Repeat")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["repeat"] = [.. tagHelpers[0].BoundAttributes]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["class"],
            currentTagName: "custom");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_AppliedDescriptorsReturnBoundAttributesWithExistingCompletionsForSchemaTags()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .BoundAttribute<bool>(name: "repeat", propertyName: "Repeat")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
            ["repeat"] = [.. tagHelpers[0].BoundAttributes]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["class"],
            currentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_NoDescriptorsReturnsExistingCompletions()
    {
        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = []
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers: [],
            existingCompletions: ["class"],
            currentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_NoDescriptorsForUnprefixedTagReturnsExistingCompletions()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div", static b => b
                    .RequiredAttribute("special"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = []
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["class"],
            currentTagName: "div",
            tagHelperPrefix: "th:");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_NoDescriptorsForTagReturnsExistingCompletions()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("MyTableTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "table", static b => b
                    .RequiredAttribute("special"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = []
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: ["class"],
            currentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetAttributeCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_IgnoresDirectiveAttributes()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BindAttribute", "TestAssembly")
                .TagMatchingRule(tagName: "input")
                .BoundAttributeDescriptor(builder =>
                {
                    builder.Name = "@bind";
                    builder.IsDirectiveAttribute = true;
                })
                .TagOutputHint("table")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create([]);

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["table"],
            containingTagName: "body",
            containingParentTagName: null);

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_FiltersFullyQualifiedElementsIfShortNameExists()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "Test")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "TestAssembly.Test")
                .IsFullyQualifiedNameMatch(true)
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("Test2TagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "Test2Assembly.Test")
                .IsFullyQualifiedNameMatch(true)
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["Test"] = [tagHelpers[0]],
            ["Test2Assembly.Test"] = [tagHelpers[2]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "body",
            containingParentTagName: null);

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_TagOutputHintDoesNotFallThroughToSchemaCheck()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("MyTableTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "my-table")
                .TagOutputHint("table")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("MyTrTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "my-tr")
                .TagOutputHint("tr")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["my-table"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["table", "div"],
            containingTagName: "body",
            containingParentTagName: null);

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CatchAllsOnlyApplyToCompletionsStartingWithPrefix()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "li")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["th:li"] = [tagHelpers[1], tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul",
            tagHelperPrefix: "th:");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_TagHelperPrefixIsPrependedToTagHelperCompletions()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("SuperLiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "superli")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "li")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["th:superli"] = [tagHelpers[0]],
            ["th:li"] = [tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul",
            tagHelperPrefix: "th:");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_IsCaseSensitive()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("MyliTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "myli")
                .SetCaseSensitive()
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("MYLITagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "MYLI")
                .SetCaseSensitive()
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["myli"] = [tagHelpers[0]],
            ["MYLI"] = [tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_HTMLSchemaTagName_IsCaseSensitive()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("LITagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "LI")
                .SetCaseSensitive()
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "li")
                .SetCaseSensitive()
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["LI"] = [tagHelpers[0]],
            ["li"] = [tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CatchAllsApplyToOnlyTagHelperCompletions()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("SuperLiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "superli")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAll", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .Build()
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["superli"] = [tagHelpers[0], tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CatchAllsApplyToNonTagHelperCompletionsIfStartsWithTagHelperPrefix()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("SuperLiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "superli")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAll", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["th:superli"] = [tagHelpers[0], tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["th:li"],
            containingTagName: "ul",
            tagHelperPrefix: "th:");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_AllowsMultiTargetingTagHelpers()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldTagHelper1", "TestAssembly")
                .TagMatchingRule(tagName: "strong")
                .TagMatchingRule(tagName: "b")
                .TagMatchingRule(tagName: "bold")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("BoldTagHelper2", "TestAssembly")
                .TagMatchingRule(tagName: "strong")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["strong"] = [tagHelpers[0], tagHelpers[1]],
            ["b"] = [tagHelpers[0]],
            ["bold"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["strong", "b", "bold"],
            containingTagName: "ul");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CombinesDescriptorsOnExistingCompletions()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper1", "TestAssembly")
                .TagMatchingRule(tagName: "li")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper2", "TestAssembly")
                .TagMatchingRule(tagName: "li")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["li"] = [tagHelpers[0], tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_NewCompletionsForSchemaTagsNotInExistingCompletionsAreIgnored()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("SuperLiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "superli")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "li")
                .TagOutputHint("strong")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["li"] = [tagHelpers[1]],
            ["superli"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_OutputHintIsCrossReferencedWithExistingCompletions()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .TagOutputHint("li")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "li")
                .TagOutputHint("strong")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["div"] = [tagHelpers[0]],
            ["li"] = [tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_EnsuresDescriptorsHaveSatisfiedParent()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper1", "TestAssembly")
                .TagMatchingRule(tagName: "li")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper2", "TestAssembly")
                .TagMatchingRule(tagName: "li", parentTagName: "ol")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["li"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["li"],
            containingTagName: "ul");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_NoContainingParentTag_DoesNotGetCompletionForRuleWithParentTag()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("Tag1", "TestAssembly")
                .TagMatchingRule(tagName: "outer-child-tag")
                .TagMatchingRule(tagName: "child-tag", parentTagName: "parent-tag")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("Tag2", "TestAssembly")
                .TagMatchingRule(tagName: "parent-tag")
                .AllowChildTag("child-tag")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["outer-child-tag"] = [tagHelpers[0]],
            ["parent-tag"] = [tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: null,
            containingParentTagName: null);

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_WithContainingParentTag_GetsCompletionForRuleWithParentTag()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("Tag1", "TestAssembly")
                .TagMatchingRule(tagName: "outer-child-tag")
                .TagMatchingRule(tagName: "child-tag", parentTagName: "parent-tag")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("Tag2", "TestAssembly")
                .TagMatchingRule(tagName: "parent-tag")
                .AllowChildTag("child-tag")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["child-tag"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "child-tag",
            containingParentTagName: "parent-tag");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_AllowedChildrenAreIgnoredWhenAtRoot()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAll", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .AllowChildTag("div")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create([]);

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: null,
            containingParentTagName: null);

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_DoesNotReturnExistingCompletionsWhenAllowedChildren()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParent", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .AllowChildTag("div")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["b"] = [],
            ["bold"] = [],
            ["div"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["p", "em"],
            containingTagName: "thing",
            containingParentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_NoneTagHelpers()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParent", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["b"] = [],
            ["bold"] = []
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "",
            containingParentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_SomeTagHelpers()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParent", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .AllowChildTag("div")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["b"] = [],
            ["bold"] = [],
            ["div"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "",
            containingParentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_AllTagHelpers()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParentCatchAll", "TestAssembly")
                .TagMatchingRule(tagName: "*")
                .AllowChildTag("strong")
                .AllowChildTag("div")
                .AllowChildTag("b")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParent", "TestAssembly")
                .TagMatchingRule(tagName: "div")
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["strong"] = [tagHelpers[0]],
            ["b"] = [tagHelpers[0]],
            ["bold"] = [tagHelpers[0]],
            ["div"] = [tagHelpers[0], tagHelpers[1]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "",
            containingParentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_MustSatisfyAttributeRules_WithAttributes()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("FormTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "form", static b => b
                    .RequiredAttribute(name: "asp-route-", nameComparison: RequiredAttributeNameComparison.PrefixMatch))
                .Build()
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["form"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["form"],
            containingTagName: "",
            containingParentTagName: "div",
            attributes: [KeyValuePair.Create("asp-route-id", "123")]);

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_MustSatisfyAttributeRules_NoAttributes()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("FormTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "form", static b => b
                    .RequiredAttribute(name: "asp-route-", nameComparison: RequiredAttributeNameComparison.PrefixMatch))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create([]);

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: ["form"],
            containingTagName: "",
            containingParentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_MustSatisfyAttributeRules_NoAttributes_AllowedIfNotHtml()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("ComponentTagHelper", "TestAssembly")
                .TagMatchingRule(tagName: "component", static b => b
                    .RequiredAttribute(name: "type", nameComparison: RequiredAttributeNameComparison.PrefixMatch))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["component"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "",
            containingParentTagName: "div");

        var service = CreateTagHelperCompletionFactsService();

        var completions = service.GetElementCompletions(completionContext);

        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    private static TagHelperCompletionService CreateTagHelperCompletionFactsService() => new();

    private static void AssertCompletionsAreEquivalent(ElementCompletionResult expected, ElementCompletionResult actual)
    {
        Assert.Equal(expected.Completions.Count, actual.Completions.Count);

        foreach (var (key, value) in expected.Completions)
        {
            var actualValue = actual.Completions[key];
            Assert.NotNull(actualValue);
            Assert.Equal(value, actualValue);
        }
    }

    private static void AssertCompletionsAreEquivalent(AttributeCompletionResult expected, AttributeCompletionResult actual)
    {
        Assert.Equal(expected.Completions.Count, actual.Completions.Count);

        foreach (var expectedCompletion in expected.Completions)
        {
            var actualValue = actual.Completions[expectedCompletion.Key];
            Assert.NotNull(actualValue);
            Assert.Equal(expectedCompletion.Value, actualValue);
        }
    }

    private static ElementCompletionContext BuildElementCompletionContext(
        TagHelperCollection tagHelpers,
        ImmutableArray<string> existingCompletions,
        string? containingTagName,
        string? containingParentTagName = "body",
        bool containingParentIsTagHelper = false,
        string? tagHelperPrefix = null,
        ImmutableArray<KeyValuePair<string, string>> attributes = default)
    {
        attributes = attributes.NullToEmpty();

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelperPrefix, tagHelpers);
        var completionContext = new ElementCompletionContext(
            documentContext,
            existingCompletions,
            containingTagName,
            attributes,
            containingParentTagName,
            containingParentIsTagHelper,
            inHTMLSchema: static tag => tag is "strong" or "b" or "bold" or "li" or "div" or "form");

        return completionContext;
    }

    private static AttributeCompletionContext BuildAttributeCompletionContext(
        TagHelperCollection tagHelpers,
        ImmutableArray<string> existingCompletions,
        string currentTagName,
        string? currentAttributeName = null,
        ImmutableArray<KeyValuePair<string, string>> attributes = default,
        string tagHelperPrefix = "")
    {
        attributes = attributes.NullToEmpty();

        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelperPrefix, tagHelpers);
        var completionContext = new AttributeCompletionContext(
            documentContext,
            existingCompletions,
            currentTagName,
            currentAttributeName,
            attributes,
            currentParentTagName: "body",
            currentParentIsTagHelper: false,
            inHTMLSchema: static tag => tag is "strong" or "b" or "bold" or "li" or "div" or "form");

        return completionContext;
    }
}
