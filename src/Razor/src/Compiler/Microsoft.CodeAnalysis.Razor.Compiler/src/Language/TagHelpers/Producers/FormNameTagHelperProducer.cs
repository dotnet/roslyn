// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class FormNameTagHelperProducer : TagHelperProducer
{
    private static readonly Lazy<TagHelperDescriptor> s_formNameTagHelper = new(CreateFormNameTagHelper);

    private readonly INamedTypeSymbol _renderTreeBuilderType;

    private FormNameTagHelperProducer(INamedTypeSymbol renderTreeBuilderType)
    {
        _renderTreeBuilderType = renderTreeBuilderType;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.FormName;

    public override bool SupportsStaticTagHelpers => true;

    public override void AddStaticTagHelpers(IAssemblySymbol assembly, ref TagHelperCollection.RefBuilder results)
    {
        if (assembly.Name != ComponentsApi.AssemblyName &&
            !SymbolEqualityComparer.Default.Equals(assembly, _renderTreeBuilderType.ContainingAssembly))
        {
            return;
        }

        results.Add(s_formNameTagHelper.Value);
    }

    private static TagHelperDescriptor CreateFormNameTagHelper()
    {
        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            kind: TagHelperKind.FormName,
            name: "FormName",
            assemblyName: ComponentsApi.AssemblyName,
            builder: out var builder);

        builder.SetTypeName(
            fullName: "Microsoft.AspNetCore.Components.FormName",
            typeNamespace: "Microsoft.AspNetCore.Components",
            typeNameIdentifier: "FormName");

        builder.CaseSensitive = true;
        builder.ClassifyAttributesOnly = true;
        builder.SetDocumentation(DocumentationDescriptor.FormNameTagHelper);

        builder.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.Attribute(attribute =>
            {
                attribute.Name = "@formname";
                attribute.IsDirectiveAttribute = true;
            });
        });

        builder.BindAttribute(attribute =>
        {
            attribute.SetDocumentation(DocumentationDescriptor.FormNameTagHelper);
            attribute.Name = "@formname";

            attribute.TypeName = typeof(string).FullName;
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "FormName";
        });

        return builder.Build();
    }
}
