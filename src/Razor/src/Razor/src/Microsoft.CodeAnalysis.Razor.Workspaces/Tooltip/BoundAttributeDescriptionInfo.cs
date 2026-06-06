// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal record BoundAttributeDescriptionInfo(string ReturnTypeName, string TypeName, string PropertyName, string? Documentation = null)
{
    public static BoundAttributeDescriptionInfo From(BoundAttributeParameterDescriptor parameter)
    {
        ArgHelper.ThrowIfNull(parameter);

        var parentTagHelperTypeName = parameter.Parent.Parent.TypeName;
        var propertyName = parameter.PropertyName;

        return new BoundAttributeDescriptionInfo(
            parameter.TypeName,
            parentTagHelperTypeName,
            propertyName,
            parameter.Documentation);
    }

    public static BoundAttributeDescriptionInfo From(BoundAttributeDescriptor boundAttribute, bool isIndexer)
        => From(boundAttribute, isIndexer, parentTagHelperTypeName: null);

    public static BoundAttributeDescriptionInfo From(BoundAttributeDescriptor boundAttribute, bool isIndexer, string? parentTagHelperTypeName)
    {
        if (boundAttribute is null)
        {
            throw new ArgumentNullException(nameof(boundAttribute));
        }

        var returnTypeName = isIndexer ? boundAttribute.IndexerTypeName : boundAttribute.TypeName;
        var propertyName = boundAttribute.PropertyName;

        // The BoundAttributeDescriptor does not directly have the TagHelperTypeName information available.
        // Because of this we need to resolve it from other parts of it.
        parentTagHelperTypeName ??= ResolveTagHelperTypeName(propertyName, boundAttribute.DisplayName);

        return new BoundAttributeDescriptionInfo(
            returnTypeName.AssumeNotNull(),
            parentTagHelperTypeName,
            propertyName,
            boundAttribute.Documentation);
    }

    // Internal for testing
    internal static string ResolveTagHelperTypeName(string propertyName, string? displayName)
    {
        // A BoundAttributeDescriptor does not have a direct reference to its parent TagHelper.
        // However, when it was constructed the parent TagHelper's type name was embedded into
        // its DisplayName. In VSCode we can't use the DisplayName verbatim for descriptions
        // because the DisplayName is typically too long to display properly. Therefore we need
        // to break it apart and then reconstruct it in a reduced format.
        // i.e. this is the format the display name comes in:
        // ReturnTypeName SomeTypeName.SomePropertyName
        //
        // See DefaultBoundAttributeDescriptorBuilder.GetDisplayName() for added detail.

        var displayNameSpan = displayName.AsSpanOrDefault();

        // Search for the first space, which should be immediately after the return type.
        var spaceIndex = displayNameSpan.IndexOf(' ');
        if (spaceIndex < 0)
        {
            return string.Empty;
        }

        // Increment by one to skip over the space.
        displayNameSpan = displayNameSpan[(spaceIndex + 1)..];

        var propertyNameSpan = propertyName.AsSpanOrDefault();

        // Strip off the trailing property name.
        if (displayNameSpan.EndsWith(propertyNameSpan, StringComparison.Ordinal))
        {
            displayNameSpan = displayNameSpan[..^propertyNameSpan.Length];
        }

        // Strip off the trailing '.'
        if (displayNameSpan is [.. var start, '.'])
        {
            displayNameSpan = start;
        }

        return displayNameSpan.ToString();
    }
}
