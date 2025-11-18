// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static partial class DirectiveSyntaxExtensions
{
    private static readonly ConditionalWeakTable<SyntaxNode, DirectiveInfo<DirectiveTriviaSyntax>> s_rootToDirectiveInfo = new();

    private static SyntaxNode GetAbsoluteRoot(this SyntaxNode node)
    {
        while (node.Parent != null || node is StructuredTriviaSyntax)
        {
            node = node.Parent ?? node.ParentTrivia.Token.GetRequiredParent();
        }

        return node;
    }

    private static DirectiveInfo<DirectiveTriviaSyntax> GetDirectiveInfo(SyntaxNode node, CancellationToken cancellationToken)
        => s_rootToDirectiveInfo.GetValue(
            node.GetAbsoluteRoot(),
            root => GetDirectiveInfoForRoot(root, cancellationToken));

    private static DirectiveInfo<DirectiveTriviaSyntax> GetDirectiveInfoForRoot(SyntaxNode root, CancellationToken cancellationToken)
        => CodeAnalysis.Shared.Extensions.SyntaxNodeExtensions.GetDirectiveInfoForRoot<DirectiveTriviaSyntax>(
            root, CSharpSyntaxKinds.Instance, cancellationToken);

    public static DirectiveTriviaSyntax? GetMatchingDirective(this DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
    {
        if (IsConditionalDirective(directive) ||
            IsRegionDirective(directive))
        {
            var directiveSyntaxMap = GetDirectiveInfo(directive, cancellationToken).DirectiveMap;
            if (directiveSyntaxMap.TryGetValue(directive, out var result))
                return result;
        }

        return null;
    }

    public static ImmutableArray<DirectiveTriviaSyntax> GetMatchingConditionalDirectives(this DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
    {
        if (IsConditionalDirective(directive))
        {
            var directiveConditionalMap = GetDirectiveInfo(directive, cancellationToken).ConditionalMap;
            if (directiveConditionalMap.TryGetValue(directive, out var result))
                return result;
        }

        return [];
    }

    private static bool IsRegionDirective(DirectiveTriviaSyntax directive)
        => directive?.Kind() is SyntaxKind.RegionDirectiveTrivia or SyntaxKind.EndRegionDirectiveTrivia;

    private static bool IsConditionalDirective(DirectiveTriviaSyntax directive)
        => directive?.Kind()
            is SyntaxKind.IfDirectiveTrivia
            or SyntaxKind.ElifDirectiveTrivia
            or SyntaxKind.ElseDirectiveTrivia
            or SyntaxKind.EndIfDirectiveTrivia;
}
