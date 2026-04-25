// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class SplatTagHelperProducer : TagHelperProducer
{
    private static readonly Lazy<TagHelperDescriptor> s_splatTagHelper = new(CreateSplatTagHelper);

    private readonly INamedTypeSymbol _renderTreeBuilderType;

    private SplatTagHelperProducer(INamedTypeSymbol renderTreeBuilderType)
    {
        _renderTreeBuilderType = renderTreeBuilderType;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.Splat;

    public override bool SupportsStaticTagHelpers => true;

    public override void AddStaticTagHelpers(IAssemblySymbol assembly, ref TagHelperCollection.RefBuilder results)
    {
        if (!SymbolEqualityComparer.Default.Equals(assembly, _renderTreeBuilderType.ContainingAssembly))
        {
            return;
        }

        results.Add(s_splatTagHelper.Value);
    }

    private static TagHelperDescriptor CreateSplatTagHelper()
    {
        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            TagHelperKind.Splat, "Attributes", ComponentsApi.AssemblyName,
            out var builder);

        builder.SetTypeName(
            fullName: "Microsoft.AspNetCore.Components.Attributes",
            typeNamespace: "Microsoft.AspNetCore.Components",
            typeNameIdentifier: "Attributes");

        builder.CaseSensitive = true;
        builder.ClassifyAttributesOnly = true;
        builder.SetDocumentation(DocumentationDescriptor.SplatTagHelper);

        builder.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.Attribute(attribute =>
            {
                attribute.Name = "@attributes";
                attribute.IsDirectiveAttribute = true;
            });
        });

        builder.BindAttribute(attribute =>
        {
            attribute.SetDocumentation(DocumentationDescriptor.SplatTagHelper);
            attribute.Name = "@attributes";

            attribute.TypeName = typeof(object).FullName;
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "Attributes";
        });

        return builder.Build();
    }
}
