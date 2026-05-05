// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class TagHelperBoundAttributeDescriptorExtensions
{
    public static bool IsDelegateProperty(this BoundAttributeDescriptor attribute)
        => attribute.Metadata is PropertyMetadata { IsDelegateSignature: true };

    public static bool IsDelegateWithAwaitableResult(this BoundAttributeDescriptor attribute)
        => attribute.Metadata is PropertyMetadata { IsDelegateWithAwaitableResult: true };

    /// <summary>
    /// Gets a value indicating whether the attribute is of type <c>EventCallback</c> or
    /// <c>EventCallback{T}</c>
    /// </summary>
    /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
    /// <returns><c>true</c> if the attribute is an event callback, otherwise <c>false</c>.</returns>
    public static bool IsEventCallbackProperty(this BoundAttributeDescriptor attribute)
        => attribute.Metadata is PropertyMetadata { IsEventCallback: true };

    public static bool IsGenericTypedProperty(this BoundAttributeDescriptor attribute)
        => attribute.Metadata is PropertyMetadata { IsGenericTyped: true };

    public static bool IsTypeParameterProperty(this BoundAttributeDescriptor attribute)
        => attribute.Metadata.Kind == MetadataKind.TypeParameter;

    public static bool IsCascadingTypeParameterProperty(this BoundAttributeDescriptor attribute)
        => attribute.Metadata is TypeParameterMetadata { IsCascading: true };

    /// <summary>
    /// Gets a value that indicates whether the property is a child content property. Properties are
    /// considered child content if they have the type <c>RenderFragment</c> or <c>RenderFragment{T}</c>.
    /// </summary>
    /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
    /// <returns>Returns <c>true</c> if the property is child content, otherwise <c>false</c>.</returns>
    public static bool IsChildContentProperty(this BoundAttributeDescriptor attribute)
        => attribute.Metadata is PropertyMetadata { IsChildContent: true };

    /// <summary>
    /// Gets a value that indicates whether the property is a child content property. Properties are
    /// considered child content if they have the type <c>RenderFragment</c> or <c>RenderFragment{T}</c>.
    /// </summary>
    /// <param name="builder">The <see cref="BoundAttributeDescriptorBuilder"/>.</param>
    /// <returns>Returns <c>true</c> if the property is child content, otherwise <c>false</c>.</returns>
    public static bool IsChildContentProperty(this BoundAttributeDescriptorBuilder builder)
        => builder.MetadataObject is PropertyMetadata { IsChildContent: true };

    /// <summary>
    /// Gets a value that indicates whether the property is a parameterized child content property. Properties are
    /// considered parameterized child content if they have the type <c>RenderFragment{T}</c> (for some T).
    /// </summary>
    /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
    /// <returns>Returns <c>true</c> if the property is parameterized child content, otherwise <c>false</c>.</returns>
    public static bool IsParameterizedChildContentProperty(this BoundAttributeDescriptor attribute)
        => attribute.IsChildContentProperty() &&
           attribute.TypeName != ComponentsApi.RenderFragment.FullTypeName;

    /// <summary>
    /// Gets a value that indicates whether the property is a parameterized child content property. Properties are
    /// considered parameterized child content if they have the type <c>RenderFragment{T}</c> (for some T).
    /// </summary>
    /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
    /// <returns>Returns <c>true</c> if the property is parameterized child content, otherwise <c>false</c>.</returns>
    public static bool IsParameterizedChildContentProperty(this BoundAttributeDescriptorBuilder attribute)
        => attribute.IsChildContentProperty() &&
           attribute.TypeName != ComponentsApi.RenderFragment.FullTypeName;

    /// <summary>
    /// Gets a value that indicates whether the property is used to specify the name of the parameter
    /// for a parameterized child content property.
    /// </summary>
    /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
    /// <returns>
    /// Returns <c>true</c> if the property specifies the name of a parameter for a parameterized child content,
    /// otherwise <c>false</c>.
    /// </returns>
    public static bool IsChildContentParameterNameProperty(this BoundAttributeDescriptor attribute)
        => attribute.Metadata is ChildContentParameterMetadata;
}
