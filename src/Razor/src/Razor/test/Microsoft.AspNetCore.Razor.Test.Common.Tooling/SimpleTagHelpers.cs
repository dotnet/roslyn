// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class SimpleTagHelpers
{
    public static TagHelperCollection Default { get; }

    static SimpleTagHelpers()
    {
        var builder1 = TagHelperDescriptorBuilder.CreateTagHelper("Test1TagHelper", "TestAssembly");
        builder1.TypeName = "Test1TagHelper";
        builder1.TagMatchingRule(rule => rule.TagName = "test1");
        builder1.BindAttribute(attribute =>
        {
            attribute.Name = "bool-val";
            attribute.PropertyName = "BoolVal";
            attribute.TypeName = typeof(bool).FullName;
        });
        builder1.BindAttribute(attribute =>
        {
            attribute.Name = "int-val";
            attribute.PropertyName = "IntVal";
            attribute.TypeName = typeof(int).FullName;
        });

        var builder1WithRequiredParent = TagHelperDescriptorBuilder.CreateTagHelper("Test1TagHelper.SomeChild", "TestAssembly");
        builder1WithRequiredParent.TypeName = "Test1TagHelper.SomeChild";
        builder1WithRequiredParent.TagMatchingRule(rule =>
        {
            rule.TagName = "SomeChild";
            rule.ParentTag = "test1";
        });
        builder1WithRequiredParent.BindAttribute(attribute =>
        {
            attribute.Name = "attribute";
            attribute.PropertyName = "Attribute";
            attribute.TypeName = typeof(string).FullName;
        });

        var builder2 = TagHelperDescriptorBuilder.CreateTagHelper("Test2TagHelper", "TestAssembly");
        builder2.TypeName = "Test2TagHelper";
        builder2.TagMatchingRule(rule => rule.TagName = "test2");
        builder2.BindAttribute(attribute =>
        {
            attribute.Name = "bool-val";
            attribute.PropertyName = "BoolVal";
            attribute.TypeName = typeof(bool).FullName;
        });
        builder2.BindAttribute(attribute =>
        {
            attribute.Name = "int-val";
            attribute.PropertyName = "IntVal";
            attribute.TypeName = typeof(int).FullName;
        });

        var builder3 = TagHelperDescriptorBuilder.CreateComponent("Component1TagHelper", "TestAssembly");
        builder3.SetTypeName(
            fullName: "System.Component1",
            typeNamespace: "System",
            typeNameIdentifier: "Component1");
        builder3.TagMatchingRule(rule => rule.TagName = "Component1");
        builder3.IsFullyQualifiedNameMatch = true;
        builder3.BindAttribute(attribute =>
        {
            attribute.Name = "bool-val";
            attribute.PropertyName = "BoolVal";
            attribute.TypeName = typeof(bool).FullName;
        });
        builder3.BindAttribute(attribute =>
        {
            attribute.Name = "int-val";
            attribute.PropertyName = "IntVal";
            attribute.TypeName = typeof(int).FullName;
        });
        builder3.BindAttribute(attribute =>
        {
            attribute.Name = "Title";
            attribute.PropertyName = "Title";
            attribute.TypeName = typeof(string).FullName;
        });

        var textComponent = TagHelperDescriptorBuilder.CreateComponent("TextTagHelper", "TestAssembly");
        textComponent.SetTypeName(
            fullName: "System.Text",
            typeNamespace: "System",
            typeNameIdentifier: "Text");
        textComponent.TagMatchingRule(rule => rule.TagName = "Text");
        textComponent.IsFullyQualifiedNameMatch = true;

        var directiveAttribute1 = TagHelperDescriptorBuilder.CreateComponent("TestDirectiveAttribute", "TestAssembly");
        directiveAttribute1.TypeName = "TestDirectiveAttribute";
        directiveAttribute1.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@test";
                b.NameComparison = RequiredAttributeNameComparison.PrefixMatch;
            });
        });
        directiveAttribute1.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@test";
                b.NameComparison = RequiredAttributeNameComparison.FullMatch;
            });
        });
        directiveAttribute1.BindAttribute(attribute =>
        {
            attribute.Name = "@test";
            attribute.PropertyName = "Test";
            attribute.IsDirectiveAttribute = true;
            attribute.TypeName = typeof(string).FullName;

            attribute.BindAttributeParameter(parameter =>
            {
                parameter.Name = "something";
                parameter.PropertyName = "Something";
                parameter.TypeName = typeof(string).FullName;
            });
        });
        directiveAttribute1.IsFullyQualifiedNameMatch = true;
        directiveAttribute1.ClassifyAttributesOnly = true;

        var directiveAttribute2 = TagHelperDescriptorBuilder.CreateComponent("MinimizedDirectiveAttribute", "TestAssembly");
        directiveAttribute2.TypeName = "TestDirectiveAttribute";
        directiveAttribute2.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@minimized";
                b.NameComparison = RequiredAttributeNameComparison.PrefixMatch;
            });
        });
        directiveAttribute2.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(b =>
            {
                b.Name = "@minimized";
                b.NameComparison = RequiredAttributeNameComparison.FullMatch;
            });
        });
        directiveAttribute2.BindAttribute(attribute =>
        {
            attribute.Name = "@minimized";
            attribute.IsDirectiveAttribute = true;
            attribute.PropertyName = "Minimized";
            attribute.TypeName = typeof(bool).FullName;

            attribute.BindAttributeParameter(parameter =>
            {
                parameter.Name = "something";
                parameter.PropertyName = "Something";
                parameter.TypeName = typeof(string).FullName;
            });
        });
        directiveAttribute2.IsFullyQualifiedNameMatch = true;
        directiveAttribute2.ClassifyAttributesOnly = true;

        var directiveAttribute3 = TagHelperDescriptorBuilder.CreateEventHandler("OnClickDirectiveAttribute", "TestAssembly");
        directiveAttribute3.SetTypeName(
            fullName: "Microsoft.AspNetCore.Components.Web.EventHandlers",
            typeNamespace: "Microsoft.AspNetCore.Components.Web",
            typeNameIdentifier: "EventHandlers");
        directiveAttribute3.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(attribute => attribute
                .Name("@onclick", RequiredAttributeNameComparison.FullMatch)
                .IsDirectiveAttribute());
        });
        directiveAttribute3.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.RequireAttributeDescriptor(attribute => attribute
                .Name("@onclick", RequiredAttributeNameComparison.PrefixMatch)
                .IsDirectiveAttribute());
        });
        directiveAttribute3.BindAttribute(attribute =>
        {
            attribute.Name = "@onclick";
            attribute.PropertyName = "onclick";
            attribute.IsWeaklyTyped = true;
            attribute.IsDirectiveAttribute = true;
            attribute.TypeName = "Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>";
        });
        directiveAttribute3.IsFullyQualifiedNameMatch = true;
        directiveAttribute3.ClassifyAttributesOnly = true;
        directiveAttribute3.SetMetadata(new EventHandlerMetadata()
        {
            EventArgsType = "Microsoft.AspNetCore.Components.Web.MouseEventArgs"
        });

        var htmlTagMutator = TagHelperDescriptorBuilder.CreateTagHelper("HtmlMutator", "TestAssembly");
        htmlTagMutator.TagMatchingRule(rule =>
        {
            rule.TagName = "title";
            rule.RequireAttributeDescriptor(attributeRule =>
            {
                attributeRule.Name = "mutator";
            });
        });
        htmlTagMutator.TypeName = "HtmlMutator";
        htmlTagMutator.BindAttribute(attribute =>
        {
            attribute.Name = "Extra";
            attribute.PropertyName = "Extra";
            attribute.TypeName = typeof(bool).FullName;
        });

        Default =
        [
            builder1.Build(),
            builder1WithRequiredParent.Build(),
            builder2.Build(),
            builder3.Build(),
            textComponent.Build(),
            directiveAttribute1.Build(),
            directiveAttribute2.Build(),
            directiveAttribute3.Build(),
            htmlTagMutator.Build(),
        ];
    }
}
