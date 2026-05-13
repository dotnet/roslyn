// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TestTagHelperDescriptorBuilderExtensions
{
    extension(TagHelperDescriptorBuilder)
    {
        public static TagHelperDescriptorBuilder CreateTagHelper(string name, string assemblyName)
            => CreateTagHelper(TagHelperKind.ITagHelper, name, assemblyName);

        public static TagHelperDescriptorBuilder CreateTagHelper(TagHelperKind kind, string name, string assemblyName)
        {
            var builder = TagHelperDescriptorBuilder.Create(kind, name, assemblyName);
            builder.RuntimeKind = Language.RuntimeKind.ITagHelper;

            return builder;
        }

        public static TagHelperDescriptorBuilder CreateViewComponent(string name, string assemblyName)
            => CreateTagHelper(TagHelperKind.ViewComponent, name, assemblyName);

        public static TagHelperDescriptorBuilder CreateComponent(string name, string assemblyName)
        {
            var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, name, assemblyName);
            builder.RuntimeKind = Language.RuntimeKind.IComponent;

            return builder;
        }

        public static TagHelperDescriptorBuilder CreateEventHandler(string name, string assemblyName)
            => TagHelperDescriptorBuilder.Create(TagHelperKind.EventHandler, name, assemblyName);
    }

    public static TagHelperDescriptorBuilder Metadata(
        this TagHelperDescriptorBuilder builder,
        MetadataObject metadata)
    {
        builder.SetMetadata(metadata);

        return builder;
    }

    public static TagHelperDescriptorBuilder DisplayName(this TagHelperDescriptorBuilder builder, string displayName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.DisplayName = displayName;

        return builder;
    }

    public static TagHelperDescriptorBuilder AllowChildTag(this TagHelperDescriptorBuilder builder, string allowedChild)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AllowChildTag(childTagBuilder => childTagBuilder.Name = allowedChild);

        return builder;
    }

    public static TagHelperDescriptorBuilder TagOutputHint(this TagHelperDescriptorBuilder builder, string hint)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.TagOutputHint = hint;

        return builder;
    }

    public static TagHelperDescriptorBuilder RuntimeKind(this TagHelperDescriptorBuilder builder, RuntimeKind runtimeKind)
    {
        builder.RuntimeKind = runtimeKind;

        return builder;
    }

    public static TagHelperDescriptorBuilder IsFullyQualifiedNameMatch(this TagHelperDescriptorBuilder builder, bool value)
    {
        builder.IsFullyQualifiedNameMatch = value;

        return builder;
    }

    public static TagHelperDescriptorBuilder ClassifyAttributesOnly(this TagHelperDescriptorBuilder builder, bool value)
    {
        builder.ClassifyAttributesOnly = value;

        return builder;
    }

    public static TagHelperDescriptorBuilder SetCaseSensitive(this TagHelperDescriptorBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.CaseSensitive = true;

        return builder;
    }

    public static TagHelperDescriptorBuilder TypeName(
        this TagHelperDescriptorBuilder builder,
        string fullName,
        string? typeNamespace = null,
        string? typeNameIdentifier = null)
    {
        builder.SetTypeName(fullName, typeNamespace, typeNameIdentifier);

        return builder;
    }

    public static TagHelperDescriptorBuilder Documentation(this TagHelperDescriptorBuilder builder, string documentation)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.SetDocumentation(documentation);

        return builder;
    }

    public static TagHelperDescriptorBuilder AddDiagnostic(this TagHelperDescriptorBuilder builder, RazorDiagnostic diagnostic)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Diagnostics.Add(diagnostic);

        return builder;
    }

    public static TagHelperDescriptorBuilder BoundAttributeDescriptor(
        this TagHelperDescriptorBuilder builder,
        Action<BoundAttributeDescriptorBuilder> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.BindAttribute(configure);

        return builder;
    }

    public static TagHelperDescriptorBuilder TagMatchingRuleDescriptor(
        this TagHelperDescriptorBuilder builder,
        Action<TagMatchingRuleDescriptorBuilder> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.TagMatchingRule(configure);

        return builder;
    }

#nullable enable

    public static TagHelperDescriptorBuilder AllowedChildTag(
        this TagHelperDescriptorBuilder builder,
        string tagName,
        Action<AllowedChildTagDescriptorBuilder>? configure = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AllowChildTag(childTag =>
        {
            childTag.Name = tagName;

            configure?.Invoke(childTag);
        });

        return builder;
    }

    public static TagHelperDescriptorBuilder BoundAttribute<T>(
        this TagHelperDescriptorBuilder builder,
        string name,
        string propertyName,
        Action<BoundAttributeDescriptorBuilder>? configure = null)
        where T : notnull
        => BoundAttribute(builder, name, propertyName, typeof(T), configure);

    public static TagHelperDescriptorBuilder BoundAttribute(
        this TagHelperDescriptorBuilder builder,
        string name,
        string propertyName,
        Type type,
        Action<BoundAttributeDescriptorBuilder>? configure = null)
        => BoundAttribute(builder, name, propertyName, type.FullName!, configure);

    public static TagHelperDescriptorBuilder BoundAttribute(
        this TagHelperDescriptorBuilder builder,
        string name,
        string propertyName,
        string typeName,
        Action<BoundAttributeDescriptorBuilder>? configure = null)
    {
        builder.BoundAttributeDescriptor(attribute =>
         {
             attribute.Name = name;
             attribute.PropertyName = propertyName;
             attribute.TypeName = typeName;

             configure?.Invoke(attribute);
         });

        return builder;
    }

    public static TagHelperDescriptorBuilder TagMatchingRule(
        this TagHelperDescriptorBuilder builder,
        Action<TagMatchingRuleDescriptorBuilder> configure)
        => builder.TagMatchingRule(tagName: null, parentTagName: null, tagStructure: TagStructure.Unspecified, configure);

    public static TagHelperDescriptorBuilder TagMatchingRule(
        this TagHelperDescriptorBuilder builder,
        string tagName,
        Action<TagMatchingRuleDescriptorBuilder> configure)
        => builder.TagMatchingRule(tagName, parentTagName: null, tagStructure: TagStructure.Unspecified, configure);

    public static TagHelperDescriptorBuilder TagMatchingRule(
        this TagHelperDescriptorBuilder builder,
        string tagName,
        string parentTagName,
        Action<TagMatchingRuleDescriptorBuilder> configure)
        => builder.TagMatchingRule(tagName, parentTagName, tagStructure: TagStructure.Unspecified, configure);

    public static TagHelperDescriptorBuilder TagMatchingRule(
        this TagHelperDescriptorBuilder builder,
        string tagName,
        TagStructure tagStructure,
        Action<TagMatchingRuleDescriptorBuilder> configure)
        => builder.TagMatchingRule(tagName, parentTagName: null, tagStructure, configure);

    public static TagHelperDescriptorBuilder TagMatchingRule(
        this TagHelperDescriptorBuilder builder,
        string? tagName = null,
        string? parentTagName = null,
        TagStructure tagStructure = TagStructure.Unspecified,
        Action<TagMatchingRuleDescriptorBuilder>? configure = null)
    {
        builder.TagMatchingRule(rule =>
        {
            rule.TagName = tagName;
            rule.ParentTag = parentTagName;
            rule.TagStructure = tagStructure;

            configure?.Invoke(rule);
        });

        return builder;
    }
}
