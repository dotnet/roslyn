// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperBinderTest
{
    [Fact]
    public void GetBinding_ReturnsBindingWithInformation()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        TagHelperCollection expectedTagHelpers = [divTagHelper];
        var expectedAttributes = ImmutableArray.Create(
            new KeyValuePair<string, string>("class", "something"));
        var binder = new TagHelperBinder("th:", expectedTagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "th:div",
            attributes: expectedAttributes,
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.Equal(expectedTagHelpers, binding.TagHelpers);
        Assert.Equal("th:div", binding.TagName);
        Assert.Equal("body", binding.ParentTagName);
        Assert.Equal<KeyValuePair<string, string>>(expectedAttributes, binding.Attributes);
        Assert.Equal("th:", binding.TagNamePrefix);
        Assert.Equal<TagMatchingRuleDescriptor>(divTagHelper.TagMatchingRules, binding.GetBoundRules(divTagHelper));
    }

    [Fact]
    public void GetBinding_With_Multiple_TagNameRules_SingleHelper()
    {
        // Arrange
        var multiTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("MultiTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("a"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("img"))
            .Build();
        TagHelperCollection expectedTagHelpers = [multiTagHelper];
        var binder = new TagHelperBinder("", expectedTagHelpers);

        TestTagName("div", multiTagHelper.TagMatchingRules[0]);
        TestTagName("a", multiTagHelper.TagMatchingRules[1]);
        TestTagName("img", multiTagHelper.TagMatchingRules[2]);
        TestTagName("p", null);
        TestTagName("*", null);

        void TestTagName(string tagName, TagMatchingRuleDescriptor? expectedBindingResult)
        {
            // Act
            var binding = binder.GetBinding(
                tagName: tagName,
                attributes: [],
                parentTagName: "body",
                parentIsTagHelper: false);

            // Assert
            if (expectedBindingResult == null)
            {
                Assert.Null(binding);
                return;
            }
            else
            {
                Assert.NotNull(binding);
                Assert.Equal(expectedTagHelpers, binding.TagHelpers);

                Assert.Equal(tagName, binding.TagName);
                var mapping = Assert.Single(binding.GetBoundRules(multiTagHelper));
                Assert.Equal(expectedBindingResult, mapping);
            }
        }
    }

    [Fact]
    public void GetBinding_With_Multiple_TagNameRules_MultipleHelpers()
    {
        // Arrange
        var multiTagHelper1 = TagHelperDescriptorBuilder.CreateTagHelper("MultiTagHelper1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("a"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("img"))
            .Build();

        var multiTagHelper2 = TagHelperDescriptorBuilder.CreateTagHelper("MultiTagHelper2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("table"))
            .Build();

        var binder = new TagHelperBinder("", [multiTagHelper1, multiTagHelper2]);

        TestTagName("div", [multiTagHelper1, multiTagHelper2], [multiTagHelper1.TagMatchingRules[0], multiTagHelper2.TagMatchingRules[0]]);
        TestTagName("a", [multiTagHelper1], [multiTagHelper1.TagMatchingRules[1]]);
        TestTagName("img", [multiTagHelper1], [multiTagHelper1.TagMatchingRules[2]]);
        TestTagName("p", [multiTagHelper2], [multiTagHelper2.TagMatchingRules[1]]);
        TestTagName("table", [multiTagHelper2], [multiTagHelper2.TagMatchingRules[2]]);
        TestTagName("*", null, null);

        void TestTagName(string tagName, TagHelperCollection? expectedTagHelpers, TagMatchingRuleDescriptor[]? expectedBindingResults)
        {
            // Act
            var binding = binder.GetBinding(
                tagName: tagName,
                attributes: [],
                parentTagName: "body",
                parentIsTagHelper: false);

            // Assert
            if (expectedTagHelpers is null)
            {
                Assert.Null(binding);
            }
            else
            {
                Assert.NotNull(binding);
                Assert.Equal(expectedTagHelpers, binding.TagHelpers);
                Assert.NotNull(expectedBindingResults);

                Assert.Equal(tagName, binding.TagName);

                for (var i = 0; i < expectedTagHelpers.Count; i++)
                {
                    var mapping = Assert.Single(binding.GetBoundRules(expectedTagHelpers[i]));
                    Assert.Equal(expectedBindingResults[i], mapping);
                }
            }
        }
    }

    public static TheoryData<string, string, TagHelperCollection, TagHelperCollection> RequiredParentData
    {
        get
        {
            var strongPDivParent = TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireParentTag("p"))
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireParentTag("div"))
                .Build();
            var catchAllPParent = TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("*")
                    .RequireParentTag("p"))
                .Build();

            // tagName, parentTagName, availableTagHelpers, expectedTagHelpers
            return new()
            {
                {
                    "strong",
                    "p",
                    [strongPDivParent],
                    [strongPDivParent]
                },
                {
                    "strong",
                    "div",
                    [strongPDivParent, catchAllPParent],
                    [strongPDivParent]
                },
                {
                    "strong",
                    "p",
                    [strongPDivParent, catchAllPParent],
                    [strongPDivParent, catchAllPParent]
                },
                {
                    "custom",
                    "p",
                    [strongPDivParent, catchAllPParent],
                    [catchAllPParent]
                }
            };
        }
    }

    [Theory]
    [MemberData(nameof(RequiredParentData))]
    public void GetBinding_ReturnsBindingResultWithDescriptorsParentTags(
        string tagName,
        string parentTagName,
        TagHelperCollection availableTagHelpers,
        TagHelperCollection expectedTagHelpers)
    {
        // Arrange
        var binder = new TagHelperBinder(null, availableTagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName,
            attributes: [],
            parentTagName: parentTagName,
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.Equal(expectedTagHelpers, binding.TagHelpers);
    }

    public static TheoryData<string, ImmutableArray<KeyValuePair<string, string>>, TagHelperCollection, TagHelperCollection?> RequiredAttributeData
    {
        get
        {
            var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("style")))
                .Build();
            var inputDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("input")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class"))
                    .RequireAttributeDescriptor(attribute => attribute.Name("style")))
                .Build();
            var inputWildcardPrefixDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("InputWildCardAttribute", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("input")
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("nodashprefix", RequiredAttributeNameComparison.PrefixMatch)))
                .Build();
            var catchAllDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .Build();
            var catchAllDescriptor2 = TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                    .RequireAttributeDescriptor(attribute => attribute.Name("custom"))
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .Build();
            var catchAllWildcardPrefixDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("CatchAllWildCardAttribute", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("prefix-", RequiredAttributeNameComparison.PrefixMatch)))
                .Build();
            TagHelperCollection defaultAvailableDescriptors =
                [divDescriptor, inputDescriptor, catchAllDescriptor, catchAllDescriptor2];
            TagHelperCollection defaultWildcardDescriptors =
                [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor];
            Func<string, KeyValuePair<string, string>> kvp =
                (name) => new KeyValuePair<string, string>(name, "test value");

            // tagName, providedAttributes, availableTagHelpers, expectedTagHelpers
            return new()
            {
                {
                    "div",
                    ImmutableArray.Create(kvp("custom")),
                    defaultAvailableDescriptors,
                    default
                },
                { "div", ImmutableArray.Create(kvp("style")), defaultAvailableDescriptors, [divDescriptor] },
                { "div", ImmutableArray.Create(kvp("class")), defaultAvailableDescriptors, [catchAllDescriptor] },
                {
                    "div",
                    ImmutableArray.Create(kvp("class"), kvp("style")),
                    defaultAvailableDescriptors,
                    [divDescriptor, catchAllDescriptor]
                },
                {
                    "div",
                    ImmutableArray.Create(kvp("class"), kvp("style"), kvp("custom")),
                    defaultAvailableDescriptors,
                    [divDescriptor, catchAllDescriptor, catchAllDescriptor2]
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("class"), kvp("style")),
                    defaultAvailableDescriptors,
                    [inputDescriptor, catchAllDescriptor]
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("nodashprefixA")),
                    defaultWildcardDescriptors,
                    [inputWildcardPrefixDescriptor]
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("nodashprefix-ABC-DEF"), kvp("random")),
                    defaultWildcardDescriptors,
                    [inputWildcardPrefixDescriptor]
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("prefixABCnodashprefix")),
                    defaultWildcardDescriptors,
                    null
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("prefix-")),
                    defaultWildcardDescriptors,
                    null
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("nodashprefix")),
                    defaultWildcardDescriptors,
                    null
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("prefix-A")),
                    defaultWildcardDescriptors,
                    [catchAllWildcardPrefixDescriptor]
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("prefix-ABC-DEF"), kvp("random")),
                    defaultWildcardDescriptors,
                    [catchAllWildcardPrefixDescriptor]
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("prefix-abc"), kvp("nodashprefix-def")),
                    defaultWildcardDescriptors,
                    [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor]
                },
                {
                    "input",
                    ImmutableArray.Create(kvp("class"), kvp("prefix-abc"), kvp("onclick"), kvp("nodashprefix-def"), kvp("style")),
                    defaultWildcardDescriptors,
                    [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor]
                },
            };
        }
    }

    [Theory]
    [MemberData(nameof(RequiredAttributeData))]
    public void GetBinding_ReturnsBindingResultDescriptorsWithRequiredAttributes(
        string tagName,
        ImmutableArray<KeyValuePair<string, string>> providedAttributes,
        TagHelperCollection availableTagHelpers,
        TagHelperCollection? expectedTagHelpers)
    {
        // Arrange
        var binder = new TagHelperBinder(null, availableTagHelpers);

        // Act
        var binding = binder.GetBinding(tagName, providedAttributes, parentTagName: "p", parentIsTagHelper: false);
        var tagHelpers = binding?.TagHelpers;

        // Assert
        if (expectedTagHelpers is null)
        {
            Assert.Null(tagHelpers);
        }
        else
        {
            Assert.Equal(expectedTagHelpers, tagHelpers);
        }
    }

    [Fact]
    public void GetBinding_ReturnsNullBindingResultPrefixAsTagName()
    {
        // Arrange
        var catchAllTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();
        TagHelperCollection tagHelpers = [catchAllTagHelper];
        var tagHelperBinder = new TagHelperBinder("th", tagHelpers);

        // Act
        var binding = tagHelperBinder.GetBinding(
            tagName: "th",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void GetBinding_ReturnsBindingResultCatchAllDescriptorsForPrefixedTags()
    {
        // Arrange
        var catchAllTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();
        TagHelperCollection tagHelpers = [catchAllTagHelper];
        var tagHelperBinder = new TagHelperBinder("th:", tagHelpers);

        // Act
        var bindingDiv = tagHelperBinder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);
        var bindingSpan = tagHelperBinder.GetBinding(
            tagName: "th:span",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingDiv);
        var tagHelper = Assert.Single(bindingDiv.TagHelpers);
        Assert.Same(catchAllTagHelper, tagHelper);
        Assert.NotNull(bindingSpan);
        tagHelper = Assert.Single(bindingSpan.TagHelpers);
        Assert.Same(catchAllTagHelper, tagHelper);
    }

    [Fact]
    public void GetBinding_ReturnsBindingResultDescriptorsForPrefixedTags()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        TagHelperCollection tagHelpers = [divTagHelper];
        var tagHelperBinder = new TagHelperBinder("th:", tagHelpers);

        // Act
        var binding = tagHelperBinder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var tagHelper = Assert.Single(binding.TagHelpers);
        Assert.Same(divTagHelper, tagHelper);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("div")]
    public void GetBinding_ReturnsNullForUnprefixedTags(string tagName)
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(tagName))
            .Build();
        TagHelperCollection tagHelpers = [divTagHelper];
        var binder = new TagHelperBinder("th:", tagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void GetDescriptors_ReturnsNothingForUnregisteredTags()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        var spanTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("span"))
            .Build();
        TagHelperCollection tagHelpers = [divTagHelper, spanTagHelper];
        var binder = new TagHelperBinder(null, tagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "foo",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void GetDescriptors_ReturnsCatchAllsWithEveryTagName()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        var spanTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("span"))
            .Build();
        var catchAllTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo3", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();
        TagHelperCollection tagHelpers = [divTagHelper, spanTagHelper, catchAllTagHelper];
        var binder = new TagHelperBinder(null, tagHelpers);

        // Act
        var divBinding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);
        var spanBinding = binder.GetBinding(
            tagName: "span",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        // For divs
        Assert.NotNull(divBinding);
        Assert.Equal(2, divBinding.TagHelpers.Count);
        Assert.Contains(divTagHelper, divBinding.TagHelpers);
        Assert.Contains(catchAllTagHelper, divBinding.TagHelpers);

        // For spans
        Assert.NotNull(spanBinding);
        Assert.Equal(2, spanBinding.TagHelpers.Count);
        Assert.Contains(spanTagHelper, spanBinding.TagHelpers);
        Assert.Contains(catchAllTagHelper, spanBinding.TagHelpers);
    }

    [Fact]
    public void GetDescriptors_DuplicateDescriptorsAreNotPartOfTagHelperDescriptorPool()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        TagHelperCollection tagHelpers = [divTagHelper, divTagHelper];
        var binder = new TagHelperBinder(null, tagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var tagHelper = Assert.Single(binding.TagHelpers);
        Assert.Same(divTagHelper, tagHelper);
    }

    [Fact]
    public void GetBinding_DescriptorWithMultipleRules_CorrectlySelectsMatchingRules()
    {
        // Arrange
        var multiRuleTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                .RequireParentTag("body"))
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("span"))
            .Build();
        TagHelperCollection tagHelper = [multiRuleTagHelper];
        var binder = new TagHelperBinder(null, tagHelper);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var boundTagHelper = Assert.Single(binding.TagHelpers);
        Assert.Same(multiRuleTagHelper, boundTagHelper);
        var boundRules = binding.GetBoundRules(boundTagHelper);
        var boundRule = Assert.Single(boundRules);
        Assert.Equal("div", boundRule.TagName);
    }

    [Fact]
    public void GetBinding_PrefixedParent_ReturnsBinding()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div").RequireParentTag("p"))
            .Build();
        var pTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build();
        TagHelperCollection tagHelpers = [divTagHelper, pTagHelper];
        var binder = new TagHelperBinder("th:", tagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "th:p",
            parentIsTagHelper: true);

        // Assert
        Assert.NotNull(binding);
        var boundTagHelper = Assert.Single(binding.TagHelpers);
        Assert.Same(divTagHelper, boundTagHelper);
        var boundRules = binding.GetBoundRules(boundTagHelper);
        var boundRule = Assert.Single(boundRules);
        Assert.Equal("div", boundRule.TagName);
        Assert.Equal("p", boundRule.ParentTag);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_SingleAttributeMatch()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        TagHelperCollection tagHelpers = [divTagHelper];
        var binder = new TagHelperBinder("", tagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.True(binding.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_MultipleAttributeMatches()
    {
        // Arrange
        var divTagHelper1 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var divTagHelper2 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        TagHelperCollection tagHelpers = [divTagHelper1, divTagHelper2];
        var binder = new TagHelperBinder("", tagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.True(binding.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_MixedAttributeMatches()
    {
        // Arrange
        var divTagHelper1 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var divTagHelper2 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        TagHelperCollection tagHelpers = [divTagHelper1, divTagHelper2];
        var tagHelperBinder = new TagHelperBinder("", tagHelpers);

        // Act
        var binding = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.False(binding.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_CaseSensitiveRule_CaseMismatch_ReturnsNull()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .SetCaseSensitive()
            .Build();
        TagHelperCollection expectedTagHelpers = [divTagHelper];
        var expectedAttributes = ImmutableArray.Create(
            new KeyValuePair<string, string>("class", "something"));
        var binder = new TagHelperBinder("th:", expectedTagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "th:Div",
            attributes: expectedAttributes,
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void GetBinding_CaseSensitiveRequiredAttribute_CaseMismatch_ReturnsNull()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("div")
                .RequireAttributeDescriptor(attribute => attribute.Name("class")))
            .SetCaseSensitive()
            .Build();
        TagHelperCollection expectedTagHelpers = [divTagHelper];
        var expectedAttributes = ImmutableArray.Create(
            new KeyValuePair<string, string>("CLASS", "something"));
        var binder = new TagHelperBinder(null, expectedTagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: expectedAttributes,
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }
}
