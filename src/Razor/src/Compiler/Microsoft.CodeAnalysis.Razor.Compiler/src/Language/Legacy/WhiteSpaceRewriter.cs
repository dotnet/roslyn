// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal sealed class WhitespaceRewriter(CancellationToken cancellationToken = default) : SyntaxRewriter
{
    private readonly CancellationToken _cancellationToken = cancellationToken;

    [return: NotNullIfNotNull(nameof(node))]
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (node == null)
        {
            return base.Visit(node);
        }

        var children = node.ChildNodesAndTokens();

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];

            if (child.AsNode() is CSharpCodeBlockSyntax codeBlock &&
                TryRewriteWhitespace(codeBlock, out var rewritten, out var whitespaceLiteral))
            {
                // Replace the existing code block with the whitespace literal
                // followed by the rewritten code block (with the code whitespace removed).
                node = node.ReplaceNode(codeBlock, [whitespaceLiteral, rewritten]);

                // Since we replaced node, its children are different. Update our collection.
                children = node.ChildNodesAndTokens();
            }
        }

        return base.Visit(node);
    }

    private static bool TryRewriteWhitespace(
        CSharpCodeBlockSyntax codeBlock,
        [NotNullWhen(true)] out CSharpCodeBlockSyntax? rewritten,
        [NotNullWhen(true)] out SyntaxNode? whitespaceLiteral)
    {
        // Rewrite any whitespace represented as code at the start of a line preceding an expression block.
        // We want it to be rendered as Markup.

        if (codeBlock.Children is [CSharpStatementLiteralSyntax literal, CSharpExplicitExpressionSyntax or CSharpImplicitExpressionSyntax, ..])
        {
            var containsNonWhitespace = literal.DescendantTokens().Any(static t => !string.IsNullOrWhiteSpace(t.Content));

            if (!containsNonWhitespace)
            {
                // Literal node is all whitespace. Can rewrite.
                whitespaceLiteral = SyntaxFactory.MarkupTextLiteral(literal.LiteralTokens);
                rewritten = codeBlock.ReplaceNode(literal, newNode: null);
                return true;
            }
        }

        rewritten = null;
        whitespaceLiteral = null;

        return false;
    }
}
