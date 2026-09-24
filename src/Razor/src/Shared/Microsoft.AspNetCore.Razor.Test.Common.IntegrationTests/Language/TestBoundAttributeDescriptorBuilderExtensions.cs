// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TestBoundAttributeDescriptorBuilderExtensions
{
    public static BoundAttributeDescriptorBuilder Name(this BoundAttributeDescriptorBuilder builder, string name)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Name = name;

        return builder;
    }

    public static BoundAttributeDescriptorBuilder TypeName(this BoundAttributeDescriptorBuilder builder, string typeName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.TypeName = typeName;

        return builder;
    }

    public static BoundAttributeDescriptorBuilder PropertyName(this BoundAttributeDescriptorBuilder builder, string propertyName)
    {
        builder.PropertyName = propertyName;

        return builder;
    }

    public static BoundAttributeDescriptorBuilder IsDirectiveAttribute(
        this BoundAttributeDescriptorBuilder builder, bool isDirectiveAttribute = true)
    {
        builder.IsDirectiveAttribute = isDirectiveAttribute;

        return builder;
    }

    public static BoundAttributeDescriptorBuilder Metadata(
        this BoundAttributeDescriptorBuilder builder,
        MetadataObject metadata)
    {
        builder.SetMetadata(metadata);

        return builder;
    }

    public static BoundAttributeDescriptorBuilder DisplayName(this BoundAttributeDescriptorBuilder builder, string displayName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.DisplayName = displayName;

        return builder;
    }

    public static BoundAttributeDescriptorBuilder AsEnum(this BoundAttributeDescriptorBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.IsEnum = true;

        return builder;
    }

    public static BoundAttributeDescriptorBuilder AsDictionaryAttribute(
        this BoundAttributeDescriptorBuilder builder,
        string attributeNamePrefix,
        string valueTypeName)
    {
        builder.IsDictionary = true;
        builder.IndexerAttributeNamePrefix = attributeNamePrefix;
        builder.IndexerValueTypeName = valueTypeName;

        return builder;
    }

    public static BoundAttributeDescriptorBuilder Documentation(this BoundAttributeDescriptorBuilder builder, string documentation)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Documentation = documentation;

        return builder;
    }

    public static BoundAttributeDescriptorBuilder AddDiagnostic(this BoundAttributeDescriptorBuilder builder, RazorDiagnostic diagnostic)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Diagnostics.Add(diagnostic);

        return builder;
    }

#nullable enable

    public static BoundAttributeDescriptorBuilder AsDictionaryAttribute<TValue>(
        this BoundAttributeDescriptorBuilder builder,
        string attributeNamePrefix)
        => builder.AsDictionaryAttribute(attributeNamePrefix, typeof(TValue));

    public static BoundAttributeDescriptorBuilder AsDictionaryAttribute(
        this BoundAttributeDescriptorBuilder builder,
        string attributeNamePrefix,
        Type valueType)
        => builder.AsDictionaryAttribute(attributeNamePrefix, valueType.FullName);
}
