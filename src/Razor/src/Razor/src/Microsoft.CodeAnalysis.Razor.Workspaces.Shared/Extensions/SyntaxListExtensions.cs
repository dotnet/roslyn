// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxListExtensions
{
    internal static SyntaxNode PreviousSiblingOrSelf(this SyntaxList<RazorSyntaxNode> syntaxList, RazorSyntaxNode syntaxNode)
    {
        var index = syntaxList.IndexOf(syntaxNode);

        return index switch
        {
            0 => syntaxNode,
            -1 => ThrowHelper.ThrowInvalidOperationException<SyntaxNode>("The provided node was not in the SyntaxList"),
            _ => syntaxList[index - 1]
        };
    }

    internal static SyntaxNode NextSiblingOrSelf(this SyntaxList<RazorSyntaxNode> syntaxList, RazorSyntaxNode syntaxNode)
    {
        var index = syntaxList.IndexOf(syntaxNode);

        return index switch
        {
            var i when i == syntaxList.Count - 1 => syntaxNode,
            -1 => ThrowHelper.ThrowInvalidOperationException<SyntaxNode>("The provided node was not in the SyntaxList"),
            _ => syntaxList[index + 1]
        };
    }

    internal static bool TryGetOpenBraceNode(this SyntaxList<RazorSyntaxNode> children, [NotNullWhen(true)] out RazorMetaCodeSyntax? brace)
    {
        // If there is no whitespace between the directive and the brace then there will only be
        // three children and the brace should be the first child
        brace = null;

        if (children.FirstOrDefault(static c => c.Kind == SyntaxKind.RazorMetaCode) is RazorMetaCodeSyntax metaCode)
        {
            var token = metaCode.MetaCode.SingleOrDefault(static m => m.Kind == SyntaxKind.LeftBrace);
            if (token != default)
            {
                brace = metaCode;
            }
        }

        return brace != null;
    }

    internal static bool TryGetCloseBraceNode(this SyntaxList<RazorSyntaxNode> children, [NotNullWhen(true)] out RazorMetaCodeSyntax? brace)
    {
        // If there is no whitespace between the directive and the brace then there will only be
        // three children and the brace should be the last child
        brace = null;

        if (children.LastOrDefault(static c => c.Kind == SyntaxKind.RazorMetaCode) is RazorMetaCodeSyntax metaCode)
        {
            var token = metaCode.MetaCode.SingleOrDefault(static m => m.Kind == SyntaxKind.RightBrace);
            if (token != default)
            {
                brace = metaCode;
            }
        }

        return brace != null;
    }

    internal static bool TryGetOpenBraceToken(this SyntaxList<RazorSyntaxNode> children, out SyntaxToken brace)
    {
        brace = default;

        if (children.TryGetOpenBraceNode(out var metacode))
        {
            var token = metacode.MetaCode.SingleOrDefault(static m => m.Kind == SyntaxKind.LeftBrace);
            if (token != default)
            {
                brace = token;
            }
        }

        return brace != default;
    }

    internal static bool TryGetCloseBraceToken(this SyntaxList<RazorSyntaxNode> children, out SyntaxToken brace)
    {
        brace = default;

        if (children.TryGetCloseBraceNode(out var metacode))
        {
            var token = metacode.MetaCode.SingleOrDefault(static m => m.Kind == SyntaxKind.RightBrace);
            if (token != default)
            {
                brace = token;
            }
        }

        return brace != default;
    }
}
