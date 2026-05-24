// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class KeyTagHelperProducer : TagHelperProducer
{
    private static readonly Lazy<TagHelperDescriptor> s_keyTagHelper = new(CreateKeyTagHelper);

    private readonly INamedTypeSymbol _renderTreeBuilderType;

    private KeyTagHelperProducer(INamedTypeSymbol renderTreeBuilderType)
    {
        _renderTreeBuilderType = renderTreeBuilderType;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.Key;

    public override bool SupportsStaticTagHelpers => true;

    public override void AddStaticTagHelpers(IAssemblySymbol assembly, ref TagHelperCollection.RefBuilder results)
    {
        if (!SymbolEqualityComparer.Default.Equals(assembly, _renderTreeBuilderType.ContainingAssembly))
        {
            return;
        }

        results.Add(s_keyTagHelper.Value);
    }

    private static TagHelperDescriptor CreateKeyTagHelper()
    {
        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            TagHelperKind.Key, "Key", ComponentsApi.AssemblyName,
            out var builder);

        builder.SetTypeName(
            fullName: "Microsoft.AspNetCore.Components.Key",
            typeNamespace: "Microsoft.AspNetCore.Components",
            typeNameIdentifier: "Key");

        builder.CaseSensitive = true;
        builder.ClassifyAttributesOnly = true;
        builder.SetDocumentation(DocumentationDescriptor.KeyTagHelper);

        builder.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.Attribute(attribute =>
            {
                attribute.Name = "@key";
                attribute.IsDirectiveAttribute = true;
            });
        });

        builder.BindAttribute(attribute =>
        {
            attribute.SetDocumentation(DocumentationDescriptor.KeyTagHelper);
            attribute.Name = "@key";

            attribute.TypeName = typeof(object).FullName;
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "Key";
        });

        return builder.Build();
    }
}
