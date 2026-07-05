// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class TagHelperDescriptorExtensions
{
    public static bool IsAnyComponentDocumentTagHelper(this TagHelperDescriptor tagHelper)
        => tagHelper.Kind.IsAnyComponentKind;

    public static bool IsComponentOrChildContentTagHelper(this TagHelperDescriptor tagHelper)
        => tagHelper.Kind.IsComponentOrChildContentKind;

    public static bool IsFallbackBindTagHelper(this TagHelperDescriptor tagHelper)
        => tagHelper is
        {
            Kind: TagHelperKind.Bind,
            Metadata: BindMetadata { IsFallback: true }
        };

    public static bool IsGenericTypedComponent(this TagHelperDescriptor tagHelper)
        => tagHelper is
        {
            Kind: TagHelperKind.Component,
            Metadata: ComponentMetadata { IsGeneric: true }
        };

    /// <summary>
    /// Given a taghelper binding it finds the BoundAttribute that is a type parameter and then the
    /// actual binding value for that type.
    ///
    /// <code>
    /// &lt;MyTagHelper
    ///   TItem="string"
    ///   OnChange="OnMyTagHelperChange" /&gt;
    /// </code>
    ///
    /// The above code will return "string" for the typeName.
    /// </summary>
    /// <remarks>
    /// As of now this method only supports cases where there is a single bound attribute that is a type parameter. If there are multiple this returns false.
    /// </remarks>
    public static bool TryGetGenericTypeNameFromComponent(this TagHelperDescriptor tagHelper, TagHelperBinding binding, [NotNullWhen(true)] out string? typeName)
    {
        typeName = null;

        if (tagHelper.Kind != TagHelperKind.Component)
        {
            return false;
        }

        foreach (var boundAttribute in tagHelper.BoundAttributes)
        {
            // This is a bit of a headache so let me explain:
            // The bound attribute needs to be marked "True" for the "TypeParameter" key in order to be considered a type parameter.
            // The property name for that is the actual property we need to read, such as "TItem".
            // However, since you can't get the value from the TagHelperDescriptor directly (it's the type, not what the user has provided data to map)
            // it has to be looked up in the bindingAttributes to find the value for that type. This assumes that the type is valid because the user
            // provided it, and if it's not the calling context probably doesn't care.
            if (boundAttribute.IsTypeParameterProperty() &&
                binding.Attributes.FirstOrDefault(boundAttribute.PropertyName, static (kvp, propertyName) => kvp.Key == propertyName) is { Value: var bindingTypeName })
            {
                if (typeName is not null)
                {
                    // Multiple generic types were found and currently not supported.
                    typeName = null;
                    return false;
                }

                typeName = bindingTypeName;
            }
        }

        return typeName is not null;
    }

    public static bool IsInputElementBindTagHelper(this TagHelperDescriptor tagHelper)
        => tagHelper is
        {
            Kind: TagHelperKind.Bind,
            TagMatchingRules: [{ TagName: "input" }, _]
        };

    public static bool IsInputElementFallbackBindTagHelper(this TagHelperDescriptor tagHelper)
        => tagHelper.IsInputElementBindTagHelper() &&
           tagHelper.Metadata is not BindMetadata { TypeAttribute: not null };

    public static string? GetValueAttributeName(this TagHelperDescriptor tagHelper)
        => tagHelper.Metadata is BindMetadata { ValueAttribute: var value } ? value : null;

    public static string? GetChangeAttributeName(this TagHelperDescriptor tagHelper)
        => tagHelper.Metadata is BindMetadata { ChangeAttribute: var value } ? value : null;

    public static string? GetExpressionAttributeName(this TagHelperDescriptor tagHelper)
        => tagHelper.Metadata is BindMetadata { ExpressionAttribute: var value } ? value : null;

    /// <summary>
    /// Gets a value that indicates where the tag helper is a bind tag helper with a default
    /// culture value of <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>
    /// <c>true</c> if this tag helper is a bind tag helper and defaults in <see cref="CultureInfo.InvariantCulture"/>
    /// </returns>
    public static bool IsInvariantCultureBindTagHelper(this TagHelperDescriptor tagHelper)
        => tagHelper.Metadata is BindMetadata { IsInvariantCulture: true };

    /// <summary>
    /// Gets the default format value for a bind tag helper.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>The format, or <c>null</c>.</returns>
    public static string? GetFormat(this TagHelperDescriptor tagHelper)
        => tagHelper.Metadata is BindMetadata { Format: var value } ? value : null;

    public static string? GetEventArgsType(this TagHelperDescriptor tagHelper)
        => tagHelper.Metadata is EventHandlerMetadata { EventArgsType: var value } ? value : null;

    /// <summary>
    /// Gets the set of component attributes that can accept child content (<c>RenderFragment</c> or <c>RenderFragment{T}</c>).
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>The child content attributes</returns>
    public static IEnumerable<BoundAttributeDescriptor> GetChildContentProperties(this TagHelperDescriptor tagHelper)
    {
        foreach (var attribute in tagHelper.BoundAttributes)
        {
            if (attribute.IsChildContentProperty())
            {
                yield return attribute;
            }
        }
    }

    /// <summary>
    /// Gets the set of component attributes that represent generic type parameters of the component type.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>The type parameter attributes</returns>
    public static IEnumerable<BoundAttributeDescriptor> GetTypeParameters(this TagHelperDescriptor tagHelper)
    {
        foreach (var attribute in tagHelper.BoundAttributes)
        {
            if (attribute.IsTypeParameterProperty())
            {
                yield return attribute;
            }
        }
    }

    /// <summary>
    /// Gets a flag that indicates whether the corresponding component supplies any cascading
    /// generic type parameters to descendants.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>True if it does supply one or more generic type parameters to descendants; false otherwise.</returns>
    public static bool SuppliesCascadingGenericParameters(this TagHelperDescriptor tagHelper)
    {
        foreach (var attribute in tagHelper.BoundAttributes)
        {
            if (attribute.IsCascadingTypeParameterProperty())
            {
                return true;
            }
        }

        return false;
    }
}
