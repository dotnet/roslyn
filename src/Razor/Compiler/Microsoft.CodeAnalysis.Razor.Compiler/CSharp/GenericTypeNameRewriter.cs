// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Razor;

internal class GenericTypeNameRewriter : TypeNameRewriter
{
    private readonly Dictionary<string, ComponentTypeArgumentIntermediateNode> _bindings;

    public GenericTypeNameRewriter(Dictionary<string, ComponentTypeArgumentIntermediateNode> bindings)
    {
        _bindings = bindings;
    }

    public override string Rewrite(string typeName) => Rewrite(typeName, out _);

    public override void RewriteComponentTypeName(ComponentIntermediateNode node)
    {
        node.TypeName = Rewrite(node.TypeName, out var usedBindings);
        node.OrderedTypeArguments = usedBindings;
    }

    private string Rewrite(string typeName, out ImmutableArray<ComponentTypeArgumentIntermediateNode> usedTypeArguments)
    {
        using var _ = ArrayBuilderPool<ComponentTypeArgumentIntermediateNode>.GetPooledObject(out var builder);

        var parsed = SyntaxFactory.ParseTypeName(typeName);
        var rewritten = (TypeSyntax)new Visitor(_bindings, builder).Visit(parsed);
        usedTypeArguments = builder.ToImmutable();
        return rewritten.ToFullString();
    }

    private class Visitor : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, ComponentTypeArgumentIntermediateNode> _bindings;
        private readonly ImmutableArray<ComponentTypeArgumentIntermediateNode>.Builder _usedBindings;

        public Visitor(Dictionary<string, ComponentTypeArgumentIntermediateNode> bindings, ImmutableArray<ComponentTypeArgumentIntermediateNode>.Builder usedBindings)
        {
            _bindings = bindings;
            _usedBindings = usedBindings;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            // We can handle a single IdentifierNameSyntax at the top level (like 'TItem)
            // OR a GenericNameSyntax recursively (like `List<T>`)
            if (node is IdentifierNameSyntax identifier && !(identifier.Parent is QualifiedNameSyntax))
            {
                if (_bindings.TryGetValue(identifier.Identifier.Text, out var binding))
                {
                    _usedBindings.Add(binding);

                    // If we don't have a valid replacement, use object. This will make the code at least reasonable
                    // compared to leaving the type parameter in place.
                    //
                    // We add our own diagnostics for missing/invalid type parameters anyway.
                    var content = binding?.Value?.Content;
                    var replacement = !string.IsNullOrWhiteSpace(content) ? content : "object";
                    return identifier.Update(SyntaxFactory.Identifier(replacement).WithTriviaFrom(identifier.Identifier));
                }
            }

            return base.Visit(node);
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            var args = node.TypeArgumentList.Arguments;
            for (var i = 0; i < args.Count; i++)
            {
                var typeArgument = args[i];
                args = args.Replace(typeArgument, (TypeSyntax)Visit(typeArgument));
            }

            return node.WithTypeArgumentList(node.TypeArgumentList.WithArguments(args));
        }
    }
}
