// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RazorSyntaxTreeExtensions
{
    public static ImmutableArray<RazorDirectiveSyntax> GetSectionDirectives(this RazorSyntaxTree syntaxTree)
        => GetDirectives<RazorDirectiveSyntax>(syntaxTree, static d => d.IsSectionDirective());

    public static ImmutableArray<RazorDirectiveSyntax> GetCodeBlockDirectives(this RazorSyntaxTree syntaxTree)
        => GetDirectives<RazorDirectiveSyntax>(syntaxTree, static d => d.IsCodeBlockDirective());

    public static ImmutableArray<RazorUsingDirectiveSyntax> GetUsingDirectives(this RazorSyntaxTree syntaxTree)
        => GetDirectives<RazorUsingDirectiveSyntax>(syntaxTree);

    public static ImmutableArray<TDirective> GetDirectives<TDirective>(
        this RazorSyntaxTree syntaxTree, Func<TDirective, bool>? predicate = null)
        where TDirective : BaseRazorDirectiveSyntax
    {
        using var builder = new PooledArrayBuilder<TDirective>();
        builder.AddRange(EnumerateDirectives(syntaxTree, predicate));

        return builder.ToImmutable();
    }

    public static IEnumerable<RazorDirectiveSyntax> EnumerateSectionDirectives(this RazorSyntaxTree syntaxTree)
        => EnumerateDirectives<RazorDirectiveSyntax>(syntaxTree, static d => d.IsSectionDirective());

    public static IEnumerable<RazorDirectiveSyntax> EnumerateCodeBlockDirectives(this RazorSyntaxTree syntaxTree)
        => EnumerateDirectives<RazorDirectiveSyntax>(syntaxTree, static d => d.IsCodeBlockDirective());

    public static IEnumerable<RazorUsingDirectiveSyntax> EnumerateUsingDirectives(this RazorSyntaxTree syntaxTree)
        => EnumerateDirectives<RazorUsingDirectiveSyntax>(syntaxTree);

    public static IEnumerable<RazorDirectiveSyntax> EnumerateAddTagHelperDirectives(this RazorSyntaxTree syntaxTree)
        => EnumerateDirectives<RazorDirectiveSyntax>(syntaxTree, static d => d.IsAddTagHelperDirective());

    public static IEnumerable<TDirective> EnumerateDirectives<TDirective>(
        this RazorSyntaxTree syntaxTree, Func<TDirective, bool>? predicate = null)
        where TDirective : BaseRazorDirectiveSyntax
    {
        foreach (var node in syntaxTree.Root.DescendantNodes(MayContainDirectives))
        {
            if (node is TDirective directive && (predicate == null || predicate(directive)))
            {
                yield return directive;
            }
        }
    }

    public static bool MayContainDirectives(this SyntaxNode node)
    {
        return node is RazorDocumentSyntax or MarkupBlockSyntax or CSharpCodeBlockSyntax;
    }
}
