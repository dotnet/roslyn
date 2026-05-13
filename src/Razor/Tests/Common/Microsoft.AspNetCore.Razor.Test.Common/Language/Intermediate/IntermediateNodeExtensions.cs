// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal static class IntermediateNodeExtensions
{
    public static string GetCSharpContent(this IntermediateNode node)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        foreach (var child in node.Children)
        {
            if (child is CSharpIntermediateToken csharpToken)
            {
                builder.Append(csharpToken.Content);
            }
        }

        return builder.ToString();
    }

    public static ImmutableArray<NamespaceDeclarationIntermediateNode> GetNamespaceNodes(this IntermediateNode node)
    {
        var visitor = new Visitor();
        visitor.Visit(node);

        return visitor.NamespaceNodes;
    }

    public static ImmutableArray<ClassDeclarationIntermediateNode> GetClassNodes(this IntermediateNode node)
    {
        var visitor = new Visitor();
        visitor.Visit(node);

        return visitor.ClassNodes;
    }

    public static ImmutableArray<MethodDeclarationIntermediateNode> GetMethodNodes(this IntermediateNode node)
    {
        var visitor = new Visitor();
        visitor.Visit(node);

        return visitor.MethodNodes;
    }

    public static ImmutableArray<ExtensionIntermediateNode> GetExtensionNodes(this IntermediateNode node)
    {
        var visitor = new Visitor();
        visitor.Visit(node);

        return visitor.ExtensionNodes;
    }

    public static ImmutableArray<TagHelperIntermediateNode> GetTagHelperNodes(this IntermediateNode node)
    {
        var visitor = new Visitor();
        visitor.Visit(node);

        return visitor.TagHelperNodes;
    }

    public static NamespaceDeclarationIntermediateNode GetNamespaceNode(this IntermediateNode node)
        => node.GetNamespaceNodes().First();

    public static ClassDeclarationIntermediateNode GetClassNode(this IntermediateNode node)
        => node.GetClassNodes().First();

    public static MethodDeclarationIntermediateNode GetMethodNode(this IntermediateNode node)
        => node.GetMethodNodes().First();

    public static ExtensionIntermediateNode GetExtensionNode(this IntermediateNode node)
        => node.GetExtensionNodes().First();

    public static TagHelperIntermediateNode GetTagHelperNode(this IntermediateNode node)
        => node.GetTagHelperNodes().First();

    private sealed class Visitor : IntermediateNodeWalker
    {
        private readonly ImmutableArray<NamespaceDeclarationIntermediateNode>.Builder _namespaceNodes = ImmutableArray.CreateBuilder<NamespaceDeclarationIntermediateNode>();
        private readonly ImmutableArray<ClassDeclarationIntermediateNode>.Builder _classNodes = ImmutableArray.CreateBuilder<ClassDeclarationIntermediateNode>();
        private readonly ImmutableArray<MethodDeclarationIntermediateNode>.Builder _methodNodes = ImmutableArray.CreateBuilder<MethodDeclarationIntermediateNode>();
        private readonly ImmutableArray<ExtensionIntermediateNode>.Builder _extensionNodes = ImmutableArray.CreateBuilder<ExtensionIntermediateNode>();
        private readonly ImmutableArray<TagHelperIntermediateNode>.Builder _tagHelperNodes = ImmutableArray.CreateBuilder<TagHelperIntermediateNode>();

        public ImmutableArray<NamespaceDeclarationIntermediateNode> NamespaceNodes => _namespaceNodes.ToImmutable();
        public ImmutableArray<ClassDeclarationIntermediateNode> ClassNodes => _classNodes.ToImmutable();
        public ImmutableArray<MethodDeclarationIntermediateNode> MethodNodes => _methodNodes.ToImmutable();
        public ImmutableArray<ExtensionIntermediateNode> ExtensionNodes => _extensionNodes.ToImmutable();
        public ImmutableArray<TagHelperIntermediateNode> TagHelperNodes => _tagHelperNodes.ToImmutable();

        public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
        {
            _methodNodes.Add(node);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
        {
            _namespaceNodes.Add(node);
            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            _classNodes.Add(node);
            base.VisitClassDeclaration(node);
        }

        public override void VisitExtension(ExtensionIntermediateNode node)
        {
            _extensionNodes.Add(node);
            base.VisitExtension(node);
        }

        public override void VisitTagHelper(TagHelperIntermediateNode node)
        {
            _tagHelperNodes.Add(node);
            base.VisitTagHelper(node);
        }
    }
}
