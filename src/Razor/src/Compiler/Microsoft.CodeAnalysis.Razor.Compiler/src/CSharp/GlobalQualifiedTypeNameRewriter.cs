// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Razor;

// Rewrites type names to use the 'global::' prefix for identifiers.
//
// This is useful when we're generating code in a different namespace than
// what the user code lives in. When we synthesize a namespace it's easy to have
// clashes.
internal class GlobalQualifiedTypeNameRewriter : TypeNameRewriter
{
    // List of names to ignore.
    //
    // NOTE: this is the list of type parameters defined on the component.
    private readonly HashSet<string> _ignore;

    public GlobalQualifiedTypeNameRewriter(ICollection<string> ignore)
    {
        _ignore = new HashSet<string>(ignore);
    }

    public override string Rewrite(string typeName)
    {
        var parsed = SyntaxFactory.ParseTypeName(typeName);
        var rewritten = (TypeSyntax)new Visitor(_ignore).Visit(parsed);
        return rewritten.ToFullString();
    }

    public override void RewriteComponentTypeName(ComponentIntermediateNode node)
    {
        node.TypeName = Rewrite(node.TypeName);
    }

    private class Visitor : CSharpSyntaxRewriter
    {
        private readonly HashSet<string> _ignore;

        public Visitor(HashSet<string> ignore)
        {
            _ignore = ignore;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            return base.Visit(node);
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (node.Parent is QualifiedNameSyntax)
            {
                return base.VisitQualifiedName(node);
            }

            // Need to rewrite postorder so we can rewrite the names of generic type arguments.
            node = (QualifiedNameSyntax)base.VisitQualifiedName(node);

            // Qualify only the leftmost name. Reusing the rewritten children avoids serializing
            // and reparsing the entire name, including all of its generic type arguments.
            return Qualify(node);

            static NameSyntax Qualify(QualifiedNameSyntax node)
            {
                NameSyntax name = node;
                // Find the leftmost name so the qualifier applies to the whole name, not its final segment.
                while (name is QualifiedNameSyntax qualified)
                {
                    name = qualified.Left;
                }

                // An existing global:: or custom alias already anchors the name, so don't add another qualifier.
                if (name is AliasQualifiedNameSyntax)
                {
                    return node;
                }

                // With qualified and alias-qualified names excluded, only identifier and generic names remain,
                // which both inherit from SimpleNameSyntax.
                var simple = (SimpleNameSyntax)name;
                NameSyntax rewritten = SyntaxFactory.AliasQualifiedName(
                    SyntaxFactory.IdentifierName(SyntaxFactory.Token(CSharp.SyntaxKind.GlobalKeyword)),
                    simple.WithoutLeadingTrivia()).WithLeadingTrivia(simple.GetLeadingTrivia());

                // The original parent links let us rebuild from the inside out without recursion
                // or a separate stack, even for long namespace-qualified names.
                while (name != node)
                {
                    var parent = (QualifiedNameSyntax)name.Parent;
                    rewritten = parent.WithLeft(rewritten);
                    name = parent;
                }

                return rewritten;
            }
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (_ignore.Contains(node.ToString()))
            {
                return node;
            }

            if (node.Parent != null)
            {
                return node;
            }

            return SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(CSharp.SyntaxKind.GlobalKeyword)), node);
        }
    }
}
