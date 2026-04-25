// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class RefTagHelperProducer : TagHelperProducer
{
    private static readonly Lazy<TagHelperDescriptor> s_refTagHelper = new(CreateRefTagHelper);

    private readonly INamedTypeSymbol _elementReferenceType;

    private RefTagHelperProducer(INamedTypeSymbol elementReferenceType)
    {
        _elementReferenceType = elementReferenceType;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.Ref;

    public override bool SupportsStaticTagHelpers => true;

    public override void AddStaticTagHelpers(IAssemblySymbol assembly, ref TagHelperCollection.RefBuilder results)
    {
        if (!SymbolEqualityComparer.Default.Equals(assembly, _elementReferenceType.ContainingAssembly))
        {
            return;
        }

        results.Add(s_refTagHelper.Value);
    }

    private static TagHelperDescriptor CreateRefTagHelper()
    {
        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            TagHelperKind.Ref, "Ref", ComponentsApi.AssemblyName,
            out var builder);

        builder.SetTypeName(
            fullName: "Microsoft.AspNetCore.Components.Ref",
            typeNamespace: "Microsoft.AspNetCore.Components",
            typeNameIdentifier: "Ref");

        builder.CaseSensitive = true;
        builder.ClassifyAttributesOnly = true;
        builder.SetDocumentation(DocumentationDescriptor.RefTagHelper);

        builder.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.Attribute(attribute =>
            {
                attribute.Name = "@ref";
                attribute.IsDirectiveAttribute = true;
            });
        });

        builder.BindAttribute(attribute =>
        {
            attribute.SetDocumentation(DocumentationDescriptor.RefTagHelper);
            attribute.Name = "@ref";

            attribute.TypeName = typeof(object).FullName;
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "Ref";
        });

        return builder.Build();
    }
}
