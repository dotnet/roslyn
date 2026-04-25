// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperMatchingConventionsTest
{
    public static TheoryData RequiredAttributeDescriptorData
    {
        get
        {
            // requiredAttributeDescriptor, attributeName, attributeValue, expectedResult
            return new TheoryData<Action<RequiredAttributeDescriptorBuilder>, string, string, bool>
                {
                    {
                        builder => builder.Name("key"),
                        "KeY",
                        "value",
                        true
                    },
                    {
                        builder => builder.Name("key"),
                        "keys",
                        "value",
                        false
                    },
                    {
                        builder => builder.Name("route-", RequiredAttributeNameComparison.PrefixMatch),
                        "ROUTE-area",
                        "manage",
                        true
                    },
                    {
                        builder => builder.Name("route-", RequiredAttributeNameComparison.PrefixMatch),
                        "routearea",
                        "manage",
                        false
                    },
                    {
                        builder => builder.Name("route-", RequiredAttributeNameComparison.PrefixMatch),
                        "route-",
                        "manage",
                        false
                    },
                    {
                        builder => builder.Name("key", RequiredAttributeNameComparison.FullMatch),
                        "KeY",
                        "value",
                        true
                    },
                    {
                        builder => builder.Name("key", RequiredAttributeNameComparison.FullMatch),
                        "keys",
                        "value",
                        false
                    },
                    {
                        builder => builder
                            .Name("key", RequiredAttributeNameComparison.FullMatch)
                            .Value("value", RequiredAttributeValueComparison.FullMatch),
                        "key",
                        "value",
                        true
                    },
                    {
                        builder => builder
                            .Name("key", RequiredAttributeNameComparison.FullMatch)
                            .Value("value", RequiredAttributeValueComparison.FullMatch),
                        "key",
                        "Value",
                        false
                    },
                    {
                        builder => builder
                            .Name("class", RequiredAttributeNameComparison.FullMatch)
                            .Value("btn", RequiredAttributeValueComparison.PrefixMatch),
                        "class",
                        "btn btn-success",
                        true
                    },
                    {
                        builder => builder
                            .Name("class", RequiredAttributeNameComparison.FullMatch)
                            .Value("btn", RequiredAttributeValueComparison.PrefixMatch),
                        "class",
                        "BTN btn-success",
                        false
                    },
                    {
                        builder => builder
                            .Name("href", RequiredAttributeNameComparison.FullMatch)
                            .Value("#navigate", RequiredAttributeValueComparison.SuffixMatch),
                        "href",
                        "/home/index#navigate",
                        true
                    },
                    {
                        builder => builder
                            .Name("href", RequiredAttributeNameComparison.FullMatch)
                            .Value("#navigate", RequiredAttributeValueComparison.SuffixMatch),
                        "href",
                        "/home/index#NAVigate",
                        false
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(RequiredAttributeDescriptorData))]
    public void Matches_ReturnsExpectedResult(
        Action<RequiredAttributeDescriptorBuilder> configure,
        string attributeName,
        string attributeValue,
        bool expectedResult)
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TagMatchingRuleDescriptor(builder => builder
                .RequiredAttribute(builder => configure(builder)))
            .Build();

        var requiredAttribute = tagHelper.TagMatchingRules[0].Attributes[0];

        // Act
        var result = TagHelperMatchingConventions.SatisfiesRequiredAttribute(requiredAttribute, attributeName, attributeValue);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void CanSatisfyBoundAttribute_IndexerAttribute_ReturnsFalseIsNotMatching()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .BoundAttributeDescriptor(builder => builder.AsDictionary("asp-", typeof(Dictionary<string, string>).FullName))
            .Build();

        var boundAttribute = tagHelper.BoundAttributes[0];

        // Act
        var result = TagHelperMatchingConventions.CanSatisfyBoundAttribute("style", boundAttribute);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanSatisfyBoundAttribute_IndexerAttribute_ReturnsTrueIfMatching()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .BoundAttributeDescriptor(builder => builder.AsDictionary("asp-", typeof(Dictionary<string, string>).FullName))
            .Build();

        var boundAttribute = tagHelper.BoundAttributes[0];

        // Act
        var result = TagHelperMatchingConventions.CanSatisfyBoundAttribute("asp-route-controller", boundAttribute);

        // Assert
        Assert.True(result);
    }
}
