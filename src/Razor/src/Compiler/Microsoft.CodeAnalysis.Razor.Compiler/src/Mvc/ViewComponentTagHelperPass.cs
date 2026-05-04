// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public sealed class ViewComponentTagHelperPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    // Run after the default tag helper pass
    public override int Order => DefaultFeatureOrder + 2000;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (documentNode.DocumentKind != RazorPageDocumentClassifierPass.RazorPageDocumentKind &&
            documentNode.DocumentKind != MvcViewDocumentClassifierPass.MvcViewDocumentKind)
        {
            // Not a MVC file. Skip.
            return;
        }

        var namespaceNode = documentNode.FindPrimaryNamespace();
        var classNode = documentNode.FindPrimaryClass();
        if (namespaceNode == null || classNode == null)
        {
            // Nothing to do, bail. We can't function without the standard structure.
            return;
        }

        var context = new Context(namespaceNode, classNode);

        // For each VCTH *usage* we need to rewrite the tag helper node to use the tag helper runtime to construct
        // and set properties on the the correct field, and using the name of the type we will generate.
        foreach (var node in documentNode.FindDescendantNodes<TagHelperIntermediateNode>())
        {
            foreach (var tagHelper in node.TagHelpers)
            {
                RewriteUsage(context, node, tagHelper);
            }
        }

        // Then for each VCTH *definition* that we've seen we need to generate the class that implements
        // ITagHelper and the field that will hold it.
        foreach (var tagHelper in context.TagHelpers)
        {
            AddField(context, tagHelper);
            AddTagHelperClass(context, tagHelper);
        }
    }

    private static void RewriteUsage(Context context, TagHelperIntermediateNode node, TagHelperDescriptor tagHelper)
    {
        if (!tagHelper.IsViewComponentKind)
        {
            return;
        }

        context.Add(tagHelper);

        // Now we need to insert a create node using the default tag helper runtime. This is similar to
        // code in DefaultTagHelperOptimizationPass.
        //
        // Find the body node.

        var children = node.Children;
        var index = 0;

        SkipNodes<TagHelperBodyIntermediateNode>(children, ref index);
        SkipNodes<DefaultTagHelperBodyIntermediateNode>(children, ref index);

        // Now find the last create node.
        SkipNodes<DefaultTagHelperCreateIntermediateNode>(children, ref index);

        // Now 'index' has the right insertion point.
        children.Insert(index, new DefaultTagHelperCreateIntermediateNode()
        {
            FieldName = context.GetFieldName(tagHelper),
            TagHelper = tagHelper,
            TypeName = context.GetFullyQualifiedName(tagHelper),
        });

        // Now we need to rewrite any set property nodes to use the default runtime.
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is TagHelperPropertyIntermediateNode propertyNode &&
                propertyNode.TagHelper == tagHelper)
            {
                // This is a set property for this VCTH - we need to replace it with a node
                // that will use our field and property name.
                children[i] = new DefaultTagHelperPropertyIntermediateNode(propertyNode)
                {
                    FieldName = context.GetFieldName(tagHelper),
                    PropertyName = propertyNode.BoundAttribute.PropertyName,
                };
            }
        }
    }

    private static void AddField(Context context, TagHelperDescriptor tagHelper)
    {
        // We need to insert a node for the field that will hold the tag helper. We've already generated a field name
        // at this time and use it for all uses of the same tag helper type.
        //
        // We also want to preserve the ordering of the nodes for testability. So insert at the end of any existing
        // field nodes.

        var children = context.Class.Children;
        var index = 0;

        SkipNodes<DefaultTagHelperRuntimeIntermediateNode>(children, ref index);
        SkipNodes<FieldDeclarationIntermediateNode>(children, ref index);

        context.Class.Children.Insert(index, new FieldDeclarationIntermediateNode()
        {
            IsTagHelperField = true,
            Modifiers = CommonModifiers.Private,
            Name = context.GetFieldName(tagHelper),
            Type = "global::" + context.GetFullyQualifiedName(tagHelper),
        });
    }

    private static void SkipNodes<T>(IntermediateNodeCollection children, ref int i)
        where T : IntermediateNode
    {
        while (i < children.Count && children[i] is T)
        {
            i++;
        }
    }

    private static void AddTagHelperClass(Context context, TagHelperDescriptor tagHelper)
    {
        var node = new ViewComponentTagHelperIntermediateNode(context.GetClassName(tagHelper), tagHelper);

        context.Class.Children.Add(node);
    }

    private readonly struct Context(NamespaceDeclarationIntermediateNode namespaceNode, ClassDeclarationIntermediateNode classNode)
    {
        public ClassDeclarationIntermediateNode Class { get; } = classNode;
        public NamespaceDeclarationIntermediateNode Namespace { get; } = namespaceNode;

        private readonly Dictionary<TagHelperDescriptor, (string className, string fullyQualifiedName, string fieldName)> _tagHelpers = [];

        public IEnumerable<TagHelperDescriptor> TagHelpers => _tagHelpers.Keys;

        public bool Add(TagHelperDescriptor tagHelper)
        {
            if (_tagHelpers.ContainsKey(tagHelper))
            {
                return false;
            }

            var viewComponentName = tagHelper.ViewComponentName;

            var className = $"__Generated__{viewComponentName}ViewComponentTagHelper";
            var fullyQualifiedName = !Namespace.Name.IsNullOrEmpty()
                ? $"{Namespace.Name}.{Class.Name}.{className}"
                : $"{Namespace.Name}{Class.Name}.{className}";
            var fieldName = $"__{viewComponentName}ViewComponentTagHelper";

            _tagHelpers.Add(tagHelper, (className, fullyQualifiedName, fieldName));

            return true;
        }

        public string GetClassName(TagHelperDescriptor tagHelper)
            => _tagHelpers[tagHelper].className;

        public string GetFullyQualifiedName(TagHelperDescriptor tagHelper)
            => _tagHelpers[tagHelper].fullyQualifiedName;

        public string GetFieldName(TagHelperDescriptor tagHelper)
            => _tagHelpers[tagHelper].fieldName;
    }
}
