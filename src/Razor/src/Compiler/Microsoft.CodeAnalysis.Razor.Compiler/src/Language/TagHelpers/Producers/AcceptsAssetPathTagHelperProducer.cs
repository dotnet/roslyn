// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

// Reads the runtime's [AcceptsAssetPath(elementName, attributeName)] declarations and produces a
// carrier TagHelperDescriptor per element/attribute pair. These descriptors never match an
// element (they declare no tag-matching rules); they exist purely so ComponentTildePathPass
// can discover which element/attribute combinations opt into '~/' asset-path expansion. The
// declarations live on a public convention type named AssetPathAttributes.
internal sealed partial class AcceptsAssetPathTagHelperProducer : TagHelperProducer
{
    private readonly INamedTypeSymbol _acceptsAssetPathAttributeType;

    private AcceptsAssetPathTagHelperProducer(INamedTypeSymbol acceptsAssetPathAttributeType)
    {
        _acceptsAssetPathAttributeType = acceptsAssetPathAttributeType;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.AcceptsAssetPath;

    public override bool SupportsTypes => true;

    public override bool IsCandidateType(INamedTypeSymbol type)
        => type.DeclaredAccessibility == Accessibility.Public &&
           type.Name == ComponentsApi.AcceptsAssetPathAttribute.CandidateTypeName;

    public override void AddTagHelpersForType(
        INamedTypeSymbol type,
        ref TagHelperCollection.RefBuilder results,
        CancellationToken cancellationToken)
    {
        var typeName = type.GetDefaultDisplayString();
        var namespaceName = type.ContainingNamespace.GetFullName();

        foreach (var attribute in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _acceptsAssetPathAttributeType) &&
                TryGetArgs(attribute, out var element, out var attributeName))
            {
                results.Add(CreateTagHelper(typeName, namespaceName, type.Name, element, attributeName));
            }
        }
    }

    private static bool TryGetArgs(AttributeData attribute, out string element, out string attributeName)
    {
        // AcceptsAssetPathAttribute(string element, string attribute)
        if (attribute.ConstructorArguments is [
            { Value: string elementValue },
            { Value: string attributeValue }])
        {
            element = elementValue;
            attributeName = attributeValue;
            return true;
        }

        element = null!;
        attributeName = null!;
        return false;
    }

    private static TagHelperDescriptor CreateTagHelper(
        string typeName,
        string typeNamespace,
        string typeNameIdentifier,
        string element,
        string attribute)
    {
        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            TagHelperKind.AssetPath, $"{element}[{attribute}]", ComponentsApi.AssemblyName,
            out var builder);

        builder.SetTypeName(typeName, typeNamespace, typeNameIdentifier);

        builder.CaseSensitive = true;

        builder.SetMetadata(new AssetPathMetadata()
        {
            Element = element,
            Attribute = attribute,
        });

        return builder.Build();
    }
}
