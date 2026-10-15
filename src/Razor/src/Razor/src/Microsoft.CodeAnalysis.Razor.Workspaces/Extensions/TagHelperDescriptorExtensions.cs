// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class TagHelperDescriptorExtensions
{
    public static bool IsAttributeDescriptor(this TagHelperDescriptor descriptor)
        => descriptor.ClassifyAttributesOnly;

    public static string? TryGetComponentTag(this TagHelperDescriptor descriptor, RazorCodeDocument codeDocument)
    {
        // Non-component documents can't bind component tags, so let the caller fall back to HTML.
        if (!FileKinds.IsComponent(codeDocument.FileKind))
        {
            return null;
        }

        var typeName = descriptor.TypeNameIdentifier;
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        var binding = codeDocument.GetRequiredTagHelperContext().GetBinder()
            .GetBinding(typeName, attributes: [], parentTagName: null, parentIsTagHelper: false);
        var potentialMatchCount = 0;
        var matchesDroppedComponent = false;
        if (binding is not null)
        {
            foreach (var tagHelper in binding.TagHelpers)
            {
                if (tagHelper.Kind == TagHelperKind.Component)
                {
                    potentialMatchCount++;
                    matchesDroppedComponent |= tagHelper.TypeName == descriptor.TypeName &&
                        tagHelper.AssemblyName == descriptor.AssemblyName;
                }
            }
        }

        // A short tag is safe only when it resolves uniquely to the component being dropped.
        // This is best-effort: even if we know we can't use a short tag, if we don't know a
        // namespace to qualify it, leave it unqualified for the user to fix.
        var canUseShortName = potentialMatchCount == 1 && matchesDroppedComponent;
        if (!canUseShortName &&
            descriptor.TypeNamespace is { Length: > 0 } typeNamespace)
        {
            typeName = $"{typeNamespace}.{typeName}";
        }

        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        builder.Append('<');
        builder.Append(typeName);

        foreach (var requiredAttribute in descriptor.EditorRequiredAttributes)
        {
            builder.Append(' ');
            builder.Append(requiredAttribute.Name);
            builder.Append("=\"\"");
        }

        if (descriptor.AllowedChildTags.Length > 0)
        {
            builder.Append("></");
            builder.Append(typeName);
            builder.Append('>');
        }
        else
        {
            builder.Append(" />");
        }

        return builder.ToString();
    }
}
