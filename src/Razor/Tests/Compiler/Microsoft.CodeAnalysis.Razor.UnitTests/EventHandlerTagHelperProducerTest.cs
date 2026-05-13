// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class EventHandlerTagHelperProducerTest : TagHelperDescriptorProviderTestBase
{
    protected override void ConfigureEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new EventHandlerTagHelperProducer.Factory());
    }

    [Fact]
    public void Execute_EventHandler_TwoArgumentsCreatesDescriptor()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Web;

            namespace Test
            {
                [EventHandler("onclick", typeof(Action<MouseEventArgs>))]
                public class EventHandlers
                {
                }
            }
            """));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetEventHandlerTagHelpers(result);
        var item = Assert.Single(matches);

        // These are features Event Handler Tag Helpers don't use. Verifying them once here and
        // then ignoring them.
        Assert.Empty(item.AllowedChildTags);
        Assert.Null(item.TagOutputHint);

        // These are features that are invariants of all Event Handler Helpers. Verifying them once
        // here and then ignoring them.
        Assert.Empty(item.Diagnostics);
        Assert.False(item.HasErrors);
        Assert.Equal(TagHelperKind.EventHandler, item.Kind);
        Assert.Equal(RuntimeKind.None, item.RuntimeKind);
        Assert.False(item.IsDefaultKind());
        Assert.False(item.KindUsesDefaultTagHelperRuntime());
        Assert.False(item.IsComponentOrChildContentTagHelper());
        Assert.True(item.CaseSensitive);
        Assert.True(item.ClassifyAttributesOnly);

        Assert.Equal(
            "Sets the '@onclick' attribute to the provided string or delegate value. " +
            "A delegate value should be of type 'System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs>'.",
            item.Documentation);

        // These are all trivially derived from the assembly/namespace/type name
        Assert.Equal("Microsoft.AspNetCore.Components", item.AssemblyName);
        Assert.Equal("onclick", item.Name);
        Assert.Equal("Test.EventHandlers", item.DisplayName);
        Assert.Equal("Test.EventHandlers", item.TypeName);

        // The tag matching rule for an event handler is just the attribute name
        var rule = Assert.Single(item.TagMatchingRules);
        Assert.Empty(rule.Diagnostics);
        Assert.False(rule.HasErrors);
        Assert.Null(rule.ParentTag);
        Assert.Equal("*", rule.TagName);
        Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

        var requiredAttribute = Assert.Single(rule.Attributes);
        Assert.Empty(requiredAttribute.Diagnostics);
        Assert.Equal("@onclick", requiredAttribute.DisplayName);
        Assert.Equal("@onclick", requiredAttribute.Name);
        Assert.Equal(RequiredAttributeNameComparison.FullMatch, requiredAttribute.NameComparison);
        Assert.Null(requiredAttribute.Value);
        Assert.Equal(RequiredAttributeValueComparison.None, requiredAttribute.ValueComparison);

        var attribute = Assert.Single(item.BoundAttributes);

        // Invariants
        Assert.Empty(attribute.Diagnostics);
        Assert.False(attribute.HasErrors);
        Assert.Equal(TagHelperKind.EventHandler, attribute.Parent.Kind);
        Assert.False(attribute.IsDefaultKind());
        Assert.False(attribute.HasIndexer);
        Assert.Null(attribute.IndexerNamePrefix);
        Assert.Null(attribute.IndexerTypeName);
        Assert.False(attribute.IsIndexerBooleanProperty);
        Assert.False(attribute.IsIndexerStringProperty);
        Assert.True(attribute.IsDirectiveAttribute);
        Assert.Equal("onclick", attribute.PropertyName);
        Assert.True(attribute.IsWeaklyTyped);

        Assert.Equal(
            "Sets the '@onclick' attribute to the provided string or delegate value. " +
            "A delegate value should be of type 'System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs>'.",
            attribute.Documentation);

        Assert.Equal("@onclick", attribute.Name);
        Assert.Equal("onclick", attribute.PropertyName);
        Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs>> Test.EventHandlers.onclick", attribute.DisplayName);

        // Defined from the property type
        Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs>>", attribute.TypeName);
        Assert.False(attribute.IsStringProperty);
        Assert.False(attribute.IsBooleanProperty);
        Assert.False(attribute.IsEnum);
    }

    [Fact]
    public void Execute_EventHandler_FourArgumentsCreatesDescriptorWithDiagnostic()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Web;

            namespace Test
            {
                [EventHandler("onclick", typeof(Action<MouseEventArgs>), true, true)]
                public class EventHandlers
                {
                }
            }
            """));

        Assert.Empty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetEventHandlerTagHelpers(result);
        var item = Assert.Single(matches);

        // These are features Event Handler Tag Helpers don't use. Verifying them once here and
        // then ignoring them.
        Assert.Empty(item.AllowedChildTags);
        Assert.Null(item.TagOutputHint);

        // These are features that are invariants of all Event Handler Helpers. Verifying them once
        // here and then ignoring them.
        Assert.Empty(item.Diagnostics);
        Assert.False(item.HasErrors);
        Assert.Equal(TagHelperKind.EventHandler, item.Kind);
        Assert.Equal(RuntimeKind.None, item.RuntimeKind);
        Assert.False(item.IsDefaultKind());
        Assert.False(item.KindUsesDefaultTagHelperRuntime());
        Assert.False(item.IsComponentOrChildContentTagHelper());
        Assert.True(item.CaseSensitive);
        Assert.True(item.ClassifyAttributesOnly);

        Assert.Equal(
            "Sets the '@onclick' attribute to the provided string or delegate value. " +
            "A delegate value should be of type 'System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs>'.",
            item.Documentation);

        // These are all trivially derived from the assembly/namespace/type name
        Assert.Equal("Microsoft.AspNetCore.Components", item.AssemblyName);
        Assert.Equal("onclick", item.Name);
        Assert.Equal("Test.EventHandlers", item.DisplayName);
        Assert.Equal("Test.EventHandlers", item.TypeName);

        Assert.Equal(3, item.TagMatchingRules.Length);

        var catchAllRule = item.TagMatchingRules[0];
        Assert.Empty(catchAllRule.Diagnostics);
        Assert.False(catchAllRule.HasErrors);
        Assert.Null(catchAllRule.ParentTag);
        Assert.Equal("*", catchAllRule.TagName);
        Assert.Equal(TagStructure.Unspecified, catchAllRule.TagStructure);

        var catchAllRequiredAttribute = Assert.Single(catchAllRule.Attributes);
        Assert.Empty(catchAllRequiredAttribute.Diagnostics);
        Assert.Equal("@onclick", catchAllRequiredAttribute.DisplayName);
        Assert.Equal("@onclick", catchAllRequiredAttribute.Name);
        Assert.Equal(RequiredAttributeNameComparison.FullMatch, catchAllRequiredAttribute.NameComparison);
        Assert.Null(catchAllRequiredAttribute.Value);
        Assert.Equal(RequiredAttributeValueComparison.None, catchAllRequiredAttribute.ValueComparison);

        var preventDefaultRule = item.TagMatchingRules[1];
        Assert.Empty(preventDefaultRule.Diagnostics);
        Assert.False(preventDefaultRule.HasErrors);
        Assert.Null(preventDefaultRule.ParentTag);
        Assert.Equal("*", preventDefaultRule.TagName);
        Assert.Equal(TagStructure.Unspecified, preventDefaultRule.TagStructure);

        var preventDefaultRequiredAttribute = Assert.Single(preventDefaultRule.Attributes);
        Assert.Empty(preventDefaultRequiredAttribute.Diagnostics);
        Assert.Equal("@onclick:preventDefault", preventDefaultRequiredAttribute.DisplayName);
        Assert.Equal("@onclick:preventDefault", preventDefaultRequiredAttribute.Name);
        Assert.Equal(RequiredAttributeNameComparison.FullMatch, preventDefaultRequiredAttribute.NameComparison);
        Assert.Null(preventDefaultRequiredAttribute.Value);
        Assert.Equal(RequiredAttributeValueComparison.None, preventDefaultRequiredAttribute.ValueComparison);

        var stopPropagationRule = item.TagMatchingRules[2];
        Assert.Empty(stopPropagationRule.Diagnostics);
        Assert.False(stopPropagationRule.HasErrors);
        Assert.Null(stopPropagationRule.ParentTag);
        Assert.Equal("*", stopPropagationRule.TagName);
        Assert.Equal(TagStructure.Unspecified, stopPropagationRule.TagStructure);

        var stopPropagationRequiredAttribute = Assert.Single(stopPropagationRule.Attributes);
        Assert.Empty(stopPropagationRequiredAttribute.Diagnostics);
        Assert.Equal("@onclick:stopPropagation", stopPropagationRequiredAttribute.DisplayName);
        Assert.Equal("@onclick:stopPropagation", stopPropagationRequiredAttribute.Name);
        Assert.Equal(RequiredAttributeNameComparison.FullMatch, stopPropagationRequiredAttribute.NameComparison);
        Assert.Null(stopPropagationRequiredAttribute.Value);
        Assert.Equal(RequiredAttributeValueComparison.None, stopPropagationRequiredAttribute.ValueComparison);

        var attribute = Assert.Single(item.BoundAttributes);

        // Invariants
        Assert.Empty(attribute.Diagnostics);
        Assert.False(attribute.HasErrors);
        Assert.Equal(TagHelperKind.EventHandler, attribute.Parent.Kind);
        Assert.False(attribute.IsDefaultKind());
        Assert.False(attribute.HasIndexer);
        Assert.Null(attribute.IndexerNamePrefix);
        Assert.Null(attribute.IndexerTypeName);
        Assert.False(attribute.IsIndexerBooleanProperty);
        Assert.False(attribute.IsIndexerStringProperty);
        Assert.True(attribute.IsDirectiveAttribute);
        Assert.Equal("onclick", attribute.PropertyName);
        Assert.True(attribute.IsWeaklyTyped);

        Assert.Equal(
            "Sets the '@onclick' attribute to the provided string or delegate value. " +
            "A delegate value should be of type 'System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs>'.",
            attribute.Documentation);

        Assert.Equal("@onclick", attribute.Name);
        Assert.Equal("onclick", attribute.PropertyName);
        Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs>> Test.EventHandlers.onclick", attribute.DisplayName);

        // Defined from the property type
        Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs>>", attribute.TypeName);
        Assert.False(attribute.IsStringProperty);
        Assert.False(attribute.IsBooleanProperty);
        Assert.False(attribute.IsEnum);
    }

    [Fact]
    public void Execute_EventHandler_NoArgumentsDoesNotCreateDescriptor()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Web;

            namespace Test
            {
                [EventHandler]
                public class EventHandlers
                {
                }
            }
            """));

        Assert.NotEmpty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetEventHandlerTagHelpers(result);
        Assert.Empty(matches);
    }

    [Fact]
    public void Execute_EventHandler_OneArgumentDoesNotCreateDescriptor()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Web;

            namespace Test
            {
                [EventHandler("onclick")]
                public class EventHandlers
                {
                }
            }
            """));

        Assert.NotEmpty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetEventHandlerTagHelpers(result);
        Assert.Empty(matches);
    }

    [Fact]
    public void Execute_EventHandler_ThreeArgumentsDoesNotCreateDiagnostic()
    {
        // Arrange
        var compilation = BaseCompilation.AddSyntaxTrees(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Web;

            namespace Test
            {
                [EventHandler("onclick", typeof(Action<MouseEventArgs>), true)]
                public class EventHandlers
                {
                }
            }
            """));

        Assert.NotEmpty(compilation.GetDiagnostics());

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        var matches = GetEventHandlerTagHelpers(result);
        Assert.Empty(matches);
    }

    private static TagHelperCollection GetEventHandlerTagHelpers(TagHelperCollection collection)
        => collection.Where(static t => t.Kind == TagHelperKind.EventHandler && !IsBuiltInComponent(t));
}
