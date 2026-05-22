// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class KeyTagHelperProducerTest : TagHelperDescriptorProviderTestBase
{
    protected override void ConfigureEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new KeyTagHelperProducer.Factory());
    }

    [Fact]
    public void GetTagHelpers_CreatesTagHelper()
    {
        // Act
        var result = GetTagHelpers(BaseCompilation);

        // Assert
        var matches = result.Where(static result => result.Kind == TagHelperKind.Key);
        var item = Assert.Single(matches);

        Assert.Empty(item.AllowedChildTags);
        Assert.Null(item.TagOutputHint);
        Assert.Empty(item.Diagnostics);
        Assert.False(item.HasErrors);
        Assert.Equal(TagHelperKind.Key, item.Kind);
        Assert.Equal(RuntimeKind.None, item.RuntimeKind);
        Assert.False(item.IsDefaultKind());
        Assert.False(item.KindUsesDefaultTagHelperRuntime());
        Assert.False(item.IsComponentOrChildContentTagHelper());
        Assert.True(item.CaseSensitive);
        Assert.True(item.ClassifyAttributesOnly);

        Assert.Equal(
            "Ensures that the component or element will be preserved across renders if (and only if) the supplied key value matches.",
            item.Documentation);

        Assert.Equal("Microsoft.AspNetCore.Components", item.AssemblyName);
        Assert.Equal("Key", item.Name);
        Assert.Equal("Microsoft.AspNetCore.Components.Key", item.DisplayName);
        Assert.Equal("Microsoft.AspNetCore.Components.Key", item.TypeName);

        // The tag matching rule for a key is just the attribute name "key"
        var rule = Assert.Single(item.TagMatchingRules);
        Assert.Empty(rule.Diagnostics);
        Assert.False(rule.HasErrors);
        Assert.Null(rule.ParentTag);
        Assert.Equal("*", rule.TagName);
        Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

        var requiredAttribute = Assert.Single(rule.Attributes);
        Assert.Empty(requiredAttribute.Diagnostics);
        Assert.Equal("@key", requiredAttribute.DisplayName);
        Assert.Equal("@key", requiredAttribute.Name);
        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
        Assert.Null(requiredAttribute.Value);
        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);

        var attribute = Assert.Single(item.BoundAttributes);
        Assert.Empty(attribute.Diagnostics);
        Assert.False(attribute.HasErrors);
        Assert.Equal(TagHelperKind.Key, attribute.Parent.Kind);
        Assert.False(attribute.IsDefaultKind());
        Assert.False(attribute.HasIndexer);
        Assert.Null(attribute.IndexerNamePrefix);
        Assert.Null(attribute.IndexerTypeName);
        Assert.False(attribute.IsIndexerBooleanProperty);
        Assert.False(attribute.IsIndexerStringProperty);

        Assert.Equal(
            "Ensures that the component or element will be preserved across renders if (and only if) the supplied key value matches.",
            attribute.Documentation);

        Assert.Equal("@key", attribute.Name);
        Assert.Equal("Key", attribute.PropertyName);
        Assert.Equal("object Microsoft.AspNetCore.Components.Key.Key", attribute.DisplayName);
        Assert.Equal("System.Object", attribute.TypeName);
        Assert.False(attribute.IsStringProperty);
        Assert.False(attribute.IsBooleanProperty);
        Assert.False(attribute.IsEnum);
    }
}
