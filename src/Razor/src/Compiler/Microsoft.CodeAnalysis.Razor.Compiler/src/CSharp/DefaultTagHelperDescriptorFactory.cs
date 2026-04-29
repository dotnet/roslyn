// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor;

internal sealed class DefaultTagHelperDescriptorFactory(bool includeDocumentation, bool excludeHidden)
{
    private const string TagHelperNameEnding = "TagHelper";

    private readonly bool _excludeHidden = excludeHidden;
    private readonly bool _includeDocumentation = includeDocumentation;

    public TagHelperDescriptor? CreateDescriptor(INamedTypeSymbol type)
    {
        ArgHelper.ThrowIfNull(type);

        if (ShouldSkipDescriptorCreation(type))
        {
            return null;
        }

        var typeName = TypeNameObject.From(type);
        var assemblyName = type.ContainingAssembly.Identity.Name;

        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            typeName.FullName.AssumeNotNull(), assemblyName,
            out var descriptorBuilder);

        descriptorBuilder.SetTypeName(typeName);
        descriptorBuilder.RuntimeKind = RuntimeKind.ITagHelper;

        AddBoundAttributes(type, descriptorBuilder);
        AddTagMatchingRules(type, descriptorBuilder);
        AddAllowedChildren(type, descriptorBuilder);
        AddDocumentation(type, descriptorBuilder);
        AddTagOutputHint(type, descriptorBuilder);

        return descriptorBuilder.Build();
    }

    private bool ShouldSkipDescriptorCreation(ISymbol symbol)
    {
        if (!_excludeHidden)
        {
            return false;
        }

        if (!symbol.TryGetAttribute(typeof(EditorBrowsableAttribute).FullName!, out var editorBrowsableAttribute))
        {
            return false;
        }

        // We need to be careful with pattern matching below because TypedConstant.Value
        // is an object and not an enum value.
        return editorBrowsableAttribute?.ConstructorArguments is [{ Value: object value }, ..] &&
               (EditorBrowsableState)value == EditorBrowsableState.Never;
    }

    private static void AddTagMatchingRules(INamedTypeSymbol type, TagHelperDescriptorBuilder descriptorBuilder)
    {
        using var targetElementAttributes = new PooledArrayBuilder<AttributeData>();

        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.HasFullName(TagHelperTypes.HtmlTargetElementAttribute))
            {
                targetElementAttributes.Add(attribute);
            }
        }

        // If there isn't an attribute specifying the tag name derive it from the name
        if (!targetElementAttributes.Any())
        {
            var name = type.Name;

            if (name.EndsWith(TagHelperNameEnding, StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^TagHelperNameEnding.Length];
            }

            descriptorBuilder.TagMatchingRule(ruleBuilder =>
            {
                var htmlCasedName = HtmlConventions.ToHtmlCase(name);
                ruleBuilder.TagName = htmlCasedName;
            });

            return;
        }

        foreach (var targetElementAttribute in targetElementAttributes)
        {
            descriptorBuilder.TagMatchingRule(ruleBuilder =>
            {
                var tagName = HtmlTargetElementAttribute_Tag(targetElementAttribute);
                ruleBuilder.TagName = tagName;

                var parentTag = HtmlTargetElementAttribute_ParentTag(targetElementAttribute);
                ruleBuilder.ParentTag = parentTag;

                var tagStructure = HtmlTargetElementAttribute_TagStructure(targetElementAttribute);
                ruleBuilder.TagStructure = tagStructure;

                var requiredAttributeString = HtmlTargetElementAttribute_Attributes(targetElementAttribute);

                if (requiredAttributeString is not null)
                {
                    RequiredAttributeParser.AddRequiredAttributes(requiredAttributeString, ruleBuilder);
                }
            });
        }
    }

    private void AddBoundAttributes(INamedTypeSymbol type, TagHelperDescriptorBuilder builder)
    {
        using var accessibleProperties = new PooledArrayBuilder<IPropertySymbol>();

        CollectAccessibleProperties(type, ref accessibleProperties.AsRef());

        foreach (var property in accessibleProperties)
        {
            if (ShouldSkipDescriptorCreation(property))
            {
                continue;
            }

            builder.BindAttribute(attributeBuilder =>
            {
                ConfigureBoundAttribute(attributeBuilder, property, type);
            });
        }
    }

    private static void AddAllowedChildren(INamedTypeSymbol type, TagHelperDescriptorBuilder builder)
    {
        if (!type.TryGetAttribute(TagHelperTypes.RestrictChildrenAttribute, out var restrictChildrenAttribute))
        {
            return;
        }

        var constructorArguments = restrictChildrenAttribute.ConstructorArguments;

        if (constructorArguments is [var arg0, ..])
        {
            builder.AllowChildTag(childTagBuilder =>
            {
                childTagBuilder.Name = (string?)arg0.Value;
            });

            if (constructorArguments is [_, var arg1, ..])
            {
                foreach (var value in arg1.Values)
                {
                    builder.AllowChildTag(childTagBuilder => childTagBuilder.Name = (string?)value.Value);
                }
            }
        }
    }

    private void AddDocumentation(INamedTypeSymbol type, TagHelperDescriptorBuilder builder)
    {
        if (!_includeDocumentation)
        {
            return;
        }

        var xml = type.GetDocumentationCommentXml();

        if (!string.IsNullOrEmpty(xml))
        {
            builder.SetDocumentation(xml);
        }
    }

    private static void AddTagOutputHint(INamedTypeSymbol type, TagHelperDescriptorBuilder builder)
    {
        if (type.TryGetAttribute(TagHelperTypes.OutputElementHintAttribute, out var attribute) &&
            attribute.ConstructorArguments is [{ Value: string value }, ..])
        {
            builder.TagOutputHint = value;
        }
    }

    private void ConfigureBoundAttribute(
        BoundAttributeDescriptorBuilder builder,
        IPropertySymbol property,
        INamedTypeSymbol containingType)
    {
        var attributeNameAttribute = property.GetAttribute(TagHelperTypes.HtmlAttributeNameAttribute);

        var (hasExplicitName, attributeName) = attributeNameAttribute?.ConstructorArguments is [{ Value: string { Length: > 0 } value }, ..]
            ? (true, value)
            : (false, HtmlConventions.ToHtmlCase(property.Name));

        builder.TypeName = property.Type.GetFullName();
        builder.PropertyName = property.Name;

        var hasPublicSetter = HasPublicSetter(property);

        if (hasPublicSetter)
        {
            builder.Name = attributeName;

            if (property.Type.TypeKind == TypeKind.Enum)
            {
                builder.IsEnum = true;
            }

            if (_includeDocumentation)
            {
                var xml = property.GetDocumentationCommentXml();

                if (!string.IsNullOrEmpty(xml))
                {
                    builder.SetDocumentation(xml);
                }
            }
        }
        else if (hasExplicitName && !IsPotentialDictionaryProperty(property))
        {
            // Specified HtmlAttributeNameAttribute.Name though property has no public setter.
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidAttributeNameNullOrEmpty(containingType.GetFullName(), property.Name);
            builder.Diagnostics.Add(diagnostic);
        }

        ConfigureDictionaryBoundAttribute(builder, property, containingType, attributeNameAttribute, attributeName, hasPublicSetter);
    }

    private static void ConfigureDictionaryBoundAttribute(
        BoundAttributeDescriptorBuilder builder,
        IPropertySymbol property,
        INamedTypeSymbol containingType,
        AttributeData? attributeNameAttribute,
        string attributeName,
        bool hasPublicSetter)
    {
        string? dictionaryAttributePrefix = null;
        var dictionaryAttributePrefixSet = false;

        if (attributeNameAttribute != null)
        {
            foreach (var (name, argument) in attributeNameAttribute.NamedArguments)
            {
                if (name == TagHelperTypes.HtmlAttributeName.DictionaryAttributePrefix)
                {
                    dictionaryAttributePrefix = (string?)argument.Value;
                    dictionaryAttributePrefixSet = true;
                    break;
                }
            }
        }

        var dictionaryTypeArguments = GetDictionaryArgumentTypes(property);

        if (!dictionaryTypeArguments.IsEmpty)
        {
            var prefix = attributeNameAttribute is null || !dictionaryAttributePrefixSet
                ? attributeName + "-"
                : dictionaryAttributePrefix;

            if (prefix != null)
            {
                var dictionaryValueType = dictionaryTypeArguments[1];
                var dictionaryValueTypeName = dictionaryValueType.GetFullName();
                builder.AsDictionary(prefix, dictionaryValueTypeName);
            }
        }

        if (dictionaryTypeArguments is not [{ SpecialType: SpecialType.System_String }, ..])
        {
            if (dictionaryAttributePrefix != null)
            {
                // DictionaryAttributePrefix is not supported unless associated with an
                // IDictionary<string, TValue> property.
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(containingType.GetFullName(), property.Name);
                builder.Diagnostics.Add(diagnostic);
            }

            return;
        }

        if (!hasPublicSetter && attributeNameAttribute != null && !dictionaryAttributePrefixSet)
        {
            // Must set DictionaryAttributePrefix when using HtmlAttributeNameAttribute with a dictionary property
            // that lacks a public setter.
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNull(containingType.GetFullName(), property.Name);
            builder.Diagnostics.Add(diagnostic);
        }
    }

    private static ImmutableArray<ITypeSymbol> GetDictionaryArgumentTypes(IPropertySymbol property)
    {
        if (property.Type is INamedTypeSymbol propertyType &&
            propertyType.ConstructedFrom.HasFullName(TagHelperTypes.IDictionary))
        {
            return propertyType.TypeArguments;
        }

        var dictionaryType = property.Type.AllInterfaces
            .FirstOrDefault(static s => s.ConstructedFrom.HasFullName(TagHelperTypes.IDictionary));

        var result = dictionaryType?.TypeArguments ?? [];

        Debug.Assert(result.Length == 2 || result.Length == 0,
            "Expected IDictionary to have 2 type arguments (key and value) or none if not a dictionary.");

        return result;
    }

    private static string? HtmlTargetElementAttribute_Attributes(AttributeData attribute)
    {
        foreach (var (name, argument) in attribute.NamedArguments)
        {
            if (name == TagHelperTypes.HtmlTargetElement.Attributes)
            {
                return (string?)argument.Value;
            }
        }

        return null;
    }

    private static string? HtmlTargetElementAttribute_ParentTag(AttributeData attribute)
    {
        foreach (var (name, value) in attribute.NamedArguments)
        {
            if (name == TagHelperTypes.HtmlTargetElement.ParentTag)
            {
                return (string?)value.Value;
            }
        }

        return null;
    }

    private static string HtmlTargetElementAttribute_Tag(AttributeData attribute)
    {
        if (attribute.ConstructorArguments is [{ Value: string value }, ..])
        {
            return value;
        }

        return TagHelperMatchingConventions.ElementCatchAllName;
    }

    private static TagStructure HtmlTargetElementAttribute_TagStructure(AttributeData attribute)
    {
        foreach (var (name, argument) in attribute.NamedArguments)
        {
            if (name == TagHelperTypes.HtmlTargetElement.TagStructure &&
                argument is { Value: object value })
            {
                return (TagStructure)value;
            }
        }

        return TagStructure.Unspecified;
    }

    private static bool HasPublicSetter(IPropertySymbol property)
        => property is { SetMethod.DeclaredAccessibility: Accessibility.Public };

    private static bool IsPotentialDictionaryProperty(IPropertySymbol property)
        => GetDictionaryArgumentTypes(property) is [{ SpecialType: SpecialType.System_String }, ..];

    private static void CollectAccessibleProperties(
        INamedTypeSymbol typeSymbol, ref PooledArrayBuilder<IPropertySymbol> properties)
    {
        using var names = new PooledHashSet<string>(StringComparer.Ordinal);

        // Traverse the type hierarchy to find all accessible properties.
        var currentType = typeSymbol;

        do
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is not IPropertySymbol property ||
                    !IsAccessibleProperty(property) ||
                    !names.Add(property.Name))
                {
                    continue;
                }

                properties.Add(property);
            }

            currentType = currentType.BaseType;
        }
        while (currentType != null);
    }

    private static bool IsAccessibleProperty(IPropertySymbol property)
    {
        // First, the property must have a public getter and no parameters.
        if (property is not { GetMethod.DeclaredAccessibility: Accessibility.Public, Parameters: [] })
        {
            return false;
        }

        var foundHtmlAttributeNameAttribute = false;

        foreach (var attribute in property.GetAttributes())
        {
            if (attribute.HasFullName(TagHelperTypes.HtmlAttributeNotBoundAttribute))
            {
                // If the property has a HtmlAttributeNotBoundAttribute, it should not be considered for binding.
                return false;
            }

            if (!foundHtmlAttributeNameAttribute &&
                attribute.HasFullName(TagHelperTypes.HtmlAttributeNameAttribute))
            {
                foundHtmlAttributeNameAttribute = true;
            }
        }

        // Finally, the property must either have a HtmlAttributeNameAttribute, a public setter,
        // or be a potential dictionary property.

        return foundHtmlAttributeNameAttribute ||
               property is { SetMethod.DeclaredAccessibility: Accessibility.Public } ||
               IsPotentialDictionaryProperty(property);
    }
}
