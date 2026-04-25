// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class RenderModeTagHelperProducer : TagHelperProducer
{
    private static readonly Lazy<TagHelperDescriptor> s_renderModeTagHelper = new(CreateRenderModeTagHelper);

    private readonly INamedTypeSymbol _iComponentRenderModeType;

    private RenderModeTagHelperProducer(INamedTypeSymbol iComponentRenderModeType)
    {
        _iComponentRenderModeType = iComponentRenderModeType;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.RenderMode;

    public override bool SupportsStaticTagHelpers => true;

    public override void AddStaticTagHelpers(IAssemblySymbol assembly, ref TagHelperCollection.RefBuilder results)
    {
        if (!SymbolEqualityComparer.Default.Equals(assembly, _iComponentRenderModeType.ContainingAssembly))
        {
            return;
        }

        results.Add(s_renderModeTagHelper.Value);
    }

    private static TagHelperDescriptor CreateRenderModeTagHelper()
    {
        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            TagHelperKind.RenderMode, "RenderMode", ComponentsApi.AssemblyName,
            out var builder);

        builder.SetTypeName(
            fullName: "Microsoft.AspNetCore.Components.RenderMode",
            typeNamespace: "Microsoft.AspNetCore.Components",
            typeNameIdentifier: "RenderMode");

        builder.CaseSensitive = true;
        builder.ClassifyAttributesOnly = true;
        builder.SetDocumentation(DocumentationDescriptor.RenderModeTagHelper);

        builder.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.Attribute(attribute =>
            {
                attribute.Name = "@rendermode";
                attribute.IsDirectiveAttribute = true;
            });
        });

        builder.BindAttribute(attribute =>
        {
            attribute.SetDocumentation(DocumentationDescriptor.RenderModeTagHelper);
            attribute.Name = "@rendermode";

            attribute.TypeName = ComponentsApi.IComponentRenderMode.FullTypeName;
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "RenderMode";
        });

        return builder.Build();
    }
}
