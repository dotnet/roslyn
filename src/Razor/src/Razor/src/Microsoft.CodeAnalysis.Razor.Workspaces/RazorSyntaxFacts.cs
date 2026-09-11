// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;
using RazorSyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;

namespace Microsoft.CodeAnalysis.Razor;

internal static class RazorSyntaxFacts
{
    /// <summary>
    /// Given an absolute index positioned in an attribute, finds the absolute index of the part of the
    /// attribute that represents the attribute name. eg. for @bi$$nd-Value it will find the absolute index
    /// of "Value"
    /// </summary>
    public static bool TryGetAttributeNameAbsoluteIndex(RazorCodeDocument codeDocument, int absoluteIndex, out int attributeNameAbsoluteIndex)
    {
        attributeNameAbsoluteIndex = 0;

        var root = codeDocument.GetRequiredSyntaxRoot();
        var owner = root.FindInnermostNode(absoluteIndex);

        var attributeName = owner?.Parent switch
        {
            MarkupTagHelperAttributeSyntax att => att.Name,
            MarkupMinimizedTagHelperAttributeSyntax att => att.Name,
            MarkupTagHelperDirectiveAttributeSyntax att => att.Name,
            MarkupMinimizedTagHelperDirectiveAttributeSyntax att => att.Name,
            _ => null
        };

        if (attributeName is null)
        {
            return false;
        }

        // Can't get to this point if owner was null, but the compiler doesn't know that
        Assumes.NotNull(owner);

        // The GetOwner method can be surprising, eg. Foo="$$Bar" will return the starting quote of the attribute value,
        // but its parent is the attribute name. Easy enough to filter that sort of thing out by just requiring
        // the caret position to be somewhere within the attribute name.
        if (!GetFullAttributeNameSpan(owner.Parent).Contains(absoluteIndex))
        {
            return false;
        }

        if (attributeName.LiteralTokens is [{ } name])
        {
            var attribute = name.Content;
            if (attribute.StartsWith("bind-"))
            {
                attributeNameAbsoluteIndex = attributeName.SpanStart + 5;
            }
            else
            {
                attributeNameAbsoluteIndex = attributeName.SpanStart;
            }

            return true;
        }

        return false;
    }

    public static TextSpan GetFullAttributeNameSpan(RazorSyntaxNode? node)
    {
        return node switch
        {
            MarkupAttributeBlockSyntax att => att.Name.Span,
            MarkupMinimizedAttributeBlockSyntax att => att.Name.Span,
            MarkupTagHelperAttributeSyntax att => att.Name.Span,
            MarkupMinimizedTagHelperAttributeSyntax att => att.Name.Span,
            MarkupTagHelperDirectiveAttributeSyntax att => CalculateFullSpan(att.Name, att.ParameterName, att.Transition),
            MarkupMinimizedTagHelperDirectiveAttributeSyntax att => CalculateFullSpan(att.Name, att.ParameterName, att.Transition),
            _ => default,
        };

        static TextSpan CalculateFullSpan(MarkupTextLiteralSyntax attributeName, MarkupTextLiteralSyntax? parameterName, RazorMetaCodeSyntax? transition)
        {
            var start = attributeName.SpanStart;
            var length = attributeName.Span.Length;

            // The transition is the "@" if its present
            if (transition is not null)
            {
                start -= 1;
                length += 1;
            }

            // The parameter is, for example, the ":after" but does not include the colon, so we have to account for it
            if (parameterName is not null)
            {
                length += 1 + parameterName.Span.Length;
            }

            return new TextSpan(start, length);
        }
    }

    /// <summary>
    /// For example given "&lt;Goo @bi$$nd-Value:after="val" /&gt;", it would return the span from "V" to "e".
    /// </summary>
    public static bool TryGetComponentParameterNameFromFullAttributeName(string fullAttributeName, out ReadOnlySpan<char> componentParameterName, out ReadOnlySpan<char> directiveAttributeParameter)
    {
        componentParameterName = fullAttributeName.AsSpan();
        directiveAttributeParameter = default;
        if (componentParameterName.IsEmpty)
        {
            return false;
        }

        // Parse @bind directive
        if (componentParameterName[0] == '@')
        {
            // Trim `@` transition
            componentParameterName = componentParameterName[1..];

            // Check for and trim `bind-` directive prefix
            if (!componentParameterName.StartsWith("bind-", StringComparison.Ordinal))
            {
                return false;
            }

            componentParameterName = componentParameterName["bind-".Length..];

            // Trim directive parameter name, if any
            if (componentParameterName.LastIndexOf(':') is int colonIndex and > 0)
            {
                directiveAttributeParameter = componentParameterName[(colonIndex + 1)..];
                componentParameterName = componentParameterName[..colonIndex];
            }
        }

        return true;
    }

    public static bool IsInCodeBlock(RazorSyntaxNode n)
        => n.FirstAncestorOrSelf<RazorSyntaxNode>(static n => n is RazorDirectiveSyntax { DirectiveDescriptor.Directive: "code" }) is not null;

    internal static bool TryGetNamespaceFromDirective(RazorUsingDirectiveSyntax directiveNode, [NotNullWhen(true)] out string? @namespace)
    {
        foreach (var child in directiveNode.DescendantNodes())
        {
            if (child.GetChunkGenerator() is AddImportChunkGenerator usingStatement)
            {
                @namespace = usingStatement.Namespace;
                return true;
            }
        }

        @namespace = null;
        return false;
    }

    internal static bool IsInUsingDirective(RazorSyntaxNode node)
    {
        return node.AncestorsAndSelf().OfType<RazorUsingDirectiveSyntax>().Any();
    }

    internal static bool IsScriptOrStyleBlock(BaseMarkupElementSyntax? element)
    {
        // StartTag is annotated as not nullable, but on invalid documents it can be. The 'Format_DocumentWithDiagnostics' test
        // illustrates this.
        if (element?.StartTag?.Name.Content is not { } tagName)
        {
            return false;
        }

        return string.Equals(tagName, "script", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tagName, "style", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ContainsComplexRazorInScriptOrStyleBody(BaseMarkupElementSyntax element, SourceText sourceText)
    {
        if (!IsScriptOrStyleBlock(element) ||
            element.StartTag is not { } startTag ||
            element.EndTag is not { } endTag ||
            endTag.SpanStart < startTag.EndPosition)
        {
            return false;
        }

        var bodySpan = TextSpan.FromBounds(startTag.EndPosition, endTag.SpanStart);
        foreach (var token in element.DescendantTokens(bodySpan))
        {
            // Razor comments are projected as placeholders, which makes script/style indentation unsafe to delegate.
            if (token.Kind == SyntaxKind.RazorCommentTransition)
            {
                return true;
            }

            // Only non-escaped Razor transitions can introduce code that makes the block complex.
            if (token.Kind != SyntaxKind.Transition ||
                IsEscapedTransition(token))
            {
                continue;
            }

            // Allow only single-line expressions embedded within script/style content; every other Razor construct is complex.
            var expression = token.Parent?.AncestorsAndSelf().FirstOrDefault(
                static node => node is CSharpImplicitExpressionSyntax or CSharpExplicitExpressionSyntax);
            if (expression is null ||
                !IsSingleLineInlineExpression(expression, sourceText, startTag.EndPosition, endTag.SpanStart))
            {
                return true;
            }
        }

        return false;

        static bool IsSingleLineInlineExpression(RazorSyntaxNode expression, SourceText sourceText, int bodyStart, int bodyEnd)
        {
            var linePositionSpan = sourceText.GetLinePositionSpan(expression.Span);
            if (linePositionSpan.Start.Line != linePositionSpan.End.Line ||
                expression.DescendantNodes().Any(static node =>
                    node is CSharpTemplateBlockSyntax or
                        CSharpStatementSyntax or
                        RazorCommentBlockSyntax or
                        RazorDirectiveSyntax))
            {
                return false;
            }

            var line = sourceText.Lines[linePositionSpan.Start.Line];
            return ContainsNonWhitespace(sourceText, Math.Max(line.Start, bodyStart), expression.SpanStart) &&
                ContainsNonWhitespace(sourceText, expression.EndPosition, Math.Min(line.End, bodyEnd));
        }

        static bool IsEscapedTransition(RazorSyntaxToken token)
        {
            if (IsInEphemeralTextLiteral(token))
            {
                return true;
            }

            var previousToken = token.GetPreviousToken();
            return previousToken.Kind == SyntaxKind.Transition &&
                previousToken.EndPosition == token.Position &&
                IsInEphemeralTextLiteral(previousToken);
        }

        static bool IsInEphemeralTextLiteral(RazorSyntaxToken token)
            => token.Parent?.AncestorsAndSelf().Any(static node => node is MarkupEphemeralTextLiteralSyntax) == true;

        static bool ContainsNonWhitespace(SourceText sourceText, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                if (!char.IsWhiteSpace(sourceText[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal static bool IsAttributeName(RazorSyntaxNode node, [NotNullWhen(true)] out BaseMarkupStartTagSyntax? startTag)
    {
        startTag = null;

        if (node.Parent.IsAnyAttributeSyntax() &&
            GetFullAttributeNameSpan(node.Parent).Start == node.SpanStart)
        {
            startTag = node.Parent.Parent as BaseMarkupStartTagSyntax;
        }

        return startTag is not null;
    }
}
