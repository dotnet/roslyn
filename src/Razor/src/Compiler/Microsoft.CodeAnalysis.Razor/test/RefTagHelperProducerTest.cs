// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class RefTagHelperProducerTest : TagHelperDescriptorProviderTestBase
{
    protected override void ConfigureEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new RefTagHelperProducer.Factory());
    }

    [Fact]
    public void GetTagHelpers_CreatesTagHelper()
    {
        // Act
        var result = GetTagHelpers(BaseCompilation);

        // Assert
        var matches = result.Where(static result => result.Kind == TagHelperKind.Ref);
        var item = Assert.Single(matches);

        Assert.Empty(item.AllowedChildTags);
        Assert.Null(item.TagOutputHint);
        Assert.Empty(item.Diagnostics);
        Assert.False(item.HasErrors);
        Assert.Equal(TagHelperKind.Ref, item.Kind);
        Assert.Equal(RuntimeKind.None, item.RuntimeKind);
        Assert.False(item.IsDefaultKind());
        Assert.False(item.KindUsesDefaultTagHelperRuntime());
        Assert.False(item.IsComponentOrChildContentTagHelper());
        Assert.True(item.CaseSensitive);
        Assert.True(item.ClassifyAttributesOnly);

        Assert.Equal(
            "Populates the specified field or property with a reference to the element or component.",
            item.Documentation);

        Assert.Equal("Microsoft.AspNetCore.Components", item.AssemblyName);
        Assert.Equal("Ref", item.Name);
        Assert.Equal("Microsoft.AspNetCore.Components.Ref", item.DisplayName);
        Assert.Equal("Microsoft.AspNetCore.Components.Ref", item.TypeName);

        // The tag matching rule for a ref is just the attribute name "ref"
        var rule = Assert.Single(item.TagMatchingRules);
        Assert.Empty(rule.Diagnostics);
        Assert.False(rule.HasErrors);
        Assert.Null(rule.ParentTag);
        Assert.Equal("*", rule.TagName);
        Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

        var requiredAttribute = Assert.Single(rule.Attributes);
        Assert.Empty(requiredAttribute.Diagnostics);
        Assert.Equal("@ref", requiredAttribute.DisplayName);
        Assert.Equal("@ref", requiredAttribute.Name);
        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
        Assert.Null(requiredAttribute.Value);
        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);

        var attribute = Assert.Single(item.BoundAttributes);
        Assert.Empty(attribute.Diagnostics);
        Assert.False(attribute.HasErrors);
        Assert.Equal(TagHelperKind.Ref, attribute.Parent.Kind);
        Assert.False(attribute.IsDefaultKind());
        Assert.False(attribute.HasIndexer);
        Assert.Null(attribute.IndexerNamePrefix);
        Assert.Null(attribute.IndexerTypeName);
        Assert.False(attribute.IsIndexerBooleanProperty);
        Assert.False(attribute.IsIndexerStringProperty);

        Assert.Equal(
            "Populates the specified field or property with a reference to the element or component.",
            attribute.Documentation);

        Assert.Equal("@ref", attribute.Name);
        Assert.Equal("Ref", attribute.PropertyName);
        Assert.Equal("object Microsoft.AspNetCore.Components.Ref.Ref", attribute.DisplayName);
        Assert.Equal("System.Object", attribute.TypeName);
        Assert.False(attribute.IsStringProperty);
        Assert.False(attribute.IsBooleanProperty);
        Assert.False(attribute.IsEnum);
    }
}
