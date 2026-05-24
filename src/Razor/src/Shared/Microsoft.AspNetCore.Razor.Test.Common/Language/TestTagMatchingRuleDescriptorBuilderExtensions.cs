// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TestTagMatchingRuleDescriptorBuilderExtensions
{
    public static TagMatchingRuleDescriptorBuilder RequireTagName(this TagMatchingRuleDescriptorBuilder builder, string tagName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.TagName = tagName;

        return builder;
    }

    public static TagMatchingRuleDescriptorBuilder RequireParentTag(this TagMatchingRuleDescriptorBuilder builder, string parentTag)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ParentTag = parentTag;

        return builder;
    }

    public static TagMatchingRuleDescriptorBuilder RequireTagStructure(this TagMatchingRuleDescriptorBuilder builder, TagStructure tagStructure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.TagStructure = tagStructure;

        return builder;
    }

    public static TagMatchingRuleDescriptorBuilder AddDiagnostic(this TagMatchingRuleDescriptorBuilder builder, RazorDiagnostic diagnostic)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Diagnostics.Add(diagnostic);

        return builder;
    }

    public static TagMatchingRuleDescriptorBuilder RequireAttributeDescriptor(
        this TagMatchingRuleDescriptorBuilder builder,
        Action<RequiredAttributeDescriptorBuilder> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Attribute(configure);

        return builder;
    }

#nullable enable

    public static TagMatchingRuleDescriptorBuilder RequiredAttribute(
        this TagMatchingRuleDescriptorBuilder builder,
        Action<RequiredAttributeDescriptorBuilder> configure)
        => builder.RequiredAttribute(
            name: default,
            nameComparison: default,
            value: default,
            valueComparison: default,
            configure: configure);

    public static TagMatchingRuleDescriptorBuilder RequiredAttribute(
        this TagMatchingRuleDescriptorBuilder builder,
        string name,
        Action<RequiredAttributeDescriptorBuilder>? configure = null)
        => builder.RequiredAttribute(name, nameComparison: default, value: default, valueComparison: default, configure: configure);

    public static TagMatchingRuleDescriptorBuilder RequiredAttribute(
        this TagMatchingRuleDescriptorBuilder builder,
        string name,
        RequiredAttributeNameComparison nameComparison,
        Action<RequiredAttributeDescriptorBuilder>? configure = null)
        => builder.RequiredAttribute(name, nameComparison, value: default, valueComparison: default, configure: configure);

    public static TagMatchingRuleDescriptorBuilder RequiredAttribute(
        this TagMatchingRuleDescriptorBuilder builder,
        Optional<string> name = default,
        Optional<RequiredAttributeNameComparison> nameComparison = default,
        Optional<string?> value = default,
        Optional<RequiredAttributeValueComparison> valueComparison = default,
        Action<RequiredAttributeDescriptorBuilder>? configure = null)
    {
        builder.Attribute(attribute =>
        {
            if (name.HasValue)
            {
                attribute.Name = name.Value;
            }

            if (nameComparison.HasValue)
            {
                attribute.NameComparison = nameComparison.Value;
            }

            if (value.HasValue)
            {
                attribute.Value = value.Value;
            }

            if (valueComparison.HasValue)
            {
                attribute.ValueComparison = valueComparison.Value;
            }

            configure?.Invoke(attribute);
        });

        return builder;
    }
}
