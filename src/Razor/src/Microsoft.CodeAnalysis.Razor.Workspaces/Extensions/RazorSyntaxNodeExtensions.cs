// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class RazorSyntaxNodeExtensions
{
    private static bool IsDirective(SyntaxNode node, DirectiveDescriptor directive, [NotNullWhen(true)] out RazorDirectiveBodySyntax? body)
    {
        if (node is RazorDirectiveSyntax { HasDirectiveDescriptor: true } directiveNode &&
            directiveNode.IsDirective(directive))
        {
            body = directiveNode.DirectiveBody;
            return true;
        }

        body = null;
        return false;
    }

    internal static bool IsAddTagHelperDirective(this RazorDirectiveSyntax directive)
        => directive.DirectiveBody.Keyword.GetContent() == SyntaxConstants.CSharp.AddTagHelperKeyword;

    internal static bool IsSectionDirective(this SyntaxNode node)
        => (node as RazorDirectiveSyntax)?.IsDirective(SectionDirective.Directive) is true;

    internal static bool IsCodeBlockDirective(this SyntaxNode node)
        => (node as RazorDirectiveSyntax)?.IsDirectiveKind(DirectiveKind.CodeBlock) is true;

    internal static bool IsUsingDirective(this SyntaxNode node, out SyntaxTokenList tokens)
    {
        if (node is RazorUsingDirectiveSyntax
            {
                DirectiveBody.Keyword: CSharpStatementLiteralSyntax
                {
                    LiteralTokens: var literalTokens
                }
            })
        {
            tokens = literalTokens;
            return true;
        }

        tokens = default;
        return false;
    }

    internal static bool IsConstrainedTypeParamDirective(this SyntaxNode node, [NotNullWhen(true)] out CSharpStatementLiteralSyntax? typeParamNode, [NotNullWhen(true)] out CSharpStatementLiteralSyntax? conditionsNode)
    {
        // Returns true for "@typeparam T where T : IDisposable", but not "@typeparam T"
        if (node is RazorDirectiveSyntax { DirectiveDescriptor: { } descriptor } &&
            IsDirective(node, ComponentConstrainedTypeParamDirective.Directive, out var body) &&
            descriptor.Tokens.Any(t => t.Name == ComponentResources.TypeParamDirective_Constraint_Name) &&
            // Children is the " T where T : IDisposable" part of the directive
            body.CSharpCode.Children is [_ /* whitespace */, CSharpStatementLiteralSyntax typeParam, _ /* whitespace */, CSharpStatementLiteralSyntax conditions, ..])
        {
            typeParamNode = typeParam;
            conditionsNode = conditions;
            return true;
        }

        typeParamNode = null;
        conditionsNode = null;
        return false;
    }

    internal static bool IsAttributeDirective(this SyntaxNode node, [NotNullWhen(true)] out CSharpStatementLiteralSyntax? attributeNode)
    {
        if (IsDirective(node, AttributeDirective.Directive, out var body) &&
            body.CSharpCode.Children is [_, CSharpStatementLiteralSyntax attribute, ..])
        {
            attributeNode = attribute;
            return true;
        }

        attributeNode = null;
        return false;
    }

    internal static bool IsCodeDirective(this SyntaxNode node)
    {
        if (IsDirective(node, ComponentCodeDirective.Directive, out var body) &&
            body.CSharpCode is { Children: { Count: > 0 } children } &&
            children.TryGetOpenBraceToken(out _))
        {
            return true;
        }

        return false;
    }

    internal static bool IsFunctionsDirective(this SyntaxNode node)
    {
        if (IsDirective(node, FunctionsDirective.Directive, out var body) &&
            body.CSharpCode is { Children: { Count: > 0 } children } &&
            children.TryGetOpenBraceToken(out _))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Walks up the tree through the <paramref name="owner"/>'s parents to find the outermost node that starts at the same position.
    /// </summary>
    internal static SyntaxNode? GetOutermostNode(this SyntaxNode owner)
    {
        var node = owner.Parent;
        if (node is null)
        {
            return owner;
        }

        var lastNode = node;
        while (node.SpanStart == owner.SpanStart)
        {
            lastNode = node;
            node = node.Parent;
            if (node is null)
            {
                break;
            }
        }

        return lastNode;
    }

    internal static bool TryGetPreviousSibling(
        this RazorSyntaxNode node,
        [NotNullWhen(true)] out RazorSyntaxNode? previousSibling)
    {
        previousSibling = null;

        var parent = node.Parent;
        if (parent is null)
        {
            return false;
        }

        foreach (var child in parent.ChildNodes())
        {
            if (ReferenceEquals(child, node))
            {
                return previousSibling is not null;
            }

            previousSibling = (RazorSyntaxNode)child;
        }

        Debug.Fail("How can we iterate node.Parent.ChildNodes() and not find node again?");

        previousSibling = null;
        return false;
    }

    /// <summary>
    /// Determines if <paramref name="firstNode"/> is immediately followed by <paramref name="secondNode"/> in the source text ignoring whitespace.
    /// </summary>
    public static bool IsNextTo(this RazorSyntaxNode firstNode, RazorSyntaxNode secondNode, SourceText text)
    {
        var index = firstNode.Span.End;
        var end = secondNode.Span.Start - 1;
        var c = text[index];
        while (char.IsWhiteSpace(c))
        {
            if (index == end)
            {
                return true;
            }

            c = text[++index];
        }

        return false;
    }

    public static bool ContainsOnlyWhitespace(this SyntaxNode node, bool includingNewLines = true)
    {
        foreach (var token in node.DescendantTokens())
        {
            if (!token.ContainsOnlyWhitespace(includingNewLines))
            {
                return false;
            }
        }

        // All tokens were either whitespace or new-lines.
        return true;
    }

    public static LinePositionSpan GetLinePositionSpan(this SyntaxNode node, RazorSourceDocument sourceDocument)
    {
        var start = node.Position;
        var end = node.EndPosition;
        var sourceText = sourceDocument.Text;

        Debug.Assert(start <= sourceText.Length && end <= sourceText.Length, "Node position exceeds source length.");

        if (start == sourceText.Length && node.Width == 0)
        {
            // Marker symbol at the end of the document.
            var location = node.GetSourceLocation(sourceDocument);

            return location.ToLinePosition().ToZeroWidthSpan();
        }

        return sourceText.GetLinePositionSpan(start, end);
    }

    /// <summary>
    /// Finds the innermost SyntaxNode for a given location in source, within a given node.
    /// </summary>
    /// <param name="node">The parent node to search inside.</param>
    /// <param name="index">The location to find the innermost node at.</param>
    /// <param name="includeWhitespace">Whether to include whitespace in the search.</param>
    /// <param name="walkMarkersBack">When true, if there are multiple <see cref="SyntaxKind.Marker"/> tokens in a single location, return the parent node of the
    /// first one in the tree.</param>
    public static SyntaxNode? FindInnermostNode(this SyntaxNode node, int index, bool includeWhitespace = false, bool walkMarkersBack = true)
    {
        var token = node.FindToken(index, includeWhitespace);

        // If the index is EOF but the node has index-1,
        // then try to get a token to the left of the index.
        // patterns like
        // <button></button>$$
        // should get the button node instead of the razor document (which is the parent
        // of the EOF token)
        if (token.Kind == SyntaxKind.EndOfFile && node.Span.Contains(index - 1))
        {
            token = token.GetPreviousToken(includeWhitespace);
        }

        var foundPosition = token!.Position;

        if (walkMarkersBack && token.Kind == SyntaxKind.Marker)
        {
            while (true)
            {
                var previousToken = token.GetPreviousToken(includeWhitespace);

                if (previousToken.Kind != SyntaxKind.Marker ||
                    previousToken.Position != foundPosition)
                {
                    break;
                }

                token = previousToken;
            }
        }

        return token.Parent;
    }

    public static SyntaxNode? FindNode(this SyntaxNode @this, TextSpan span, bool includeWhitespace = false, bool getInnermostNodeForTie = false)
    {
        if (!@this.Span.Contains(span))
        {
            return ThrowHelper.ThrowArgumentOutOfRangeException<SyntaxNode?>(nameof(span));
        }

        var node = @this.FindToken(span.Start, includeWhitespace)
            .Parent!
            .FirstAncestorOrSelf<SyntaxNode>(a => a.Span.Contains(span));

        node.AssumeNotNull();

        // Tie-breaking.
        if (!getInnermostNodeForTie)
        {
            var cuRoot = node.Ancestors();

            // Only null if node is the original node is the root
            if (cuRoot is null)
            {
                return node;
            }

            while (true)
            {
                var parent = node.Parent;
                // NOTE: We care about FullSpan equality, but FullWidth is cheaper and equivalent.
                if (parent == null || parent.Width != node.Width)
                {
                    break;
                }

                // prefer child over compilation unit
                if (parent == cuRoot)
                {
                    break;
                }

                node = parent;
            }
        }

        return node;
    }

    public static bool ExistsOnTarget(this SyntaxNode node, SyntaxNode target)
    {
        // TODO: This looks like a potential allocation hotspot and performance bottleneck.

        var nodeString = node.RemoveEmptyNewLines().ToString();
        var matchingNode = target.DescendantNodesAndSelf()
            // Empty new lines can affect our comparison so we remove them since they're insignificant.
            .Where(n => n.RemoveEmptyNewLines().ToString() == nodeString)
            .FirstOrDefault();

        return matchingNode is not null;
    }

    public static SyntaxNode RemoveEmptyNewLines(this SyntaxNode node)
    {
        if (node is MarkupTextLiteralSyntax markupTextLiteral)
        {
            var literalTokens = markupTextLiteral.LiteralTokens;
            using var literalTokensWithoutLines = new PooledArrayBuilder<SyntaxToken>(literalTokens.Count);

            foreach (var token in literalTokens)
            {
                if (token.Kind != SyntaxKind.NewLine)
                {
                    literalTokensWithoutLines.Add(token);
                }
            }

            return markupTextLiteral.WithLiteralTokens(literalTokensWithoutLines.ToList());
        }

        return node;
    }

    public static bool IsCSharpNode(this SyntaxNode node, [NotNullWhen(true)] out CSharpCodeBlockSyntax? csharpCodeBlock)
    {
        csharpCodeBlock = null;

        // Any piece of C# code can potentially be surrounded by a CSharpCodeBlockSyntax.
        switch (node)
        {
            case CSharpCodeBlockSyntax outerCSharpCodeBlock:
                var innerCSharpNode = outerCSharpCodeBlock.ChildNodes().FirstOrDefault(
                    static n => n is CSharpStatementSyntax or
                                     RazorDirectiveSyntax or
                                     CSharpExplicitExpressionSyntax or
                                     CSharpImplicitExpressionSyntax);

                if (innerCSharpNode is not null)
                {
                    return innerCSharpNode.IsCSharpNode(out csharpCodeBlock);
                }

                break;

            // @code {
            //    var foo = "bar";
            // }
            case RazorDirectiveSyntax { DirectiveBody: var body }:
                // code {
                //    var foo = "bar";
                // }
                var directive = body.Keyword.ToString();
                if (directive != "code")
                {
                    return false;
                }

                // {
                //    var foo = "bar";
                // }
                csharpCodeBlock = body.CSharpCode;

                // var foo = "bar";
                var innerCodeBlock = csharpCodeBlock.ChildNodes().OfType<CSharpCodeBlockSyntax>().FirstOrDefault();
                if (innerCodeBlock is not null)
                {
                    csharpCodeBlock = innerCodeBlock;
                }

                break;

            // @(x)
            // (x)
            case CSharpExplicitExpressionSyntax { Body: CSharpExplicitExpressionBodySyntax body }:
                // x
                csharpCodeBlock = body.CSharpCode;
                break;

            // @x
            case CSharpImplicitExpressionSyntax { Body: CSharpImplicitExpressionBodySyntax body }:
                // x
                csharpCodeBlock = body.CSharpCode;
                break;

            // @{
            //    var x = 1;
            // }
            case CSharpStatementSyntax csharpStatement:
                // {
                //    var x = 1;
                // }
                var csharpStatementBody = csharpStatement.Body;

                // var x = 1;
                csharpCodeBlock = csharpStatementBody.ChildNodes().OfType<CSharpCodeBlockSyntax>().FirstOrDefault();
                break;
        }

        return csharpCodeBlock is not null;
    }

    public static bool IsAnyAttributeSyntax(this SyntaxNode node)
    {
        return node is
            MarkupAttributeBlockSyntax or
            MarkupMinimizedAttributeBlockSyntax or
            MarkupTagHelperAttributeSyntax or
            MarkupMinimizedTagHelperAttributeSyntax or
            MarkupTagHelperDirectiveAttributeSyntax or
            MarkupMinimizedTagHelperDirectiveAttributeSyntax or
            MarkupMiscAttributeContentSyntax;
    }

    public static bool TryGetLinePositionSpanWithoutWhitespace(this SyntaxNode node, RazorSourceDocument source, out LinePositionSpan linePositionSpan)
    {
        var tokens = node.DescendantTokens();

        SyntaxToken? firstToken = null;
        foreach (var token in tokens)
        {
            if (!token.IsWhitespace())
            {
                firstToken = token;
                break;
            }
        }

        SyntaxToken? lastToken = null;
        foreach (var token in tokens.Reverse())
        {
            if (!token.IsWhitespace())
            {
                lastToken = token;
                break;
            }
        }

        // These two are either both null or neither null, but the || means the compiler doesn't give us nullability warnings
        if (firstToken is null || lastToken is null)
        {
            linePositionSpan = default;
            return false;
        }

        var startPositionSpan = firstToken.GetValueOrDefault().GetLinePositionSpan(source);
        var endPositionSpan = lastToken.GetValueOrDefault().GetLinePositionSpan(source);

        if (endPositionSpan.End < startPositionSpan.Start)
        {
            linePositionSpan = default;
            return false;
        }

        linePositionSpan = new LinePositionSpan(startPositionSpan.Start, endPositionSpan.End);
        return true;
    }

    public static bool TryGetFirstToken(this SyntaxNode node, out SyntaxToken result)
        => node.TryGetFirstToken(includeZeroWidth: false, out result);

    public static bool TryGetFirstToken(this SyntaxNode node, bool includeZeroWidth, out SyntaxToken result)
    {
        result = node.GetFirstToken(includeZeroWidth);
        return result != default;
    }

    public static bool TryGetLastToken(this SyntaxNode node, out SyntaxToken result)
        => node.TryGetLastToken(includeZeroWidth: false, out result);

    public static bool TryGetLastToken(this SyntaxNode node, bool includeZeroWidth, out SyntaxToken result)
    {
        result = node.GetLastToken(includeZeroWidth);
        return result != default;
    }

    public static TextSpan SpanWithoutTrailingNewLines(this SyntaxNode node, SourceText sourceText)
    {
        var span = node.Span;
        var end = span.End;
        while (end > span.Start && sourceText[end - 1] is '\r' or '\n')
        {
            end--;
        }

        return TextSpan.FromBounds(span.Start, end);
    }
}
